import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { parse as parseJsonc } from 'jsonc-parser';

/**
 * Configuration interface for the Arcadia MCP server
 */
export interface Config {
  storage?: {
    directory?: string;
  };
  bash?: {
    path?: string;
  };
  apiKeys?: {
    openai?: string | null;
  };
  connections?: {
    sqlServer?: {
      [connectionName: string]: string;
    };
  };
  computerUse?: {
    enable?: boolean;
  };
}

/**
 * Load configuration from the parent directory of the current module
 * @returns Parsed configuration object
 * @throws Error if config file is not found or invalid
 */
export function loadConfig(): Config {
  return loadConfigFromModuleUrl(import.meta.url);
}

/**
 * Load configuration from a specific directory (dependency injection pattern)
 * @param currentModuleDir - The directory of the calling module
 * @returns Parsed configuration object
 * @throws Error if config file is not found or invalid
 */
export function loadConfigFromDirectory(currentModuleDir: string): Config {
  try {
    // Check for environment variable override first
    const envConfigPath = process.env.ARCADIA_CONFIG_FILE;
    if (envConfigPath) {
      return loadConfigFromPath(envConfigPath);
    }

    // Config file is in the parent directory of the running JS file
    const configPath = path.join(currentModuleDir, '..', 'config.jsonc');
    return loadConfigFromPath(configPath);
  } catch (error) {
    throw new Error(`Failed to load configuration: ${error}`);
  }
}

/**
 * Load configuration from a specific module URL
 * @param currentModuleUrl - The import.meta.url of the calling module
 * @returns Parsed configuration object
 * @throws Error if config file is not found or invalid
 */
export function loadConfigFromModuleUrl(currentModuleUrl: string): Config {
  try {
    // Check for environment variable override first
    const envConfigPath = process.env.ARCADIA_CONFIG_FILE;
    if (envConfigPath) {
      return loadConfigFromPath(envConfigPath);
    }

    // Get the directory of the current file
    const __filename = fileURLToPath(currentModuleUrl);
    const __dirname = path.dirname(__filename);

    // Config file is in the parent directory of the running JS file
    const configPath = path.join(__dirname, '..', 'config.jsonc');

    return loadConfigFromPath(configPath);
  } catch (error) {
    throw new Error(`Failed to load configuration: ${error}`);
  }
}

/**
 * Load configuration from a specific file path (useful for testing)
 * @param configPath - Path to the configuration file
 * @returns Parsed configuration object
 * @throws Error if config file is not found or invalid
 */
export function loadConfigFromPath(configPath: string): Config {
  try {
    if (!fs.existsSync(configPath)) {
      throw new Error(`Configuration file not found at: ${configPath}`);
    }

    const configData = fs.readFileSync(configPath, 'utf8');
    const config = parseJsonc(configData) as Config;

    return config;
  } catch (error) {
    throw new Error(`Failed to load configuration from ${configPath}: ${error}`);
  }
}
