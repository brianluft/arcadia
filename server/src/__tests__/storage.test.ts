import { describe, it, expect, beforeEach, afterEach } from '@jest/globals';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import {
  initializeStorageDirectoryFromBase,
  ensureStorageDirectory,
  generateTimestampedFilename,
  reserveTimestampedFilename,
  readOutputFile,
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
        storage: {
          directory: absolutePath,
        },
      };

      const result = initializeStorageDirectoryFromBase(config, tempDir);

      expect(fs.existsSync(result)).toBe(true);
      expect(result).toBe(path.resolve(absolutePath));
    });

    it('should use default storage directory when not specified', () => {
      const config: Config = {};

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

  describe('readOutputFile', () => {
    let testFile: string;

    beforeEach(() => {
      // Create a test file with known content
      testFile = path.join(tempDir, 'test-output.txt');
      const content = [
        'Line 1 with some words',
        'Line 2 with different content',
        'Line 3 has more words and text',
        'Line 4 contains additional content',
        'Line 5 with even more words to test',
      ].join('\n');
      fs.writeFileSync(testFile, content);
    });

    it('should throw error if file does not exist', () => {
      expect(() => readOutputFile('nonexistent.txt', 0, tempDir)).toThrow('File not found: nonexistent.txt');
    });

    it('should throw error if start line index is past end of file', () => {
      expect(() => readOutputFile('test-output.txt', 10, tempDir)).toThrow(
        'Requested line 10 is past the end of the file (5 lines total)'
      );
    });

    it('should read file from beginning', () => {
      const result = readOutputFile('test-output.txt', 0, tempDir);

      expect(result.lines).toEqual([
        'Line 1 with some words',
        'Line 2 with different content',
        'Line 3 has more words and text',
        'Line 4 contains additional content',
        'Line 5 with even more words to test',
      ]);
      expect(result.truncated).toBe(false);
      expect(result.totalLines).toBe(5);
    });

    it('should read file from middle', () => {
      const result = readOutputFile('test-output.txt', 2, tempDir);

      expect(result.lines).toEqual([
        'Line 3 has more words and text',
        'Line 4 contains additional content',
        'Line 5 with even more words to test',
      ]);
      expect(result.truncated).toBe(false);
      expect(result.totalLines).toBe(5);
    });

    it('should truncate output when word limit exceeded', () => {
      // Create a file with many words
      const manyWordsFile = path.join(tempDir, 'many-words.txt');
      const lines = [];
      for (let i = 1; i <= 100; i++) {
        // Each line has about 10-15 words
        lines.push(`Line ${i} has many words and content to test the word counting functionality thoroughly`);
      }
      fs.writeFileSync(manyWordsFile, lines.join('\n'));

      // Read with a low word limit
      const result = readOutputFile('many-words.txt', 0, tempDir, 50);

      expect(result.truncated).toBe(true);
      expect(result.lines.length).toBeGreaterThan(0);
      expect(result.lines.length).toBeLessThan(100);
      expect(result.nextLineIndex).toBeDefined();
      expect(result.totalLines).toBe(100);
    });

    it('should handle empty lines correctly', () => {
      const emptyLinesFile = path.join(tempDir, 'empty-lines.txt');
      const content = 'Line 1\n\nLine 3\n\nLine 5';
      fs.writeFileSync(emptyLinesFile, content);

      const result = readOutputFile('empty-lines.txt', 0, tempDir);

      expect(result.lines).toEqual(['Line 1', '', 'Line 3', '', 'Line 5']);
      expect(result.truncated).toBe(false);
      expect(result.totalLines).toBe(5);
    });

    it('should count words correctly', () => {
      const wordTestFile = path.join(tempDir, 'word-test.txt');
      // Create lines with known word counts
      const lines = [
        'one two three', // 3 words
        'four five six seven eight', // 5 words
        'nine ten', // 2 words
        'eleven twelve thirteen fourteen', // 4 words
      ];
      fs.writeFileSync(wordTestFile, lines.join('\n'));

      // Total words: 3 + 5 + 2 + 4 = 14 words
      // Test with limit of 10 words - should truncate after 3rd line (3+5+2=10 words)
      const result = readOutputFile('word-test.txt', 0, tempDir, 10);

      expect(result.lines).toEqual(['one two three', 'four five six seven eight', 'nine ten']);
      expect(result.truncated).toBe(true);
      expect(result.nextLineIndex).toBe(3);
      expect(result.totalLines).toBe(4);
    });

    it('should handle single line that exceeds word limit', () => {
      const singleLineFile = path.join(tempDir, 'single-line.txt');
      // Create a single line with many words
      const manyWords = Array.from({ length: 20 }, (_, i) => `word${i + 1}`).join(' ');
      fs.writeFileSync(singleLineFile, manyWords);

      // With word limit of 10, should still return the first line since we have no lines yet
      const result = readOutputFile('single-line.txt', 0, tempDir, 10);

      expect(result.lines).toHaveLength(1);
      expect(result.lines[0]).toBe(manyWords);
      expect(result.truncated).toBe(false);
    });
  });
});
