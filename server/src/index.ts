#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ErrorCode, ListToolsRequestSchema, McpError } from '@modelcontextprotocol/sdk/types.js';
import { loadConfigFromDirectory } from './config.js';
import { initializeStorageDirectoryFromDirectory } from './storage.js';

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
