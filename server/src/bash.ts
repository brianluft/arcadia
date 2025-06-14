import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
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
 * @returns Promise<BashCommandResult>
 */
export async function runBashCommand(
  command: string,
  workingDirectory: string,
  timeoutSeconds: number,
  config: Config,
  storageDirectory: string
): Promise<BashCommandResult> {
  // Validate working directory format
  if (!isValidWindowsPath(workingDirectory)) {
    throw new Error(
      'Working directory must be an absolute path in one of these formats: C:\\Foo\\Bar, /c/Foo/Bar, or /c:/Foo/Bar'
    );
  }

  // Get bash path from config
  const bashPath = config.bash?.path || 'C:\\Program Files\\Git\\bin\\bash.exe';

  // Check if bash exists at startup
  if (!fs.existsSync(bashPath)) {
    throw new Error(`Bash not found at configured path: ${bashPath}`);
  }

  // Convert working directory to Windows format for bash
  const normalizedWorkingDir = normalizeWindowsPath(workingDirectory);

  // Create output file
  const outputFile = generateTimestampedFilename(storageDirectory, 'log');
  const outputStream = fs.createWriteStream(outputFile);

  return new Promise((resolve, reject) => {
    // Start the bash process
    const child = spawn(bashPath, ['-c', command], {
      cwd: normalizedWorkingDir,
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true,
    });

    let timedOut = false;
    const outputLines: string[] = [];

    // Set up timeout
    const timeout = setTimeout(() => {
      timedOut = true;
      child.kill('SIGTERM');
      setTimeout(() => {
        if (!child.killed) {
          child.kill('SIGKILL');
        }
      }, 5000); // Give it 5 seconds to terminate gracefully
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

      resolve({
        output: last20Lines,
        status,
        truncationMessage,
      });
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
 * Accepts: C:\Foo\Bar, /c/Foo/Bar, /c:/Foo/Bar
 */
function isValidWindowsPath(path: string): boolean {
  // Windows format: C:\Foo\Bar
  if (/^[A-Za-z]:\\/.test(path)) {
    return true;
  }

  // Unix-style format: /c/Foo/Bar
  if (/^\/[A-Za-z]\//.test(path)) {
    return true;
  }

  // Unix-style format: /c:/Foo/Bar
  if (/^\/[A-Za-z]:\//.test(path)) {
    return true;
  }

  return false;
}

/**
 * Normalize a Windows path to the proper format for the working directory
 */
function normalizeWindowsPath(path: string): string {
  // If it's already in Windows format, return as-is
  if (/^[A-Za-z]:\\/.test(path)) {
    return path;
  }

  // Convert /c/Foo/Bar to C:\Foo\Bar
  if (/^\/[A-Za-z]\//.test(path)) {
    const drive = path[1].toUpperCase();
    const rest = path.substring(3).replace(/\//g, '\\');
    return `${drive}:\\${rest}`;
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
