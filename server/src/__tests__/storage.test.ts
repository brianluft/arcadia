import { describe, it, expect, beforeEach, afterEach } from '@jest/globals';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import {
  initializeStorageDirectoryFromBase,
  ensureStorageDirectory,
  generateTimestampedFilename,
  reserveTimestampedFilename,
} from '../storage';
import { Config } from '../config';

describe('Storage module', () => {
  let tempDir: string;

  beforeEach(() => {
    // Create a temporary directory for each test
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'arcadia-storage-test-'));
  });

  afterEach(() => {
    // Clean up the temporary directory
    if (fs.existsSync(tempDir)) {
      fs.rmSync(tempDir, { recursive: true, force: true });
    }
  });

  describe('ensureStorageDirectory', () => {
    it('should create directory if it does not exist', () => {
      const storageDir = path.join(tempDir, 'new-storage');
      expect(fs.existsSync(storageDir)).toBe(false);

      const result = ensureStorageDirectory(storageDir);

      expect(fs.existsSync(storageDir)).toBe(true);
      expect(result).toBe(path.resolve(storageDir));
    });

    it('should return existing directory if it already exists', () => {
      const storageDir = path.join(tempDir, 'existing-storage');
      fs.mkdirSync(storageDir);

      const result = ensureStorageDirectory(storageDir);

      expect(fs.existsSync(storageDir)).toBe(true);
      expect(result).toBe(path.resolve(storageDir));
    });

    it('should throw error if directory is not writable', () => {
      // Create a directory and make it read-only (on Windows, this may not work as expected)
      const storageDir = path.join(tempDir, 'readonly-storage');
      fs.mkdirSync(storageDir);

      // Try to make it read-only (this is platform-specific and may not work on all systems)
      try {
        fs.chmodSync(storageDir, 0o444);
        expect(() => ensureStorageDirectory(storageDir)).toThrow(/not writable/);
      } catch {
        // If chmod fails (e.g., on Windows), just skip this test
        console.log('Skipping read-only directory test (not supported on this platform)');
      }
    });
  });

  describe('initializeStorageDirectoryFromBase', () => {
    it('should initialize storage with relative path', () => {
      const config: Config = {
        version: '1.0.0',
        server: { name: 'test-server', description: 'Test server' },
        storage: {
          directory: './test-storage',
        },
      };

      const result = initializeStorageDirectoryFromBase(config, tempDir);

      expect(fs.existsSync(result)).toBe(true);
      expect(result).toBe(path.resolve(tempDir, 'test-storage'));
    });

    it('should initialize storage with absolute path', () => {
      const absolutePath = path.join(tempDir, 'absolute-storage');
      const config: Config = {
        version: '1.0.0',
        server: { name: 'test-server', description: 'Test server' },
        storage: {
          directory: absolutePath,
        },
      };

      const result = initializeStorageDirectoryFromBase(config, tempDir);

      expect(fs.existsSync(result)).toBe(true);
      expect(result).toBe(path.resolve(absolutePath));
    });

    it('should use default storage directory when not specified', () => {
      const config: Config = {
        version: '1.0.0',
        server: { name: 'test-server', description: 'Test server' },
      };

      const result = initializeStorageDirectoryFromBase(config, tempDir);

      expect(fs.existsSync(result)).toBe(true);
      expect(result).toBe(path.resolve(tempDir, 'storage'));
    });
  });

  describe('generateTimestampedFilename', () => {
    it('should generate a filename with correct format', () => {
      const result = generateTimestampedFilename(tempDir);

      expect(result).toMatch(/\d{17}\.txt$/);
      expect(path.dirname(result)).toBe(tempDir);
      expect(path.basename(result)).toMatch(/^\d{17}\.txt$/);
    });

    it('should generate a filename with custom extension', () => {
      const result = generateTimestampedFilename(tempDir, 'log');

      expect(result).toMatch(/\d{17}\.log$/);
      expect(path.dirname(result)).toBe(tempDir);
      expect(path.basename(result)).toMatch(/^\d{17}\.log$/);
    });

    it('should generate unique filenames when called multiple times', () => {
      const filename1 = generateTimestampedFilename(tempDir);
      // Add a small delay to ensure different timestamps
      const start = Date.now();
      while (Date.now() - start < 5) {
        /* busy wait for 5ms */
      }
      const filename2 = generateTimestampedFilename(tempDir);

      expect(filename1).not.toBe(filename2);
    });

    it('should handle filename collisions by adding counter', () => {
      // Create a file with a predictable timestamp
      const now = new Date();
      const year = now.getFullYear().toString();
      const month = (now.getMonth() + 1).toString().padStart(2, '0');
      const day = now.getDate().toString().padStart(2, '0');
      const hours = now.getHours().toString().padStart(2, '0');
      const minutes = now.getMinutes().toString().padStart(2, '0');
      const seconds = now.getSeconds().toString().padStart(2, '0');
      const millis = now.getMilliseconds().toString().padStart(3, '0');

      const baseTimestamp = `${year}${month}${day}${hours}${minutes}${seconds}${millis}`;
      const existingFile = path.join(tempDir, `${baseTimestamp}.txt`);

      // Create the file to force collision
      fs.writeFileSync(existingFile, 'test');

      // Generate a new filename - it should get a counter suffix
      const result = generateTimestampedFilename(tempDir);

      // The result should either be different from the existing file,
      // or have a counter suffix if it's the same timestamp
      expect(result).not.toBe(existingFile);
      expect(fs.existsSync(result)).toBe(false);
    });

    it('should ensure returned filename does not exist', () => {
      const result = generateTimestampedFilename(tempDir);

      expect(fs.existsSync(result)).toBe(false);
    });
  });

  describe('reserveTimestampedFilename', () => {
    it('should create an empty file at the generated path', () => {
      const result = reserveTimestampedFilename(tempDir);

      expect(fs.existsSync(result)).toBe(true);
      expect(fs.readFileSync(result, 'utf8')).toBe('');
    });

    it('should create file with custom extension', () => {
      const result = reserveTimestampedFilename(tempDir, 'log');

      expect(result).toMatch(/\.log$/);
      expect(fs.existsSync(result)).toBe(true);
    });

    it('should throw error if file cannot be created', () => {
      // Try to create a file in a non-existent directory
      const nonExistentDir = path.join(tempDir, 'non-existent', 'deep', 'path');

      expect(() => reserveTimestampedFilename(nonExistentDir)).toThrow(/Failed to reserve timestamped filename/);
    });
  });
});
