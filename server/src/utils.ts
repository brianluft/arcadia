/**
 * Utility functions for the Arcadia MCP server
 */

/**
 * Validate that a string is not empty or whitespace only
 * @param value - The string to validate
 * @returns true if the string is valid (not empty and not just whitespace)
 */
export function isValidString(value: string): boolean {
  return value.trim().length > 0;
}

/**
 * Sanitize a filename by removing invalid characters
 * @param filename - The filename to sanitize
 * @returns Sanitized filename safe for filesystem use
 */
export function sanitizeFilename(filename: string): string {
  // Remove invalid characters for Windows/Unix filesystems
  return filename.replace(/[<>:"/\\|?*]/g, '_').trim();
}

/**
 * Create a simple message response structure
 * @param message - The message content
 * @param success - Whether the operation was successful
 * @returns Response object with message and success status
 */
export function createResponse(message: string, success: boolean = true): { message: string; success: boolean } {
  return {
    message: message.trim(),
    success,
  };
}

/**
 * Parse a simple key-value string (e.g., "key=value")
 * @param input - The input string to parse
 * @returns Object with key and value, or null if invalid format
 */
export function parseKeyValue(input: string): { key: string; value: string } | null {
  const trimmed = input.trim();
  const equalIndex = trimmed.indexOf('=');

  if (equalIndex === -1 || equalIndex === 0 || equalIndex === trimmed.length - 1) {
    return null;
  }

  const key = trimmed.substring(0, equalIndex).trim();
  const value = trimmed.substring(equalIndex + 1).trim();

  if (key.length === 0 || value.length === 0) {
    return null;
  }

  return { key, value };
}

/**
 * Normalize a file path to Windows format, handling various path formats including MSYS-style paths
 * @param filePath - The file path to normalize
 * @returns Normalized Windows path
 */
export function normalizePath(filePath: string): string {
  // URL decode the path first (handles cases like /c%3A/Projects/...)
  let normalizedPath = decodeURIComponent(filePath);

  // Handle MSYS-style paths like /c/foo/bar or /c:/foo/bar
  if (normalizedPath.match(/^\/[a-zA-Z](\:|\/)/)) {
    const driveLetter = normalizedPath.charAt(1).toUpperCase();
    if (normalizedPath.charAt(2) === ':') {
      // Format: /c:/foo/bar -> C:/foo/bar
      normalizedPath = `${driveLetter}:${normalizedPath.substring(3)}`;
    } else {
      // Format: /c/foo/bar -> C:/foo/bar
      normalizedPath = `${driveLetter}:${normalizedPath.substring(2)}`;
    }
  }

  // Convert forward slashes to backslashes for Windows
  normalizedPath = normalizedPath.replace(/\//g, '\\');

  return normalizedPath;
}
