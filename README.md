# Arcadia - MCP Server for Windows Users

Arcadia is a Model Context Protocol (MCP) server that provides helpful tools for Windows users of Cursor, including bash command execution and output management.

## Features

- **Run Bash Commands**: Execute bash commands with configurable timeouts and working directories
- **Output Management**: Read and paginate through command output files
- **Windows Integration**: Designed specifically for Windows environments with Git Bash
- **Storage Management**: Automatic file management for command outputs

## Installation

### Prerequisites

- Windows 10/11
- Cursor IDE
- Git for Windows (for bash support)

### Setup Instructions

1. **Download Arcadia**: Download the latest `arcadia.zip` from our [GitHub Releases](https://github.com/your-repo/arcadia/releases)

2. **Extract**: Extract `arcadia.zip` to a folder of your choice (e.g., `C:\Tools\arcadia\`)

3. **Configure Cursor**: Add Arcadia to your Cursor MCP configuration:
   - Open Cursor settings
   - Navigate to the MCP servers configuration
   - Add the following configuration to your `mcp.json` file:

```json
{
  "mcpServers": {
    "arcadia": {
      "command": "<arcadia path>\\node\\node.exe",
      "args": [
        "<arcadia path>\\server\\index.js"
      ]
    }
  }
}
```

**Replace `<arcadia path>` with the actual path where you extracted Arcadia** (e.g., `C:\\Tools\\arcadia`).

### Example Configuration

If you extracted Arcadia to `C:\Tools\arcadia\`, your `mcp.json` should look like:

```json
{
  "mcpServers": {
    "arcadia": {
      "command": "C:\\Tools\\arcadia\\node\\node.exe",
      "args": [
        "C:\\Tools\\arcadia\\server\\index.js"
      ]
    }
  }
}
```

4. **Restart Cursor**: Restart Cursor to load the new MCP server configuration

5. **Verify Installation**: Once Cursor restarts, Arcadia should be available as an MCP server with the following tools:
   - `run_bash_command`: Execute bash commands with timeout and output capture
   - `read_output`: Read and paginate through command output files

## Configuration

Arcadia uses a `config.json` file located in its installation directory. The default configuration includes:

- **Bash Path**: `C:\Program Files\Git\bin\bash.exe` (default Git Bash location)
- **Storage Directory**: `./storage/` (relative to installation directory)

You can modify these settings by editing the `config.json` file in your Arcadia installation directory.

## Usage

Once configured, you can use Arcadia through Cursor's AI chat by asking it to:

- Run bash commands in specific directories
- Execute complex command pipelines
- Read through large command outputs
- Manage file operations through bash

### Example Commands

- "Run `ls -la` in my project directory"
- "Execute `npm install` and show me the output"
- "Run a git status check in my repository"

## Troubleshooting

### Common Issues

1. **"Command not found" errors**: Ensure Git for Windows is installed and bash is available at the configured path
2. **Permission errors**: Make sure Cursor has permission to execute files in the Arcadia directory
3. **Path issues**: Double-check that all paths in your `mcp.json` use double backslashes (`\\`) for Windows paths

### Getting Help

If you encounter issues:
1. Check the Cursor developer console for error messages
2. Verify your `mcp.json` configuration matches the examples above
3. Ensure all required files are present in your Arcadia installation directory

## Development

This MCP server is built with TypeScript and the MCP SDK. For development information, see the source repository.

## License

[Add your license information here] 