import * as fs from 'fs';
import * as path from 'path';
import { spawn, execSync } from 'child_process';
import { Config } from './config.js';
import { generateTimestampedFilename } from './storage.js';

/**
 * Result of running a bash command
 */
export interface BashCommandResult {
  /** Last 20 lines of output */
  output: string[];
  /** Exit status - either exit code or timeout message */
  status: string;
  /** Optional truncation message if output was truncated */
  truncationMessage?: string;
}

/**
 * Run a bash command with timeout and capture output to a file
 * @param command - The bash command to execute
 * @param workingDirectory - Working directory for the command (absolute path)
 * @param timeoutSeconds - Timeout in seconds
 * @param config - Server configuration
 * @param storageDirectory - Storage directory for output files
 * @param prependEnvironment - Optional key-value pairs to prepend to environment variables
 * @param appendEnvironment - Optional key-value pairs to append to environment variables
 * @param setEnvironment - Optional key-value pairs to set environment variables (replaces existing values)
 * @returns Promise<BashCommandResult>
 */
export async function runBashCommand(
  command: string,
  workingDirectory: string,
  timeoutSeconds: number,
  config: Config,
  storageDirectory: string,
  prependEnvironment?: Record<string, string>,
  appendEnvironment?: Record<string, string>,
  setEnvironment?: Record<string, string>
): Promise<BashCommandResult> {
  // Validate working directory format
  if (!isValidWindowsPath(workingDirectory)) {
    throw new Error(
      'Working directory must be an absolute path in one of these formats: C:\\Foo\\Bar, /c/Foo/Bar, /c:/Foo/Bar, C:, C:\\, /c, /c/, or /c:/'
    );
  }

  // Convert working directory to Windows format for bash
  const normalizedWorkingDir = normalizeWindowsPath(workingDirectory);

  // Check if the working directory exists
  if (!fs.existsSync(normalizedWorkingDir)) {
    throw new Error(`Working directory does not exist: ${normalizedWorkingDir}`);
  }

  // Get bash path from config
  const bashPath = config.bash?.path || 'C:\\Program Files\\Git\\bin\\bash.exe';

  // Check if bash exists at startup
  if (!fs.existsSync(bashPath)) {
    throw new Error(`Bash not found at configured path: ${bashPath}`);
  }

  // Build environment variables
  const env = { ...process.env };

  // Apply setEnvironment modifications first (these completely replace existing values)
  if (setEnvironment) {
    for (const [key, value] of Object.entries(setEnvironment)) {
      env[key] = value;
    }
  }

  // Apply prepend environment modifications
  if (prependEnvironment) {
    for (const [key, value] of Object.entries(prependEnvironment)) {
      const currentValue = env[key] || '';
      env[key] = value + currentValue;
    }
  }

  // Apply append environment modifications
  if (appendEnvironment) {
    for (const [key, value] of Object.entries(appendEnvironment)) {
      const currentValue = env[key] || '';
      env[key] = currentValue + value;
    }
  }

  // Create output file
  const outputFile = generateTimestampedFilename(storageDirectory, 'log');
  const outputStream = fs.createWriteStream(outputFile);

  return new Promise((resolve, reject) => {
    // Start the bash process
    const child = spawn(bashPath, ['-c', command], {
      cwd: normalizedWorkingDir,
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true,
      env: env,
    });

    let timedOut = false;
    let resolved = false;
    const outputLines: string[] = [];

    // Helper function to resolve once and prevent double resolution
    const resolveOnce = (result: BashCommandResult) => {
      if (!resolved) {
        resolved = true;
        resolve(result);
      }
    };

    // Set up timeout with Windows-optimized process killing
    const timeout = setTimeout(() => {
      timedOut = true;
      outputStream.end();

      // Remove empty lines from the end
      while (outputLines.length > 0 && outputLines[outputLines.length - 1].trim() === '') {
        outputLines.pop();
      }

      // Get last 20 lines
      const last20Lines = outputLines.slice(-20);

      // Check if we need truncation message
      let truncationMessage: string | undefined;
      if (outputLines.length > 20) {
        const filename = path.basename(outputFile);
        truncationMessage = `Truncated output. Full output is ${outputLines.length} lines. Use \`read_output\` tool with filename "${filename}" line 0 to read more.`;
      }

      // Resolve immediately with timeout status - don't wait for process to be killed
      resolveOnce({
        output: last20Lines,
        status: `The command timed out after ${timeoutSeconds} seconds.`,
        truncationMessage,
      });

      // Try to kill the process, but don't wait for it
      if (process.platform === 'win32' && child.pid) {
        // It's tempting to try child.kill() but in practice it does NOT work on Windows.
        try {
          execSync(`taskkill /F /T /PID ${child.pid}`, { stdio: 'ignore' });
        } catch (error) {
          // Ignore errors - process might already be dead or unkillable
        }
      } else {
        // On Unix systems, try SIGTERM first, then SIGKILL
        child.kill('SIGTERM');
        setTimeout(() => {
          if (!child.killed) {
            child.kill('SIGKILL');
          }
        }, 500); // Reduced grace period to 500ms
      }
    }, timeoutSeconds * 1000);

    // Capture stdout
    child.stdout?.on('data', (data: Buffer) => {
      const text = data.toString();
      outputStream.write(text);
      const lines = text.split('\n');
      outputLines.push(...lines);
    });

    // Capture stderr
    child.stderr?.on('data', (data: Buffer) => {
      const text = data.toString();
      outputStream.write(text);
      const lines = text.split('\n');
      outputLines.push(...lines);
    });

    // Handle process completion
    child.on('close', (code: number | null) => {
      clearTimeout(timeout);
      outputStream.end();

      // Only resolve if we haven't already resolved due to timeout
      if (!resolved) {
        // Remove empty lines from the end
        while (outputLines.length > 0 && outputLines[outputLines.length - 1].trim() === '') {
          outputLines.pop();
        }

        // Determine status
        const status = timedOut ? `The command timed out after ${timeoutSeconds} seconds.` : `Exit code: ${code}`;

        // Get last 20 lines
        const last20Lines = outputLines.slice(-20);

        // Check if we need truncation message
        let truncationMessage: string | undefined;
        if (outputLines.length > 20) {
          const filename = path.basename(outputFile);
          truncationMessage = `Truncated output. Full output is ${outputLines.length} lines. Use \`read_output\` tool with filename "${filename}" line 0 to read more.`;
        }

        resolveOnce({
          output: last20Lines,
          status,
          truncationMessage,
        });
      }
    });

    // Handle process errors
    child.on('error', (error: Error) => {
      clearTimeout(timeout);
      outputStream.end();
      reject(new Error(`Failed to start bash command: ${error.message}`));
    });
  });
}

/**
 * Check if a path is a valid Windows absolute path format
 * Accepts: C:\Foo\Bar, /c/Foo/Bar, /c:/Foo/Bar, C:, C:\ or C:/, /c, /c/, /c:/
 */
function isValidWindowsPath(path: string): boolean {
  // Windows format: C:\Foo\Bar or C:\ or C: or C:/Foo/Bar or C:/
  if (/^[A-Za-z]:([\\\/].*)?$/.test(path)) {
    return true;
  }

  // Unix-style format: /c/Foo/Bar or /c/ or /c
  if (/^\/[A-Za-z](\/.*)?$/.test(path)) {
    return true;
  }

  // Unix-style format: /c:/Foo/Bar or /c:/
  if (/^\/[A-Za-z]:(\/.*)?$/.test(path)) {
    return true;
  }

  return false;
}

/**
 * Normalize a Windows path to the proper format for the working directory
 */
function normalizeWindowsPath(path: string): string {
  // If it's already in Windows format, normalize root drive
  if (/^[A-Za-z]:/.test(path)) {
    const drive = path[0].toUpperCase();
    if (path === `${path[0]}:`) {
      // Handle C: -> C:\
      return `${drive}:\\`;
    } else if (path.startsWith(`${path[0]}:/`)) {
      // Handle C:/Foo -> C:\Foo
      return `${drive}:\\${path.substring(3).replace(/\//g, '\\')}`;
    }
    return path; // Already in correct Windows format
  }

  // Convert /c to C:\
  if (/^\/[A-Za-z]$/.test(path)) {
    const drive = path[1].toUpperCase();
    return `${drive}:\\`;
  }

  // Convert /c/ to C:\
  if (/^\/[A-Za-z]\/$/.test(path)) {
    const drive = path[1].toUpperCase();
    return `${drive}:\\`;
  }

  // Convert /c/Foo/Bar to C:\Foo\Bar
  if (/^\/[A-Za-z]\//.test(path)) {
    const drive = path[1].toUpperCase();
    const rest = path.substring(3).replace(/\//g, '\\');
    return `${drive}:\\${rest}`;
  }

  // Convert /c:/ to C:\
  if (/^\/[A-Za-z]:\/$/.test(path)) {
    const drive = path[1].toUpperCase();
    return `${drive}:\\`;
  }

  // Convert /c:/Foo/Bar to C:\Foo\Bar
  if (/^\/[A-Za-z]:\//.test(path)) {
    const drive = path[1].toUpperCase();
    const rest = path.substring(4).replace(/\//g, '\\');
    return `${drive}:\\${rest}`;
  }

  // Fallback - return as-is
  return path;
}
