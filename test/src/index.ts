#!/usr/bin/env node

import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';
import { spawn } from 'child_process';
import path from 'path';
import * as fs from 'fs';
import { parse as parseJsonc } from 'jsonc-parser';

const __dirname = import.meta.dirname;

interface TestCase {
  name: string;
  toolName: string;
  arguments: Record<string, unknown>;
  expectedContent?: string;
  expectedError?: string;
  description: string;
}

interface TestResult {
  name: string;
  passed: boolean;
  error?: string;
  actualContent?: string;
  expectedContent?: string;
}

class TestRunner {
  private client: Client;
  private results: TestResult[] = [];

  constructor(client: Client) {
    this.client = client;
  }

  async runTest(testCase: TestCase): Promise<TestResult> {
    console.log(`\n🧪 Running test: ${testCase.name}`);
    console.log(`   Description: ${testCase.description}`);

    try {
      const response = await this.client.callTool({
        name: testCase.toolName,
        arguments: testCase.arguments,
      });

      if (testCase.expectedError) {
        // Expected an error but got success
        return {
          name: testCase.name,
          passed: false,
          error: `Expected error "${testCase.expectedError}" but got success`,
          actualContent: JSON.stringify(response.content || {}),
        };
      }

      const actualContent =
        Array.isArray(response.content) && response.content.length > 0 && 'text' in response.content[0]
          ? response.content[0].text
          : '';

      if (testCase.expectedContent) {
        const passed = actualContent.includes(testCase.expectedContent);
        return {
          name: testCase.name,
          passed,
          error: passed ? undefined : `Content mismatch`,
          actualContent,
          expectedContent: testCase.expectedContent,
        };
      }

      return {
        name: testCase.name,
        passed: true,
        actualContent,
      };
    } catch (error: any) {
      if (testCase.expectedError) {
        // Check if the error message contains the expected error code or type
        const errorMessage = error.message || '';
        const passed =
          errorMessage.includes(testCase.expectedError) ||
          (errorMessage.includes('32602') && testCase.expectedError === 'InvalidParams') ||
          (errorMessage.includes('32601') && testCase.expectedError === 'MethodNotFound');
        return {
          name: testCase.name,
          passed,
          error: passed ? undefined : `Expected error "${testCase.expectedError}" but got "${error.message}"`,
        };
      }

      return {
        name: testCase.name,
        passed: false,
        error: error.message || 'Unknown error',
      };
    }
  }

  async runAllTests(testCases: TestCase[], hasSqlServerTestConnection: boolean): Promise<void> {
    console.log('🚀 Starting test suite...\n');

    for (const testCase of testCases) {
      const result = await this.runTest(testCase);
      this.results.push(result);

      if (result.passed) {
        console.log(`   ✅ PASS`);
      } else {
        console.log(`   ❌ FAIL: ${result.error}`);
        if (result.expectedContent && result.actualContent) {
          console.log(`      Expected: ${result.expectedContent}`);
          console.log(`      Actual:   ${result.actualContent}`);
        }
      }
    }

    // Add the dependent describe_database_object test if SQL Server connection is available
    if (hasSqlServerTestConnection) {
      await this.runDescribeDatabaseObjectTest();
    }

    this.printSummary();
  }

  async runDescribeDatabaseObjectTest(): Promise<void> {
    console.log(`\n🧪 Running test: sql_server_describe_database_object`);
    console.log(
      `   Description: Test SQL Server describe_database_object using first result from list_database_objects`
    );

    try {
      const objectName = '[master].[sys].[objects]';

      const describeResponse = await this.client.callTool({
        name: 'describe_database_object',
        arguments: {
          connection: 'test',
          name: objectName,
        },
      });

      const describeContent =
        Array.isArray(describeResponse.content) &&
        describeResponse.content.length > 0 &&
        'text' in describeResponse.content[0]
          ? describeResponse.content[0].text
          : '';

      if (describeContent.includes('"info"')) {
        this.results.push({
          name: 'sql_server_describe_database_object',
          passed: true,
          actualContent: describeContent,
        });
        console.log(`   ✅ PASS`);
      } else {
        this.results.push({
          name: 'sql_server_describe_database_object',
          passed: false,
          error: 'Expected "info" field in describe_database_object response',
          actualContent: describeContent,
        });
        console.log(`   ❌ FAIL: Expected "info" field in describe_database_object response`);
      }
    } catch (error: any) {
      this.results.push({
        name: 'sql_server_describe_database_object',
        passed: false,
        error: error.message || 'Unknown error in describe_database_object test',
      });
      console.log(`   ❌ FAIL: ${error.message || 'Unknown error'}`);
    }
  }

  private printSummary(): void {
    const passed = this.results.filter(r => r.passed).length;
    const total = this.results.length;
    const failed = total - passed;

    console.log('\n📊 Test Summary:');
    console.log(`   Total:  ${total}`);
    console.log(`   Passed: ${passed} ✅`);
    console.log(`   Failed: ${failed} ${failed > 0 ? '❌' : ''}`);

    if (failed > 0) {
      console.log('\n❌ Failed tests:');
      this.results
        .filter(r => !r.passed)
        .forEach(result => {
          console.log(`   • ${result.name}: ${result.error}`);
        });
      process.exit(1);
    } else {
      console.log('\n🎉 All tests passed!');
    }
  }
}

async function main() {
  // Validate required environment variables and config
  console.log('🔍 Validating test environment...');

  // Check for ARCADIA_CONFIG_FILE environment variable
  const configFilePath = process.env.ARCADIA_CONFIG_FILE;
  if (!configFilePath) {
    console.error('❌ ERROR: ARCADIA_CONFIG_FILE environment variable is required for running tests.');
    console.error('   Set it to the path of your config.jsonc file.');
    process.exit(1);
  }

  // Verify the config file exists and has OpenAI key
  if (!fs.existsSync(configFilePath)) {
    console.error(`❌ ERROR: Configuration file not found at: ${configFilePath}`);
    console.error('   Make sure ARCADIA_CONFIG_FILE points to a valid config.jsonc file.');
    process.exit(1);
  }

  let config;
  let hasSqlServerTestConnection = false;

  try {
    const configData = fs.readFileSync(configFilePath, 'utf8');
    config = parseJsonc(configData);

    if (!config.apiKeys?.openai) {
      console.error('❌ ERROR: OpenAI API key is required in the configuration file for running tests.');
      console.error('   Add a valid OpenAI API key to the "apiKeys.openai" field in your config.jsonc file.');
      process.exit(1);
    }

    // Check if there's a SQL Server connection named "test"
    hasSqlServerTestConnection = !!config.connections?.sqlServer?.test;

    if (hasSqlServerTestConnection) {
      console.log('✅ SQL Server test connection found - SQL Server tests will be included');
    } else {
      console.log('ℹ️  No SQL Server test connection found - SQL Server tests will be skipped');
    }

    console.log('✅ Environment validation passed');
  } catch (error) {
    console.error(`❌ ERROR: Failed to read configuration file: ${error}`);
    process.exit(1);
  }

  // Path to the compiled server
  const serverPath = path.resolve(__dirname, '../server/index.js');

  // Start the MCP server as a child process
  console.log('🔧 Starting MCP server...');
  const serverProcess = spawn('node', [serverPath], {
    stdio: ['pipe', 'pipe', 'inherit'],
    cwd: path.dirname(serverPath),
  });

  // Create client and connect to server
  const transport = new StdioClientTransport({
    command: 'node',
    args: [serverPath],
    cwd: path.dirname(serverPath),
    env: process.env as Record<string, string>, // Pass environment variables to the server process
  });

  const client = new Client(
    {
      name: 'arcadia-test-client',
      version: '1.0.0',
    },
    {
      capabilities: {},
    }
  );

  try {
    await client.connect(transport);
    console.log('✅ Connected to MCP server\n');

    // List available tools
    const toolsResponse = await client.listTools();
    console.log('🔧 Available tools:', toolsResponse.tools.map((t: any) => t.name).join(', '));

    // Define test cases
    const testCases: TestCase[] = [
      {
        name: 'unknown_tool',
        toolName: 'nonexistent_tool',
        arguments: {},
        expectedError: 'MethodNotFound',
        description: 'Test calling a non-existent tool',
      },
      {
        name: 'run_bash_command_and_read_output',
        toolName: 'run_bash_command',
        arguments: {
          command:
            'echo "Line 1 with some words"; echo "Line 2 with different content"; echo "Line 3 has more words and text"; echo "Line 4 contains additional content"; echo "Line 5 with even more words to test"',
          working_directory: 'C:\\',
          timeout_seconds: 30,
        },
        expectedContent: 'Line 1 with some words',
        description: 'Test run_bash_command generates output for read_output testing',
      },
      {
        name: 'read_output_nonexistent_file',
        toolName: 'read_output',
        arguments: {
          filename: 'nonexistent.txt',
          start_line_index: 0,
        },
        expectedError: 'File not found',
        description: 'Test read_output with nonexistent file',
      },
      {
        name: 'read_output_invalid_line_index',
        toolName: 'read_output',
        arguments: {
          filename: 'test.txt',
          start_line_index: 'invalid',
        },
        expectedError: 'InvalidParams',
        description: 'Test read_output with invalid line index parameter',
      },
      {
        name: 'read_output_missing_filename',
        toolName: 'read_output',
        arguments: {
          start_line_index: 0,
        },
        expectedError: 'InvalidParams',
        description: 'Test read_output with missing filename parameter',
      },
      {
        name: 'read_image_test',
        toolName: 'read_image',
        arguments: {
          image_path: path.resolve(__dirname, '../../test/files/image.png'),
          prompt: 'What text is visible in this image?',
        },
        expectedContent: 'Hello world',
        description: 'Test read_image can analyze an image and extract text',
      },
      {
        name: 'database_path_windows_backslash',
        toolName: 'list_database_objects',
        arguments: {
          connection: path.resolve(__dirname, '../../test/files/foo.sqlite3').replace(/\//g, '\\'),
          type: 'relation',
        },
        expectedContent: '{"object_name":"\\"foo\\""',
        description: 'Test database path handling with Windows backslash format (C:\\...)',
      },
      {
        name: 'database_path_windows_forward_slash',
        toolName: 'list_database_objects',
        arguments: {
          connection: path.resolve(__dirname, '../../test/files/foo.sqlite3').replace(/\\/g, '/'),
          type: 'relation',
        },
        expectedContent: '{"object_name":"\\"foo\\""',
        description: 'Test database path handling with Windows forward slash format (C:/...)',
      },
      {
        name: 'database_path_msys_simple',
        toolName: 'list_database_objects',
        arguments: {
          connection: path
            .resolve(__dirname, '../../test/files/foo.sqlite3')
            .replace(/^([A-Z]):/i, '/$1')
            .replace(/\\/g, '/')
            .toLowerCase(),
          type: 'relation',
        },
        expectedContent: '{"object_name":"\\"foo\\""',
        description: 'Test database path handling with MSYS format (/c/...)',
      },
      {
        name: 'database_path_msys_with_colon',
        toolName: 'list_database_objects',
        arguments: {
          connection: path
            .resolve(__dirname, '../../test/files/foo.sqlite3')
            .replace(/^([A-Z]):/i, '/$1:')
            .replace(/\\/g, '/')
            .toLowerCase(),
          type: 'relation',
        },
        expectedContent: '{"object_name":"\\"foo\\""',
        description: 'Test database path handling with MSYS format with colon (/c:/...)',
      },
      {
        name: 'database_path_url_encoded',
        toolName: 'list_database_objects',
        arguments: {
          connection: encodeURIComponent(
            path
              .resolve(__dirname, '../../test/files/foo.sqlite3')
              .replace(/^([A-Z]):/i, '/$1:')
              .replace(/\\/g, '/')
              .toLowerCase()
          ),
          type: 'relation',
        },
        expectedContent: '{"object_name":"\\"foo\\""',
        description: 'Test database path handling with URL-encoded MSYS format (/c%3A/...)',
      },
    ];

    // Add SQL Server tests if connection is available
    if (hasSqlServerTestConnection) {
      const sqlServerTests: TestCase[] = [
        {
          name: 'sql_server_list_database_schemas',
          toolName: 'list_database_schemas',
          arguments: {
            connection: 'test',
          },
          description: 'Test SQL Server list_database_schemas returns at least one result',
        },
        {
          name: 'sql_server_list_database_objects',
          toolName: 'list_database_objects',
          arguments: {
            connection: 'test',
            type: 'relation',
          },
          description: 'Test SQL Server list_database_objects returns at least one result',
        },
        {
          name: 'sql_server_run_sql_command',
          toolName: 'run_sql_command',
          arguments: {
            connection: 'test',
            command: 'SELECT 1 AS foo',
            timeout_seconds: 30,
          },
          expectedContent: '"foo":1',
          description: 'Test SQL Server run_sql_command with simple SELECT statement',
        },
      ];

      testCases.push(...sqlServerTests);

      console.log(`\n📊 Added ${sqlServerTests.length} SQL Server tests to the suite`);
    }

    // Run tests
    const testRunner = new TestRunner(client);
    await testRunner.runAllTests(testCases, hasSqlServerTestConnection);
  } catch (error) {
    console.error('❌ Test client failed:', error);
    process.exit(1);
  } finally {
    // Clean up
    await client.close();
    serverProcess.kill();
  }
}

main().catch(error => {
  console.error('❌ Test runner failed to start:', error);
  process.exit(1);
});
