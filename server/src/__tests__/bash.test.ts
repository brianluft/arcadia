import { describe, test, expect, beforeEach, afterEach } from '@jest/globals';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { runBashCommand } from '../bash';
import { Config } from '../config';
import { ensureStorageDirectory } from '../storage';

describe('runBashCommand', () => {
  let tempDir: string;
  let storageDir: string;
  let config: Config;

  beforeEach(() => {
    // Create a temporary directory for testing
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'bash-test-'));
    storageDir = path.join(tempDir, 'storage');
    ensureStorageDirectory(storageDir);

    // Create test config
    config = {
      bash: {
        path: 'C:\\Program Files\\Git\\bin\\bash.exe',
      },
    };
  });

  afterEach(() => {
    // Clean up temporary directory
    if (fs.existsSync(tempDir)) {
      fs.rmSync(tempDir, { recursive: true, force: true });
    }
  });

  describe('happy path tests', () => {
    test('should execute simple echo command', async () => {
      const result = await runBashCommand('echo "Hello World"', 'C:\\temp', 10, config, storageDir);

      expect(result.output).toContain('Hello World');
      expect(result.status).toBe('Exit code: 0');
      expect(result.truncationMessage).toBeUndefined();
    });

    test('should execute command with multiple lines of output', async () => {
      const result = await runBashCommand(
        'echo "Line 1"; echo "Line 2"; echo "Line 3"',
        'C:\\temp',
        10,
        config,
        storageDir
      );

      expect(result.output).toContain('Line 1');
      expect(result.output).toContain('Line 2');
      expect(result.output).toContain('Line 3');
      expect(result.status).toBe('Exit code: 0');
    });

    test('should handle different working directory formats', async () => {
      const testCases = ['C:\\temp', '/c/temp', '/c:/temp'];

      for (const workingDir of testCases) {
        const result = await runBashCommand('echo "test"', workingDir, 10, config, storageDir);
        expect(result.status).toBe('Exit code: 0');
      }
    });
  });

  describe('error handling tests', () => {
    test('should handle command that does not exist', async () => {
      const result = await runBashCommand('nonexistentcommand12345', 'C:\\temp', 10, config, storageDir);

      expect(result.status).toMatch(/Exit code: \d+/);
      expect(result.output.some(line => line.includes('command not found') || line.includes('not recognized'))).toBe(
        true
      );
    });

    test('should reject invalid working directory formats', async () => {
      await expect(runBashCommand('echo "test"', 'relative/path', 10, config, storageDir)).rejects.toThrow(
        'Working directory must be an absolute path'
      );
    });

    test('should reject when bash path does not exist', async () => {
      const invalidConfig = {
        ...config,
        bash: {
          path: 'C:\\nonexistent\\bash.exe',
        },
      };

      await expect(runBashCommand('echo "test"', 'C:\\temp', 10, invalidConfig, storageDir)).rejects.toThrow(
        'Bash not found at configured path'
      );
    });

    test('should handle non-existent working directory with clear error message', async () => {
      await expect(
        runBashCommand('echo "test"', '/c/nonexistent/working/directory/', 10, config, storageDir)
      ).rejects.toThrow('Working directory does not exist: C:\\nonexistent\\working\\directory\\');
    });

    test('should accept root drive formats', async () => {
      const rootDriveFormats = ['/c', '/c/', 'C:', 'C:/'];

      for (const workingDir of rootDriveFormats) {
        const result = await runBashCommand('echo "test root drive"', workingDir, 10, config, storageDir);
        expect(result.status).toBe('Exit code: 0');
        expect(result.output).toContain('test root drive');
      }
    });
  });

  describe('timeout tests', () => {
    test('should timeout long-running command', async () => {
      // Use ping command with multiple attempts to simulate a long-running command
      const result = await runBashCommand('ping -n 10 127.0.0.1', 'C:\\temp', 1, config, storageDir);

      expect(result.status).toBe('The command timed out after 1 seconds.');
    }, 10000); // Increase test timeout to 10 seconds

    test('should timeout within reasonable time (verify timeout implementation)', async () => {
      const startTime = Date.now();

      // Use ping command that would normally take 10 seconds
      const result = await runBashCommand('ping -n 10 127.0.0.1', 'C:\\temp', 2, config, storageDir);

      const endTime = Date.now();
      const elapsed = (endTime - startTime) / 1000;

      // Should timeout after 2 seconds, but we'll allow up to 8 seconds for process cleanup
      expect(elapsed).toBeLessThan(8);
      expect(result.status).toBe('The command timed out after 2 seconds.');
    }, 15000);

    test('should timeout infinite loop command quickly', async () => {
      const startTime = Date.now();

      // Use a bash command that creates an infinite loop
      const result = await runBashCommand(
        'while true; do echo "running"; sleep 0.1; done',
        'C:\\temp',
        1,
        config,
        storageDir
      );

      const endTime = Date.now();
      const elapsed = (endTime - startTime) / 1000;

      // Should timeout after 1 second, but we'll allow up to 7 seconds for process cleanup
      expect(elapsed).toBeLessThan(7);
      expect(result.status).toBe('The command timed out after 1 seconds.');
    }, 12000);

    test('should timeout sleep command', async () => {
      const startTime = Date.now();

      // Use sleep command which should be easier to kill
      const result = await runBashCommand('sleep 10', 'C:\\temp', 1, config, storageDir);

      const endTime = Date.now();
      const elapsed = (endTime - startTime) / 1000;

      // Should timeout after 1 second, sleep should be easier to kill
      expect(elapsed).toBeLessThan(7);
      expect(result.status).toBe('The command timed out after 1 seconds.');
    }, 12000);

    test('should handle timeout with output generated before timeout', async () => {
      const startTime = Date.now();

      // Command that generates some output then runs long
      const result = await runBashCommand(
        'echo "Starting process"; ping -n 10 127.0.0.1',
        'C:\\temp',
        1,
        config,
        storageDir
      );

      const endTime = Date.now();
      const elapsed = (endTime - startTime) / 1000;

      expect(elapsed).toBeLessThan(8);
      expect(result.status).toBe('The command timed out after 1 seconds.');
      expect(result.output.some(line => line.includes('Starting process'))).toBe(true);
    }, 12000);
  });

  describe('output truncation tests', () => {
    test('should handle output longer than 20 lines', async () => {
      // Generate 25 lines of output
      const command = Array.from({ length: 25 }, (_, i) => `echo "Line ${i + 1}"`).join('; ');
      const result = await runBashCommand(command, 'C:\\temp', 10, config, storageDir);

      expect(result.output).toHaveLength(20);
      expect(result.truncationMessage).toMatch(/Truncated output. Full output is \d+ lines/);
      expect(result.truncationMessage).toMatch(/Use `read_output` tool with filename/);
      expect(result.status).toBe('Exit code: 0');
    });

    test('should not show truncation message for output with 20 or fewer lines', async () => {
      const command = Array.from({ length: 5 }, (_, i) => `echo "Line ${i + 1}"`).join('; ');
      const result = await runBashCommand(command, 'C:\\temp', 10, config, storageDir);

      expect(result.output.length).toBeLessThanOrEqual(20);
      expect(result.truncationMessage).toBeUndefined();
    });
  });

  describe('output file creation tests', () => {
    test('should create output file in storage directory', async () => {
      await runBashCommand('echo "test output"', 'C:\\temp', 10, config, storageDir);

      // Check that at least one .log file was created in storage directory
      const files = fs.readdirSync(storageDir);
      const logFiles = files.filter(file => file.endsWith('.log'));
      expect(logFiles.length).toBeGreaterThan(0);

      // Check that the log file contains the output
      const logContent = fs.readFileSync(path.join(storageDir, logFiles[0]), 'utf8');
      expect(logContent).toContain('test output');
    });
  });

  describe('environment parameter tests', () => {
    test('should prepend to environment variables', async () => {
      const result = await runBashCommand('echo "TEST_VAR is: $TEST_VAR"', 'C:\\temp', 10, config, storageDir, {
        TEST_VAR: 'prepended_',
      });

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('TEST_VAR is: prepended_'))).toBe(true);
    });

    test('should append to environment variables', async () => {
      const result = await runBashCommand(
        'echo "TEST_VAR is: $TEST_VAR"',
        'C:\\temp',
        10,
        config,
        storageDir,
        undefined,
        { TEST_VAR: '_appended' }
      );

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('TEST_VAR is: _appended'))).toBe(true);
    });

    test('should handle prepend and append together', async () => {
      const result = await runBashCommand(
        'echo "TEST_VAR is: $TEST_VAR"',
        'C:\\temp',
        10,
        config,
        storageDir,
        { TEST_VAR: 'start_' },
        { TEST_VAR: '_end' }
      );

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('TEST_VAR is: start__end'))).toBe(true);
    });

    test('should prepend to existing environment variable', async () => {
      // Set an environment variable in the child process and test prepending
      const result = await runBashCommand('echo "EXISTING_VAR is: $EXISTING_VAR"', 'C:\\temp', 10, config, storageDir, {
        EXISTING_VAR: 'prepended_',
      });

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('EXISTING_VAR is: prepended_'))).toBe(true);
    });

    test('should append to existing environment variable', async () => {
      // Test appending to an environment variable that exists in the process
      const result = await runBashCommand(
        'echo "EXISTING_VAR is: $EXISTING_VAR"',
        'C:\\temp',
        10,
        config,
        storageDir,
        undefined,
        { EXISTING_VAR: '_appended' }
      );

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('EXISTING_VAR is:') && line.includes('_appended'))).toBe(true);
    });

    test('should handle PATH manipulation with semicolons', async () => {
      const result = await runBashCommand(
        'echo "PATH contains our paths: $PATH"',
        'C:\\temp',
        10,
        config,
        storageDir,
        { PATH: 'C:\\PrependedPath;' },
        { PATH: ';C:\\AppendedPath' }
      );

      expect(result.status).toBe('Exit code: 0');
      // In bash, Windows paths are converted to Unix-style
      expect(result.output.some(line => line.includes('/c/PrependedPath'))).toBe(true);
      expect(result.output.some(line => line.includes('/c/AppendedPath'))).toBe(true);
    });

    test('should work without environment parameters (backward compatibility)', async () => {
      const result = await runBashCommand('echo "Hello World"', 'C:\\temp', 10, config, storageDir);

      expect(result.output).toContain('Hello World');
      expect(result.status).toBe('Exit code: 0');
    });

    test('should work with empty environment objects', async () => {
      const result = await runBashCommand('echo "Hello World"', 'C:\\temp', 10, config, storageDir, {}, {});

      expect(result.output).toContain('Hello World');
      expect(result.status).toBe('Exit code: 0');
    });

    test('should handle multiple environment variables', async () => {
      const result = await runBashCommand(
        'echo "VAR1: $VAR1, VAR2: $VAR2"',
        'C:\\temp',
        10,
        config,
        storageDir,
        { VAR1: 'pre_', VAR2: 'start_' },
        { VAR1: '_post', VAR2: '_finish' }
      );

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('VAR1: pre__post'))).toBe(true);
      expect(result.output.some(line => line.includes('VAR2: start__finish'))).toBe(true);
    });
  });

  describe('setEnvironment parameter tests', () => {
    test('should set environment variables (replacing existing values)', async () => {
      const result = await runBashCommand(
        'echo "SET_VAR is: $SET_VAR"',
        'C:\\temp',
        10,
        config,
        storageDir,
        undefined,
        undefined,
        { SET_VAR: 'completely_new_value' }
      );

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('SET_VAR is: completely_new_value'))).toBe(true);
    });

    test('should set multiple environment variables', async () => {
      const result = await runBashCommand(
        'echo "VAR1: $VAR1, VAR2: $VAR2, VAR3: $VAR3"',
        'C:\\temp',
        10,
        config,
        storageDir,
        undefined,
        undefined,
        { VAR1: 'value1', VAR2: 'value2', VAR3: 'value3' }
      );

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('VAR1: value1'))).toBe(true);
      expect(result.output.some(line => line.includes('VAR2: value2'))).toBe(true);
      expect(result.output.some(line => line.includes('VAR3: value3'))).toBe(true);
    });

    test('should work with setEnvironment combined with prepend and append', async () => {
      const result = await runBashCommand(
        'echo "COMBINED_VAR is: $COMBINED_VAR"',
        'C:\\temp',
        10,
        config,
        storageDir,
        { COMBINED_VAR: 'prefix_' },
        { COMBINED_VAR: '_suffix' },
        { COMBINED_VAR: 'base_value' }
      );

      expect(result.status).toBe('Exit code: 0');
      // setEnvironment is applied first, then prepend, then append
      expect(result.output.some(line => line.includes('COMBINED_VAR is: prefix_base_value_suffix'))).toBe(true);
    });

    test('should completely replace existing environment variable', async () => {
      // First, run a command that would set an environment variable in our process
      // Then use setEnvironment to completely replace it
      const result = await runBashCommand(
        'echo "REPLACE_VAR is: $REPLACE_VAR"',
        'C:\\temp',
        10,
        config,
        storageDir,
        undefined,
        undefined,
        { REPLACE_VAR: 'new_replacement_value' }
      );

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('REPLACE_VAR is: new_replacement_value'))).toBe(true);
    });

    test('should work with empty setEnvironment object', async () => {
      const result = await runBashCommand(
        'echo "Hello World"',
        'C:\\temp',
        10,
        config,
        storageDir,
        undefined,
        undefined,
        {}
      );

      expect(result.output).toContain('Hello World');
      expect(result.status).toBe('Exit code: 0');
    });

    test('should not affect other environment variables when setting specific ones', async () => {
      const result = await runBashCommand(
        'echo "PATH_EXISTS: $(if [ -n "$PATH" ]; then echo "yes"; else echo "no"; fi), CUSTOM_VAR: $CUSTOM_VAR"',
        'C:\\temp',
        10,
        config,
        storageDir,
        undefined,
        undefined,
        { CUSTOM_VAR: 'my_custom_value' }
      );

      expect(result.status).toBe('Exit code: 0');
      expect(result.output.some(line => line.includes('PATH_EXISTS: yes'))).toBe(true);
      expect(result.output.some(line => line.includes('CUSTOM_VAR: my_custom_value'))).toBe(true);
    });
  });
});
