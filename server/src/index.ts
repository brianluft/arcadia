#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ErrorCode, ListToolsRequestSchema, McpError } from '@modelcontextprotocol/sdk/types.js';
import { loadConfigFromDirectory } from './config.js';
import { initializeStorageDirectoryFromDirectory, readOutputFile } from './storage.js';
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
            prependEnvironment: {
              type: 'object',
              description:
                'Optional key-value pairs to prepend to environment variables (e.g., {"PATH": ";C:\\\\Path1"}). Values do not support variable substitution. Use Windows-style paths and semicolons for PATH on Windows.',
              additionalProperties: {
                type: 'string',
              },
            },
            appendEnvironment: {
              type: 'object',
              description:
                'Optional key-value pairs to append to environment variables (e.g., {"PATH": ";C:\\\\Path2"}). Values do not support variable substitution. Use Windows-style paths and semicolons for PATH on Windows.',
              additionalProperties: {
                type: 'string',
              },
            },
            setEnvironment: {
              type: 'object',
              description:
                'Optional key-value pairs to set environment variables (completely replaces existing values). Values do not support variable substitution. Do not use this for PATH as it will clobber the entire PATH; use prependEnvironment or appendEnvironment instead unless you intend to replace the entire PATH.',
              additionalProperties: {
                type: 'string',
              },
            },
          },
          required: ['command', 'working_directory', 'timeout_seconds'],
        },
      },
      {
        name: 'read_output',
        description: 'Read output from a stored file with pagination support',
        inputSchema: {
          type: 'object',
          properties: {
            filename: {
              type: 'string',
              description: 'The filename to read (just the filename, not full path)',
            },
            start_line_index: {
              type: 'number',
              description: 'Zero-based line index to start reading from',
              minimum: 0,
            },
          },
          required: ['filename', 'start_line_index'],
        },
      },
    ],
  };
});

// Handle tool calls
server.setRequestHandler(CallToolRequestSchema, async request => {
  const { name, arguments: args } = request.params;

  switch (name) {
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

      // Validate optional environment parameters
      if (
        args.prependEnvironment !== undefined &&
        (typeof args.prependEnvironment !== 'object' ||
          args.prependEnvironment === null ||
          Array.isArray(args.prependEnvironment))
      ) {
        throw new McpError(ErrorCode.InvalidParams, 'prependEnvironment must be an object with string values');
      }
      if (
        args.appendEnvironment !== undefined &&
        (typeof args.appendEnvironment !== 'object' ||
          args.appendEnvironment === null ||
          Array.isArray(args.appendEnvironment))
      ) {
        throw new McpError(ErrorCode.InvalidParams, 'appendEnvironment must be an object with string values');
      }
      if (
        args.setEnvironment !== undefined &&
        (typeof args.setEnvironment !== 'object' || args.setEnvironment === null || Array.isArray(args.setEnvironment))
      ) {
        throw new McpError(ErrorCode.InvalidParams, 'setEnvironment must be an object with string values');
      }

      try {
        const result = await runBashCommand(
          args.command,
          args.working_directory,
          args.timeout_seconds,
          config,
          storageDirectory,
          args.prependEnvironment as Record<string, string> | undefined,
          args.appendEnvironment as Record<string, string> | undefined,
          args.setEnvironment as Record<string, string> | undefined
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

    case 'read_output':
      if (!args || typeof args.filename !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid filename parameter');
      }
      if (!args || typeof args.start_line_index !== 'number') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid start_line_index parameter');
      }

      try {
        const result = readOutputFile(args.filename, args.start_line_index, storageDirectory);

        // Build response text
        const responseLines = [...result.lines];
        if (result.truncated && result.nextLineIndex !== undefined) {
          const linesLeft = result.totalLines! - result.nextLineIndex;
          responseLines.push(
            `Truncated output. There are ${linesLeft} lines left. Use \`read_output\` tool with filename "${args.filename}" line ${result.nextLineIndex} to read more.`
          );
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
        throw new McpError(ErrorCode.InternalError, `Failed to read output file: ${error}`);
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
