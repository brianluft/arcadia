#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ErrorCode, ListToolsRequestSchema, McpError } from '@modelcontextprotocol/sdk/types.js';
import { loadConfigFromDirectory } from './config.js';
import { initializeStorageDirectoryFromDirectory, readOutputFile } from './storage.js';
import { runBashCommand } from './bash.js';
import { readImage } from './image.js';
import {
  runDatabaseCommand,
  getSqlServerConnectionString,
  createSqliteInput,
  createSqlServerInput,
  SqlParameter,
} from './database.js';
import { normalizePath } from './utils.js';
import OpenAI from 'openai';
import * as fs from 'fs';
import * as path from 'path';

/**
 * Convert database JSON output to JSONL format
 * @param output - Raw JSON output from database
 * @returns Formatted JSONL output
 */
function formatDatabaseOutputAsJsonl(output: string): string {
  try {
    const jsonData = JSON.parse(output);
    if (Array.isArray(jsonData) && jsonData.length > 0) {
      // Convert CSV-style array format to line-oriented JSON
      const [headers, ...rows] = jsonData;
      const jsonLines = rows.map(row => {
        const obj: any = {};
        headers.forEach((header: string, index: number) => {
          obj[header] = row[index];
        });
        return JSON.stringify(obj);
      });
      return jsonLines.join('\n');
    }
  } catch (parseError) {
    // If JSON parsing fails, use the original output
  }
  return output;
}

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

// Initialize OpenAI client if API key is configured
let openaiClient: OpenAI | null = null;
if (config.apiKeys?.openai) {
  try {
    openaiClient = new OpenAI({
      apiKey: config.apiKeys.openai,
    });
    console.error('OpenAI client initialized');
  } catch (error) {
    console.error('Failed to initialize OpenAI client:', error);
    process.exit(1);
  }
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
    name: 'arcadia-server',
    version: '1.0.0',
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// List available tools
server.setRequestHandler(ListToolsRequestSchema, async () => {
  const tools: any[] = [
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
  ];

  // Add read_image tool only if OpenAI client is available
  if (openaiClient) {
    tools.push({
      name: 'read_image',
      description: 'Ask GPT-4o a question about an image, allowing a text-only client to deal with images',
      inputSchema: {
        type: 'object' as const,
        properties: {
          image_path: {
            type: 'string' as const,
            description:
              'Absolute path to a bmp/gif/jpeg/png/tiff image file. Accepts both Windows-style (C:\\path) and MSYS-style (/c/path) paths.',
          },
          prompt: {
            type: 'string' as const,
            description:
              'Optional prompt for the OpenAI multimodal model. If not provided, defaults to "Describe this image to a blind user. Transcribe any text."',
          },
        },
        required: ['image_path'] as const,
      },
    } as const);
  }

  // Add database tools
  tools.push(
    {
      name: 'list_database_connections',
      description:
        'List configured SQL Server connections. SQLite databases can be used on-the-fly without configuration.',
      inputSchema: {
        type: 'object',
        properties: {},
        required: [],
      },
    },
    {
      name: 'list_database_schemas',
      description: 'Lists databases and their schemas (SQL Server only).',
      inputSchema: {
        type: 'object',
        properties: {
          connection: {
            type: 'string',
            description: 'Name of the SQL Server connection',
          },
        },
        required: ['connection'],
      },
    },
    {
      name: 'list_database_objects',
      description:
        'List database objects (tables, views, procedures, functions, types). Use this if you don\'t know where to find something in the database. An "object" here means a table, view, stored procedure, user defined function, or user defined type.',
      inputSchema: {
        type: 'object',
        properties: {
          connection: {
            type: 'string',
            description: 'Name of an SQL Server connection or absolute path to an SQLite file',
          },
          type: {
            type: 'string',
            enum: ['relation', 'procedure', 'function', 'type'],
            description:
              'Type of object: relation (table or view, SQL Server and SQLite), procedure (stored procedure, SQL Server only), function (user defined function, SQL Server only), type (user defined type, SQL Server only)',
          },
          search_regex: {
            type: 'string',
            description: 'Optional case-insensitive regex pattern to filter object names',
          },
          database: {
            type: 'string',
            description:
              'Optional database name (SQL Server only). If specified, search only this database, otherwise all databases are scanned.',
          },
        },
        required: ['connection', 'type'],
      },
    },
    {
      name: 'describe_database_object',
      description: 'Get the definition of a database object found in list_database_objects.',
      inputSchema: {
        type: 'object',
        properties: {
          connection: {
            type: 'string',
            description: 'Name of an SQL Server connection or absolute path to an SQLite file',
          },
          name: {
            type: 'string',
            description: 'Object name in the same syntax as returned by list_database_objects',
          },
        },
        required: ['connection', 'name'],
      },
    },
    {
      name: 'list_database_types',
      description: 'List available DbType enum values for parameter binding in run_sql_command.',
      inputSchema: {
        type: 'object',
        properties: {},
        required: [],
      },
    },
    {
      name: 'run_sql_command',
      description:
        'Execute a SQL command with named parameters. Command execution is wrapped in a transaction that ALWAYS rolls back - it never commits under any circumstance.',
      inputSchema: {
        type: 'object',
        properties: {
          connection: {
            type: 'string',
            description: 'Name of an SQL Server connection or absolute path to an SQLite file',
          },
          command: {
            type: 'string',
            description:
              'SQL command with @foo style named parameters. Can be multiple statements (SQL Server supports full T-SQL scripts).',
          },
          timeout_seconds: {
            type: 'number',
            description: 'Query timeout in seconds (recommended starting point: 30)',
            minimum: 1,
            maximum: 3600,
          },
          arguments: {
            type: 'object',
            description:
              'Optional named parameters. Object with parameter names as keys and {type, value} objects as values. For SQLite use: Int64, Double, String. For SQL Server use list_database_types for full list.',
            additionalProperties: {
              type: 'object',
              properties: {
                type: { type: 'string' },
                value: { type: ['string', 'number', 'boolean', 'null'] },
              },
              required: ['type', 'value'],
            },
          },
        },
        required: ['connection', 'command', 'timeout_seconds'],
      },
    }
  );

  return {
    tools,
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

    case 'read_image':
      // Only allow read_image if OpenAI client is available
      if (!openaiClient) {
        throw new McpError(ErrorCode.InternalError, 'read_image tool requires OpenAI API key to be configured');
      }

      if (!args || typeof args.image_path !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid image_path parameter');
      }

      try {
        const result = await readImage(
          args.image_path,
          args.prompt as string | undefined,
          openaiClient,
          storageDirectory
        );

        // Build response text
        const responseLines = [result.analysis];
        if (result.processedImagePath) {
          responseLines.push(`Note: Image was processed and saved to ${result.processedImagePath}`);
        }

        return {
          content: [
            {
              type: 'text',
              text: responseLines.join('\n\n'),
            },
          ],
        };
      } catch (error) {
        throw new McpError(ErrorCode.InternalError, `Failed to read image: ${error}`);
      }

    case 'list_database_connections':
      try {
        const connections = config.connections?.sqlServer || {};
        const connectionNames = Object.keys(connections);

        return {
          content: [
            {
              type: 'text',
              text: connectionNames.join('\n'),
            },
          ],
        };
      } catch (error) {
        throw new McpError(ErrorCode.InternalError, `Failed to list database connections: ${error}`);
      }

    case 'list_database_schemas':
      if (!args || typeof args.connection !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid connection parameter');
      }

      try {
        const connectionString = getSqlServerConnectionString(args.connection, config);
        const input = createSqlServerInput(
          connectionString,
          `SELECT DISTINCT '[' + name + '].[' + schema_name(schema_id) + ']' AS schema_name 
           FROM sys.databases d
           CROSS JOIN sys.schemas s
           ORDER BY schema_name`,
          undefined,
          30,
          0,
          1000
        );

        const result = await runDatabaseCommand(input, config, storageDirectory, __dirname);

        // Format output as JSONL
        const formattedOutput = formatDatabaseOutputAsJsonl(result.output);

        // Build response text
        const responseLines = [formattedOutput];
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
        throw new McpError(ErrorCode.InternalError, `Failed to list database schemas: ${error}`);
      }

    case 'list_database_objects':
      if (!args || typeof args.connection !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid connection parameter');
      }
      if (!args || typeof args.type !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid type parameter');
      }

      try {
        let query: string;
        let isSqlite = false;

        // Determine if this is SQLite or SQL Server
        const normalizedConnection = normalizePath(args.connection);
        if (fs.existsSync(normalizedConnection)) {
          isSqlite = true;
        }

        if (isSqlite) {
          // SQLite queries
          if (args.type === 'relation') {
            query = `SELECT '"' || name || '"' AS object_name FROM sqlite_master WHERE type IN ('table', 'view')`;
          } else {
            throw new Error(`Object type '${args.type}' is not supported for SQLite`);
          }

          if (args.search_regex && typeof args.search_regex === 'string') {
            query += ` AND name LIKE '%${args.search_regex.replace(/'/g, "''")}%'`;
          }
          query += ' ORDER BY name';

          const input = createSqliteInput(normalizedConnection, query, undefined, 30, 0, 1000);
          const result = await runDatabaseCommand(input, config, storageDirectory, __dirname);

          // Format output as JSONL
          const formattedOutput = formatDatabaseOutputAsJsonl(result.output);

          const responseLines = [formattedOutput];
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
        } else {
          // SQL Server queries
          const connectionString = getSqlServerConnectionString(args.connection, config);

          let whereClause = '';
          if (args.database && typeof args.database === 'string') {
            whereClause += ` AND d.name = '${args.database.replace(/'/g, "''")}'`;
          }
          if (args.search_regex && typeof args.search_regex === 'string') {
            whereClause += ` AND o.name LIKE '%${args.search_regex.replace(/'/g, "''")}%'`;
          }

          switch (args.type) {
            case 'relation':
              query = `SELECT '[' + d.name + '].[' + s.name + '].[' + o.name + ']' AS object_name
                       FROM sys.databases d
                       CROSS JOIN sys.objects o
                       INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                       WHERE o.type IN ('U', 'V') ${whereClause}
                       ORDER BY d.name, s.name, o.name`;
              break;
            case 'procedure':
              query = `SELECT '[' + d.name + '].[' + s.name + '].[' + o.name + ']' AS object_name
                       FROM sys.databases d
                       CROSS JOIN sys.objects o
                       INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                       WHERE o.type IN ('P', 'PC') ${whereClause}
                       ORDER BY d.name, s.name, o.name`;
              break;
            case 'function':
              query = `SELECT '[' + d.name + '].[' + s.name + '].[' + o.name + ']' AS object_name
                       FROM sys.databases d
                       CROSS JOIN sys.objects o
                       INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                       WHERE o.type IN ('FN', 'IF', 'TF') ${whereClause}
                       ORDER BY d.name, s.name, o.name`;
              break;
            case 'type':
              query = `SELECT '[' + d.name + '].[' + s.name + '].[' + t.name + ']' AS object_name
                       FROM sys.databases d
                       CROSS JOIN sys.types t
                       INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                       WHERE t.is_user_defined = 1 ${whereClause}
                       ORDER BY d.name, s.name, t.name`;
              break;
            default:
              throw new Error(`Unknown object type: ${args.type}`);
          }

          const input = createSqlServerInput(connectionString, query, undefined, 30, 0, 1000);
          const result = await runDatabaseCommand(input, config, storageDirectory, __dirname);

          // Format output as JSONL
          const formattedOutput = formatDatabaseOutputAsJsonl(result.output);

          const responseLines = [formattedOutput];
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
        }
      } catch (error) {
        throw new McpError(ErrorCode.InternalError, `Failed to list database objects: ${error}`);
      }

    case 'describe_database_object':
      if (!args || typeof args.connection !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid connection parameter');
      }
      if (!args || typeof args.name !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid name parameter');
      }

      try {
        let query: string;
        let isSqlite = false;

        // Determine if this is SQLite or SQL Server
        const normalizedConnection = normalizePath(args.connection);
        if (fs.existsSync(normalizedConnection)) {
          isSqlite = true;
        }

        if (isSqlite) {
          // SQLite: return the sql from sqlite_master
          const objectName = args.name.replace(/"/g, ''); // Remove quotes
          query = `SELECT sql FROM sqlite_master WHERE name = '${objectName.replace(/'/g, "''")}'`;

          const input = createSqliteInput(normalizedConnection, query, undefined, 30, 0, 1000);
          const result = await runDatabaseCommand(input, config, storageDirectory, __dirname);

          // Format output as JSONL
          const formattedOutput = formatDatabaseOutputAsJsonl(result.output);

          const responseLines = [formattedOutput];
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
        } else {
          // SQL Server: reconstruct pseudo-SQL from sys tables
          const connectionString = getSqlServerConnectionString(args.connection, config);

          // Extract database, schema, and object name from bracket notation
          const match = args.name.match(/\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]/);
          if (!match) {
            throw new Error(`Invalid object name format: ${args.name}. Expected [database].[schema].[object]`);
          }

          const [, database, schema, object] = match;

          query = `USE [${database.replace(/'/g, "''")}];
                   SELECT 
                     'Object: ' + OBJECT_SCHEMA_NAME(OBJECT_ID('[${schema}].[${object}]')) + '.' + OBJECT_NAME(OBJECT_ID('[${schema}].[${object}]')) AS info
                   UNION ALL
                   SELECT 'Type: ' + o.type_desc
                   FROM sys.objects o
                   WHERE o.object_id = OBJECT_ID('[${schema}].[${object}]')
                   UNION ALL
                   SELECT 'Column: ' + c.name + ' ' + t.name + 
                          CASE WHEN t.name IN ('varchar', 'nvarchar', 'char', 'nchar') 
                               THEN '(' + CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length AS varchar) END + ')'
                               WHEN t.name IN ('decimal', 'numeric') 
                               THEN '(' + CAST(c.precision AS varchar) + ',' + CAST(c.scale AS varchar) + ')'
                               ELSE '' END +
                          CASE WHEN c.is_nullable = 1 THEN ' NULL' ELSE ' NOT NULL' END
                   FROM sys.columns c
                   INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                   WHERE c.object_id = OBJECT_ID('[${schema}].[${object}]')
                   ORDER BY info`;

          const input = createSqlServerInput(connectionString, query, undefined, 30, 0, 1000);
          const result = await runDatabaseCommand(input, config, storageDirectory, __dirname);

          // Format output as JSONL
          const formattedOutput = formatDatabaseOutputAsJsonl(result.output);

          const responseLines = [formattedOutput];
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
        }
      } catch (error) {
        throw new McpError(ErrorCode.InternalError, `Failed to describe database object: ${error}`);
      }

    case 'list_database_types':
      try {
        // Static list of DbType enum values
        const dbTypes = [
          'AnsiString',
          'Binary',
          'Byte',
          'Boolean',
          'Currency',
          'Date',
          'DateTime',
          'Decimal',
          'Double',
          'Guid',
          'Int16',
          'Int32',
          'Int64',
          'Object',
          'SByte',
          'Single',
          'String',
          'Time',
          'UInt16',
          'UInt32',
          'UInt64',
          'VarNumeric',
          'AnsiStringFixedLength',
          'StringFixedLength',
          'Xml',
          'DateTime2',
          'DateTimeOffset',
        ];

        return {
          content: [
            {
              type: 'text',
              text: dbTypes.join('\n'),
            },
          ],
        };
      } catch (error) {
        throw new McpError(ErrorCode.InternalError, `Failed to list database types: ${error}`);
      }

    case 'run_sql_command':
      if (!args || typeof args.connection !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid connection parameter');
      }
      if (!args || typeof args.command !== 'string') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid command parameter');
      }
      if (!args || typeof args.timeout_seconds !== 'number') {
        throw new McpError(ErrorCode.InvalidParams, 'Missing or invalid timeout_seconds parameter');
      }

      // Validate optional arguments parameter
      let parameters: Record<string, SqlParameter> | undefined;
      if (args.arguments !== undefined) {
        if (typeof args.arguments !== 'object' || args.arguments === null || Array.isArray(args.arguments)) {
          throw new McpError(ErrorCode.InvalidParams, 'arguments must be an object with parameter definitions');
        }
        parameters = args.arguments as Record<string, SqlParameter>;
      }

      try {
        let input;
        let isSqlite = false;

        // Determine if this is SQLite or SQL Server
        const normalizedConnection = normalizePath(args.connection);
        if (fs.existsSync(normalizedConnection)) {
          isSqlite = true;
        }

        if (isSqlite) {
          input = createSqliteInput(normalizedConnection, args.command, parameters, args.timeout_seconds, 0, 1000);
        } else {
          const connectionString = getSqlServerConnectionString(args.connection, config);
          input = createSqlServerInput(connectionString, args.command, parameters, args.timeout_seconds, 0, 1000);
        }

        const result = await runDatabaseCommand(input, config, storageDirectory, __dirname);

        // Format output as JSONL
        const formattedOutput = formatDatabaseOutputAsJsonl(result.output);

        const responseLines = [formattedOutput];
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
        throw new McpError(ErrorCode.InternalError, `Failed to run SQL command: ${error}`);
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
