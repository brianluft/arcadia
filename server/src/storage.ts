import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { Config } from './config.js';

/**
 * Initialize storage directory based on configuration
 * @param config - Configuration object containing storage settings
 * @returns Absolute path to the initialized storage directory
 * @throws Error if storage directory cannot be created or is not writable
 */
export function initializeStorageDirectory(config: Config): string {
  return initializeStorageDirectoryFromModuleUrl(config, import.meta.url);
}

/**
 * Initialize storage directory from a specific directory (dependency injection pattern)
 * @param config - Configuration object containing storage settings
 * @param currentModuleDir - The directory of the calling module
 * @returns Absolute path to the initialized storage directory
 * @throws Error if storage directory cannot be created or is not writable
 */
export function initializeStorageDirectoryFromDirectory(config: Config, currentModuleDir: string): string {
  try {
    const configDir = path.join(currentModuleDir, '..');
    return initializeStorageDirectoryFromBase(config, configDir);
  } catch (error) {
    throw new Error(`Failed to initialize storage directory: ${error}`);
  }
}

/**
 * Initialize storage directory from a specific module URL
 * @param config - Configuration object containing storage settings
 * @param currentModuleUrl - The import.meta.url of the calling module (for resolving relative paths)
 * @returns Absolute path to the initialized storage directory
 * @throws Error if storage directory cannot be created or is not writable
 */
export function initializeStorageDirectoryFromModuleUrl(config: Config, currentModuleUrl: string): string {
  try {
    // Get the directory of the current file
    const __filename = fileURLToPath(currentModuleUrl);
    const __dirname = path.dirname(__filename);
    const configDir = path.join(__dirname, '..');

    return initializeStorageDirectoryFromBase(config, configDir);
  } catch (error) {
    throw new Error(`Failed to initialize storage directory: ${error}`);
  }
}

/**
 * Initialize storage directory from a specific base directory (useful for testing)
 * @param config - Configuration object containing storage settings
 * @param baseDir - Base directory to resolve relative storage paths from
 * @returns Absolute path to the initialized storage directory
 * @throws Error if storage directory cannot be created or is not writable
 */
export function initializeStorageDirectoryFromBase(config: Config, baseDir: string): string {
  try {
    // Get storage directory from config, default to './storage/'
    const storageDir = config.storage?.directory || './storage/';

    // Resolve path relative to base directory if not absolute
    const resolvedStorageDir = path.isAbsolute(storageDir) ? storageDir : path.join(baseDir, storageDir);

    return ensureStorageDirectory(resolvedStorageDir);
  } catch (error) {
    throw new Error(`Failed to initialize storage directory from base ${baseDir}: ${error}`);
  }
}

/**
 * Ensure a storage directory exists and is writable
 * @param storageDir - Path to the storage directory
 * @returns Absolute path to the storage directory
 * @throws Error if directory cannot be created or is not writable
 */
export function ensureStorageDirectory(storageDir: string): string {
  // Create the directory if it doesn't exist
  if (!fs.existsSync(storageDir)) {
    fs.mkdirSync(storageDir, { recursive: true });
  }

  // Test write and delete a file to verify directory is writable
  const testFilePath = path.join(storageDir, '.test-write-access');
  try {
    fs.writeFileSync(testFilePath, 'test');

    // Try to delete the test file with retry logic for Windows
    let deleteSuccess = false;
    let lastError: Error | null = null;

    for (let attempt = 0; attempt < 3; attempt++) {
      try {
        fs.unlinkSync(testFilePath);
        deleteSuccess = true;
        break;
      } catch (unlinkError) {
        lastError = unlinkError as Error;
        // On Windows, file deletion might be delayed due to antivirus or filesystem issues
        // Wait a bit and retry
        if (attempt < 2) {
          const delay = Math.pow(2, attempt) * 100; // 100ms, 200ms
          // Simple synchronous delay
          const start = Date.now();
          while (Date.now() - start < delay) {
            // Busy wait
          }
        }
      }
    }

    if (!deleteSuccess && lastError) {
      // If we can't delete the test file, check if it's just a permission issue
      // but the directory is still writable by trying a different approach
      try {
        // Try to overwrite the existing test file
        fs.writeFileSync(testFilePath, 'test2');
        console.error(
          `Warning: Could not delete test file ${testFilePath}, but directory appears writable. Continuing...`
        );
      } catch (overwriteError) {
        throw new Error(
          `Storage directory is not writable: ${storageDir}. Delete error: ${lastError}. Overwrite error: ${overwriteError}`
        );
      }
    }
  } catch (testError) {
    throw new Error(`Storage directory is not writable: ${storageDir}. Error: ${testError}`);
  }

  return path.resolve(storageDir);
}

/**
 * Generate a unique timestamped filename in the storage directory
 * Creates a dense numeric ID with date and time information encoded
 * Format: YYYYMMDDHHMMSS followed by milliseconds and a counter if needed
 * @param storageDir - Absolute path to the storage directory
 * @param extension - File extension (without dot), defaults to 'txt'
 * @returns Full path to a guaranteed-unique file that doesn't exist
 */
export function generateTimestampedFilename(storageDir: string, extension: string = 'txt'): string {
  const now = new Date();

  // Create base timestamp: YYYYMMDDHHMMSS + milliseconds (3 digits)
  const year = now.getFullYear().toString();
  const month = (now.getMonth() + 1).toString().padStart(2, '0');
  const day = now.getDate().toString().padStart(2, '0');
  const hours = now.getHours().toString().padStart(2, '0');
  const minutes = now.getMinutes().toString().padStart(2, '0');
  const seconds = now.getSeconds().toString().padStart(2, '0');
  const millis = now.getMilliseconds().toString().padStart(3, '0');

  const baseTimestamp = `${year}${month}${day}${hours}${minutes}${seconds}${millis}`;

  // Try the base filename first
  let filename = `${baseTimestamp}.${extension}`;
  let fullPath = path.join(storageDir, filename);

  // If file exists, add a counter suffix
  let counter = 1;
  while (fs.existsSync(fullPath)) {
    filename = `${baseTimestamp}_${counter.toString().padStart(3, '0')}.${extension}`;
    fullPath = path.join(storageDir, filename);
    counter++;

    // Safety check to prevent infinite loop (though very unlikely)
    if (counter > 999) {
      throw new Error(`Unable to generate unique filename after 999 attempts for timestamp ${baseTimestamp}`);
    }
  }

  return fullPath;
}

/**
 * Generate a unique timestamped filename and immediately create an empty file
 * This ensures the filename is reserved and prevents race conditions
 * @param storageDir - Absolute path to the storage directory
 * @param extension - File extension (without dot), defaults to 'txt'
 * @returns Full path to the created file
 * @throws Error if file cannot be created
 */
export function reserveTimestampedFilename(storageDir: string, extension: string = 'txt'): string {
  const fullPath = generateTimestampedFilename(storageDir, extension);

  try {
    // Create an empty file to reserve the filename
    fs.writeFileSync(fullPath, '');
    return fullPath;
  } catch (error) {
    throw new Error(`Failed to reserve timestamped filename ${fullPath}: ${error}`);
  }
}

/**
 * Read output file with word count limiting and pagination
 * @param filename - The filename to read (just the filename, not full path)
 * @param startLineIndex - Zero-based line index to start reading from
 * @param storageDir - Absolute path to the storage directory
 * @param maxWords - Maximum number of words to return (default: 1000)
 * @returns Object with lines array and optional truncation info
 */
export function readOutputFile(
  filename: string,
  startLineIndex: number,
  storageDir: string,
  maxWords: number = 1000
): { lines: string[]; truncated?: boolean; totalLines?: number; nextLineIndex?: number } {
  const fullPath = path.join(storageDir, filename);

  // Check if file exists
  if (!fs.existsSync(fullPath)) {
    throw new Error(`File not found: ${filename}`);
  }

  // Read all lines from the file
  const fileContent = fs.readFileSync(fullPath, 'utf8');
  const allLines = fileContent.split('\n');

  // Check if start line index is valid
  if (startLineIndex >= allLines.length) {
    throw new Error(`Requested line ${startLineIndex} is past the end of the file (${allLines.length} lines total)`);
  }

  // Start reading from the specified line
  const resultLines: string[] = [];
  let wordCount = 0;
  let currentLineIndex = startLineIndex;

  for (let i = startLineIndex; i < allLines.length; i++) {
    const line = allLines[i];

    // Count words in this line (split by whitespace and filter out empty strings)
    const wordsInLine = line.split(/\s+/).filter(word => word.length > 0).length;

    // Check if adding this line would exceed the word limit
    if (wordCount + wordsInLine > maxWords && resultLines.length > 0) {
      // We need to truncate here
      const linesLeft = allLines.length - i;
      return {
        lines: resultLines,
        truncated: true,
        totalLines: allLines.length,
        nextLineIndex: i,
      };
    }

    // Add the line and update word count
    resultLines.push(line);
    wordCount += wordsInLine;
    currentLineIndex = i + 1;
  }

  // If we got here, we read all remaining lines without hitting the word limit
  return {
    lines: resultLines,
    truncated: false,
    totalLines: allLines.length,
  };
}
