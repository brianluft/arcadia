#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ErrorCode, ListToolsRequestSchema, McpError } from '@modelcontextprotocol/sdk/types.js';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

// Configuration interface
interface Config {
  version: string;
  server: {
    name: string;
    description: string;
  };
  storage?: {
    directory?: string;
  };
}

// Load configuration from parent directory
function loadConfig(): Config {
  try {
    // Get the directory of the current file
    const __filename = fileURLToPath(import.meta.url);
    const __dirname = path.dirname(__filename);

    // Config file is in the parent directory of the running JS file
    const configPath = path.join(__dirname, '..', 'config.json');

    if (!fs.existsSync(configPath)) {
      throw new Error(`Configuration file not found at: ${configPath}`);
    }

    const configData = fs.readFileSync(configPath, 'utf8');
    const config = JSON.parse(configData) as Config;

    return config;
  } catch (error) {
    console.error('Failed to load configuration:', error);
    process.exit(1);
  }
}

// Initialize storage directory
function initializeStorageDirectory(config: Config): string {
  try {
    // Get the directory of the current file
    const __filename = fileURLToPath(import.meta.url);
    const __dirname = path.dirname(__filename);
    const configDir = path.join(__dirname, '..');

    // Get storage directory from config, default to './storage/'
    const storageDir = config.storage?.directory || './storage/';

    // Resolve path relative to config directory if not absolute
    const resolvedStorageDir = path.isAbsolute(storageDir) ? storageDir : path.join(configDir, storageDir);

    // Create the directory if it doesn't exist
    if (!fs.existsSync(resolvedStorageDir)) {
      fs.mkdirSync(resolvedStorageDir, { recursive: true });
    }

    // Test write and delete a file to verify directory is writable
    const testFilePath = path.join(resolvedStorageDir, '.test-write-access');
    try {
      fs.writeFileSync(testFilePath, 'test');
      fs.unlinkSync(testFilePath);
    } catch (testError) {
      throw new Error(`Storage directory is not writable: ${resolvedStorageDir}. Error: ${testError}`);
    }

    console.error(`Storage directory initialized: ${resolvedStorageDir}`);
    return resolvedStorageDir;
  } catch (error) {
    console.error('Failed to initialize storage directory:', error);
    process.exit(1);
  }
}

// Load configuration at startup
const config = loadConfig();

// Initialize storage directory
const storageDirectory = initializeStorageDirectory(config);

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
