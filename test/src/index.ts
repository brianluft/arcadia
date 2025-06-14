#!/usr/bin/env node

import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';
import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

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

  async runAllTests(testCases: TestCase[]): Promise<void> {
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

    this.printSummary();
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
        name: 'example_tool_valid_message',
        toolName: 'example_tool',
        arguments: { message: 'Hello, World!' },
        expectedContent: 'Echo: Hello, World!',
        description: 'Test example_tool with valid message',
      },
      {
        name: 'example_tool_empty_message',
        toolName: 'example_tool',
        arguments: { message: '' },
        expectedContent: 'Echo: ',
        description: 'Test example_tool with empty message',
      },
      {
        name: 'example_tool_missing_param',
        toolName: 'example_tool',
        arguments: {},
        expectedError: 'InvalidParams',
        description: 'Test example_tool with missing message parameter',
      },
      {
        name: 'unknown_tool',
        toolName: 'nonexistent_tool',
        arguments: {},
        expectedError: 'MethodNotFound',
        description: 'Test calling a non-existent tool',
      },
    ];

    // Run tests
    const testRunner = new TestRunner(client);
    await testRunner.runAllTests(testCases);
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
