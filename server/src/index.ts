#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ErrorCode, ListToolsRequestSchema, McpError } from '@modelcontextprotocol/sdk/types.js';
import { loadConfigFromDirectory } from './config.js';
import { initializeStorageDirectoryFromDirectory } from './storage.js';
import { runBashCommand } from './bash.js';
import * as fs from 'fs';

// Get the directory of the current file using Node.js 24+ import.meta.dirname
const __dirname = import.meta.dirname;

// Load configuration at startup
let config;
try {
  config = loadConfigFromDirectory(__dirname);
} catch (error) {
  console.error('Failed to load configuration:', error);
  process.exit(1);
}

// Initialize storage directory
let storageDirectory;
try {
  storageDirectory = initializeStorageDirectoryFromDirectory(config, __dirname);
  console.error(`Storage directory initialized: ${storageDirectory}`);
} catch (error) {
  console.error('Failed to initialize storage directory:', error);
  process.exit(1);
}

// Check that bash exists
const bashPath = config.bash?.path || 'C:\\Program Files\\Git\\bin\\bash.exe';
if (!fs.existsSync(bashPath)) {
  console.error(`Bash not found at configured path: ${bashPath}`);
  process.exit(1);
}

const server = new Server(
  {
    name: config.server.name,
    version: config.version,
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// List available tools
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: [
      {
        name: 'example_tool',
        description: 'An example tool for testing',
        inputSchema: {
          type: 'object',
          properties: {
            message: {
              type: 'string',
              description: 'A message to echo back',
            },
          },
          required: ['message'],
        },
      },
      {
        name: 'run_bash_command',
        description: 'Run a bash command with timeout and output capture',
        inputSchema: {
          type: 'object',
          properties: {
            command: {
              type: 'string',
              description: 'The bash command to execute',
            },
            working_directory: {
              type: 'string',
              description:
                'Working directory (absolute path required). Accepts C:\\Foo\\Bar, /c/Foo/Bar, or /c:/Foo/Bar formats',
            },
            timeout_seconds: {
              type: 'number',
              description: 'Timeout in seconds (recommended default: 120)',
              minimum: 1,
              maximum: 3600,
            },
          },
          required: ['command', 'working_directory', 'timeout_seconds'],
        },
      },
    ],
  };
});

// Handle tool calls
server.setRequestHandler(CallToolRequestSchema, async request => {
  const { name, arguments: args } = request.params;

  switch (name) {
    case 'example_tool':
      if (!args || typeof args.message !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid message parameter');
      }
      return {
        content: [
          {
            type: 'text',
            text: `Echo: ${args.message}`,
          },
        ],
      };

    case 'run_bash_command':
      if (!args || typeof args.command !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid command parameter');
      }
      if (!args || typeof args.working_directory !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid working_directory parameter');
      }
      if (!args || typeof args.timeout_seconds !== 'number') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid timeout_seconds parameter');
      }

      try {
        const result = await runBashCommand(
          args.command,
          args.working_directory,
          args.timeout_seconds,
          config,
          storageDirectory
        );

        // Build response text
        const responseLines = [...result.output, result.status];
        if (result.truncationMessage) {
          responseLines.push(result.truncationMessage);
        }

        return {
          content: [
            {
              type: 'text',
              text: responseLines.join('\n'),
            },
          ],
        };
      } catch (error) {
        throw new McpError(ErrorCode.InternalError, `Failed to run bash command: ${error}`);
      }

    default:
      throw new McpError(ErrorCode.MethodNotFound, `Unknown tool: ${name}`);
  }
});

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error('Arcadia MCP server running on stdio');
}

main().catch(error => {
  console.error('Server failed to start:', error);
  process.exit(1);
});
