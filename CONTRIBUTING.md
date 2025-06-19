# Contributing to Arcadia

## Getting Started

Welcome to Arcadia development! This guide will help you set up your development environment to build and test the project.

### Prerequisites

- **Windows 10/11** (Arcadia is designed for Windows)
- **Git for Windows** (provides Git Bash)
- **Node.js is included** - The project includes its own Node.js runtime in the `node/` directory

### Environment Setup

#### Required Environment Variable

To run tests, you **must** set the `ARCADIA_CONFIG_FILE` environment variable:

```bash
export ARCADIA_CONFIG_FILE="/absolute/path/to/your/config.jsonc"
```

This environment variable tells Arcadia where to find the configuration file, overriding the default auto-detection.

#### Configuration File Requirements

Your configuration file must include:

1. **OpenAI API Key** - Required for running tests and for image processing features
2. **Storage directory** - Where command outputs are stored
3. **Bash path** - Path to Git Bash executable

Example `config.jsonc`:

```jsonc
{
  // Storage configuration - defines where command output files are stored
  "storage": {
    "directory": "./storage/"
  },
  // Bash configuration - defines the path to the bash executable on Windows
  "bash": {
    "path": "C:\\Program Files\\Git\\bin\\bash.exe"
  },
  // API keys - OpenAI key is REQUIRED for tests
  "apiKeys": {
    "openai": "your-openai-api-key-here"
  }
}
```

### Building the Project

1. **Initialize the project** (first time only):
   ```bash
   scripts/init.sh
   ```

1. **Build the project**:
   ```bash
   scripts/build.sh
   ```

The build script will:
- Compile TypeScript code for both server and test client
- Copy dependencies and configuration files
- Run the automated test suite

### Running Tests

The test suite runs automatically as part of `scripts/build.sh`. Tests require:

- `ARCADIA_CONFIG_FILE` environment variable to be set
- Valid OpenAI API key in the configuration file

If these requirements are not met, the tests will exit with an error message.

### Development Workflow

1. Make your changes
2. Run `scripts/build.sh` to test your changes
3. Format code with `scripts/format.sh` before committing
4. Commit your changes with a clear, one-line commit message

### Important Notes

- Always use the local Node.js from the `node/` directory (scripts handle this automatically)
- Never use `npx` - the project uses local npm installations
- All scripts should start with `cd "$( dirname "${BASH_SOURCE[0]}" )"` to set the working directory
- The project uses dependency injection patterns - avoid `import.meta` outside of startup code

### Getting Help

If you encounter issues:

1. Check that `ARCADIA_CONFIG_FILE` is set correctly
2. Verify your configuration file has a valid OpenAI API key
3. Make sure Git Bash is installed and accessible
4. Run `scripts/build.sh` to see detailed error messages 