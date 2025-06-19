import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import { Config } from './config.js';
import { generateTimestampedFilename } from './storage.js';

/**
 * Parameter definition for SQL commands
 */
export interface SqlParameter {
  type: string;
  value: any;
}

/**
 * Input structure for the C# database program
 */
export interface DatabaseInput {
  provider: string;
  connectionString: string;
  query: string;
  parameters?: Record<string, SqlParameter>;
  commandType: string;
  timeoutSeconds: number;
  skipRows: number;
  takeRows: number;
}

/**
 * Result of running a database command
 */
export interface DatabaseCommandResult {
  /** Raw JSON output from database */
  output: string;
  /** Status message */
  status: string;
  /** Optional truncation message if output was truncated */
  truncationMessage?: string;
}

/**
 * Execute a SQL command via the C# database program
 * @param input - Database command input
 * @param config - Server configuration
 * @param storageDirectory - Storage directory for temp files
 * @param serverDirectory - Directory where the server script is located
 * @returns Promise<DatabaseCommandResult>
 */
export async function runDatabaseCommand(
  input: DatabaseInput,
  config: Config,
  storageDirectory: string,
  serverDirectory: string
): Promise<DatabaseCommandResult> {
  // Determine path to the C# database program
  const databaseProgramPath = path.join(serverDirectory, '..', 'database', 'Database.exe');

  // Check if the database program exists
  if (!fs.existsSync(databaseProgramPath)) {
    throw new Error(`Database program not found at: ${databaseProgramPath}`);
  }

  // Create temporary input file
  const inputFile = generateTimestampedFilename(storageDirectory, 'db_input.json');
  const outputFile = generateTimestampedFilename(storageDirectory, 'db_output.json');

  try {
    // Write input JSON to file
    fs.writeFileSync(inputFile, JSON.stringify(input, null, 2));

    // Run the C# database program
    const result = await executeDatabaseProgram(databaseProgramPath, inputFile, outputFile);

    // Read the output file
    let outputData = '';
    if (fs.existsSync(outputFile)) {
      outputData = fs.readFileSync(outputFile, 'utf8');
    }

    // Store full output to permanent file for paging
    const permanentOutputFile = generateTimestampedFilename(storageDirectory, 'log');
    fs.writeFileSync(permanentOutputFile, outputData);

    // Apply output truncation (10,000 characters max for database tools)
    const truncationResult = truncateOutput(outputData, 10000);

    // Build truncation message if needed
    let truncationMessage: string | undefined;
    if (truncationResult.truncated) {
      const filename = path.basename(permanentOutputFile);
      const totalLines = outputData.split('\n').length;
      const shownLines = truncationResult.output.split('\n').length;
      const linesLeft = totalLines - shownLines;
      truncationMessage = `Truncated output. Showing ${shownLines} lines, ${linesLeft} lines left. Use \`read_output\` tool with filename "${filename}" line ${shownLines} to continue paging.`;
    }

    return {
      output: truncationResult.output,
      status: result.status,
      truncationMessage,
    };
  } finally {
    // Clean up temporary files
    try {
      if (fs.existsSync(inputFile)) {
        fs.unlinkSync(inputFile);
      }
      if (fs.existsSync(outputFile)) {
        fs.unlinkSync(outputFile);
      }
    } catch (error) {
      // Ignore cleanup errors
    }
  }
}

/**
 * Execute the C# database program with input and output files
 * @param programPath - Path to the database program executable
 * @param inputFile - Path to input JSON file
 * @param outputFile - Path to output JSON file
 * @returns Promise with execution result
 */
async function executeDatabaseProgram(
  programPath: string,
  inputFile: string,
  outputFile: string
): Promise<{ status: string }> {
  return new Promise((resolve, reject) => {
    const args = ['--input', inputFile, '--output', outputFile];

    const child = spawn(programPath, args, {
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true,
    });

    let stderrOutput = '';

    // Capture stderr for error messages
    child.stderr?.on('data', (data: Buffer) => {
      stderrOutput += data.toString();
    });

    // Handle process completion
    child.on('close', (code: number | null) => {
      if (code === 0) {
        // Success - program prints nothing to stdout and exits 0
        resolve({
          status: 'Database command completed successfully',
        });
      } else {
        // Failure - program prints error to stderr and exits nonzero
        const errorMessage = stderrOutput.trim() || `Database program exited with code ${code}`;
        reject(new Error(errorMessage));
      }
    });

    // Handle process errors
    child.on('error', (error: Error) => {
      reject(new Error(`Failed to start database program: ${error.message}`));
    });
  });
}

/**
 * Truncate output to a maximum number of characters, keeping complete lines
 * @param output - The output string to truncate
 * @param maxCharacters - Maximum number of characters to return
 * @returns Object with truncated output and truncation flag
 */
function truncateOutput(output: string, maxCharacters: number): { output: string; truncated: boolean } {
  if (output.length <= maxCharacters) {
    return { output, truncated: false };
  }

  const lines = output.split('\n');
  let charCount = 0;
  let lineCount = 0;

  for (const line of lines) {
    const lineLength = line.length + 1; // +1 for newline character
    if (charCount + lineLength > maxCharacters) {
      break;
    }
    charCount += lineLength;
    lineCount++;
  }

  const truncatedOutput = lines.slice(0, lineCount).join('\n');
  return { output: truncatedOutput, truncated: true };
}

/**
 * Get SQL Server connection string by name from config
 * @param connectionName - Name of the connection in config
 * @param config - Server configuration
 * @returns Connection string
 */
export function getSqlServerConnectionString(connectionName: string, config: Config): string {
  const connections = config.connections?.sqlServer;
  if (!connections || !connections[connectionName]) {
    throw new Error(`SQL Server connection '${connectionName}' not found in configuration`);
  }
  return connections[connectionName];
}

/**
 * Create database input for SQLite
 * @param sqliteFilePath - Absolute path to SQLite database file
 * @param query - SQL query to execute
 * @param parameters - Optional named parameters
 * @param timeoutSeconds - Query timeout in seconds
 * @param skipRows - Number of rows to skip
 * @param takeRows - Maximum number of rows to return
 * @returns DatabaseInput object
 */
export function createSqliteInput(
  sqliteFilePath: string,
  query: string,
  parameters?: Record<string, SqlParameter>,
  timeoutSeconds: number = 30,
  skipRows: number = 0,
  takeRows: number = 1000
): DatabaseInput {
  return {
    provider: 'Microsoft.Data.Sqlite',
    connectionString: `Data Source=${sqliteFilePath}`,
    query,
    parameters,
    commandType: 'Text',
    timeoutSeconds,
    skipRows,
    takeRows,
  };
}

/**
 * Create database input for SQL Server
 * @param connectionString - SQL Server connection string
 * @param query - SQL query to execute
 * @param parameters - Optional named parameters
 * @param timeoutSeconds - Query timeout in seconds
 * @param skipRows - Number of rows to skip
 * @param takeRows - Maximum number of rows to return
 * @returns DatabaseInput object
 */
export function createSqlServerInput(
  connectionString: string,
  query: string,
  parameters?: Record<string, SqlParameter>,
  timeoutSeconds: number = 30,
  skipRows: number = 0,
  takeRows: number = 1000
): DatabaseInput {
  return {
    provider: 'Microsoft.Data.SqlClient',
    connectionString,
    query,
    parameters,
    commandType: 'Text',
    timeoutSeconds,
    skipRows,
    takeRows,
  };
}
