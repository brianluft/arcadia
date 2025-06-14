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
    fs.unlinkSync(testFilePath);
  } catch (testError) {
    throw new Error(`Storage directory is not writable: ${storageDir}. Error: ${testError}`);
  }

  return path.resolve(storageDir);
}
