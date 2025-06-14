import { describe, it, expect } from '@jest/globals';
import { isValidString, sanitizeFilename, createResponse, parseKeyValue } from '../utils';

describe('Utils module', () => {
  describe('isValidString', () => {
    it('should return true for valid non-empty strings', () => {
      expect(isValidString('hello')).toBe(true);
      expect(isValidString('world')).toBe(true);
      expect(isValidString('test string')).toBe(true);
    });

    it('should return false for empty or whitespace strings', () => {
      expect(isValidString('')).toBe(false);
      expect(isValidString('   ')).toBe(false);
      expect(isValidString('\t\n')).toBe(false);
    });

    it('should return true for strings with content after trimming', () => {
      expect(isValidString('  hello  ')).toBe(true);
      expect(isValidString('\tworld\n')).toBe(true);
    });
  });

  describe('sanitizeFilename', () => {
    it('should remove invalid filesystem characters', () => {
      expect(sanitizeFilename('file<name>test')).toBe('file_name_test');
      expect(sanitizeFilename('file:name|test')).toBe('file_name_test');
      expect(sanitizeFilename('file/name\\test')).toBe('file_name_test');
      expect(sanitizeFilename('file"name?test*')).toBe('file_name_test_');
    });

    it('should preserve valid characters', () => {
      expect(sanitizeFilename('filename.txt')).toBe('filename.txt');
      expect(sanitizeFilename('file_name-123')).toBe('file_name-123');
    });

    it('should trim whitespace', () => {
      expect(sanitizeFilename('  filename.txt  ')).toBe('filename.txt');
    });
  });

  describe('createResponse', () => {
    it('should create a successful response by default', () => {
      const response = createResponse('Hello world');
      expect(response).toEqual({
        message: 'Hello world',
        success: true,
      });
    });

    it('should create a failed response when specified', () => {
      const response = createResponse('Error occurred', false);
      expect(response).toEqual({
        message: 'Error occurred',
        success: false,
      });
    });

    it('should trim the message', () => {
      const response = createResponse('  Hello world  ');
      expect(response.message).toBe('Hello world');
    });
  });

  describe('parseKeyValue', () => {
    it('should parse valid key-value pairs', () => {
      expect(parseKeyValue('key=value')).toEqual({ key: 'key', value: 'value' });
      expect(parseKeyValue('name=John Doe')).toEqual({ key: 'name', value: 'John Doe' });
      expect(parseKeyValue('count=42')).toEqual({ key: 'count', value: '42' });
    });

    it('should handle whitespace around key-value pairs', () => {
      expect(parseKeyValue('  key = value  ')).toEqual({ key: 'key', value: 'value' });
      expect(parseKeyValue('name=  John Doe  ')).toEqual({ key: 'name', value: 'John Doe' });
    });

    it('should return null for invalid formats', () => {
      expect(parseKeyValue('invalid')).toBeNull();
      expect(parseKeyValue('=value')).toBeNull();
      expect(parseKeyValue('key=')).toBeNull();
      expect(parseKeyValue('')).toBeNull();
      expect(parseKeyValue('   ')).toBeNull();
    });

    it('should return null for empty keys or values after trimming', () => {
      expect(parseKeyValue('  = value')).toBeNull();
      expect(parseKeyValue('key =  ')).toBeNull();
    });

    it('should handle multiple equals signs correctly', () => {
      expect(parseKeyValue('key=value=extra')).toEqual({ key: 'key', value: 'value=extra' });
    });
  });
});
