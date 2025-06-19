#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

export PATH=$PWD/node:$PATH

# Parse command line arguments
BUILD_MODE="development"
while [[ $# -gt 0 ]]; do
    case $1 in
        --mode)
            BUILD_MODE="$2"
            shift 2
            ;;
        --mode=*)
            BUILD_MODE="${1#*=}"
            shift
            ;;
        *)
            echo "Unknown parameter: $1"
            echo "Usage: $0 [--mode development|release]"
            exit 1
            ;;
    esac
done

if [ "$BUILD_MODE" != "development" ] && [ "$BUILD_MODE" != "release" ]; then
    echo "Error: BUILD_MODE must be 'development' or 'release'"
    exit 1
fi

NATIVE_ARCH=$(scripts/get-native-arch.sh)
echo "Building in $BUILD_MODE mode for $NATIVE_ARCH architecture..."

# Create build directories
mkdir -p build/server
mkdir -p build/test
mkdir -p build/node
mkdir -p build/dotnet

# Build server TypeScript code
echo "Building server..."
cd server
npm run build
cd ..
echo "✓ Server built successfully"

# Copy server node_modules to build/server for runtime dependencies
if [ ! -d "build/server/node_modules" ]; then
  echo "Copying server dependencies to build..."
  cmd "/C robocopy server/node_modules build/server/node_modules /E /MT /NFL /NDL /NJH /NJS /NC /NS" || [ $? -le 1 ]
  echo "✓ Server dependencies copied to build/server"
fi

# Copy config.jsonc to build/
echo "Copying config.jsonc to build..."
cp -f server/config.jsonc build/
echo "✓ Config file copied to build/"

# Build dotnet solution
echo "Building dotnet solution..."
cd dotnet
if [ "$BUILD_MODE" = "development" ]; then
    echo "Building dotnet solution in development mode (debug, framework dependent)..."
    dotnet build --configuration Debug --verbosity quiet --output ../build/dotnet/
else
    echo "Building dotnet solution in release mode (self-contained, ready-to-run)..."
    dotnet publish --configuration Release --verbosity quiet --self-contained --property:PublishReadyToRun=true --output ../build/dotnet/
fi
cd ..
echo "✓ Dotnet solution built successfully"

# Test database program
echo "Testing database program..."
./build/dotnet/Database.exe --input test/files/db_test_input.json --output test/files/db_test_output.json --expect test/files/db_test_expected.json
echo "✓ Database tests passed"

# Test logs program
echo "Testing logs program..."
mkdir -p build/storage
rm -f build/storage/*.log
echo "Test log message for build verification" > build/storage/test.log
# Temporarily unset ARCADIA_CONFIG_FILE to test default config.jsonc search behavior
SAVED_ARCADIA_CONFIG_FILE="$ARCADIA_CONFIG_FILE"
unset ARCADIA_CONFIG_FILE
LOGS_OUTPUT=$(./build/dotnet/Logs.exe --snapshot 2>&1)
# Restore ARCADIA_CONFIG_FILE for other tests
export ARCADIA_CONFIG_FILE="$SAVED_ARCADIA_CONFIG_FILE"
if echo "$LOGS_OUTPUT" | grep -q "Test log message for build verification"; then
    echo "✓ Logs program test passed"
else
    echo "✗ Logs program test failed"
    echo "Expected to find test message in output, but got:"
    echo "$LOGS_OUTPUT"
    exit 1
fi

# Build test TypeScript code
echo "Building test client..."
cd test
npm run build
cd ..
echo "✓ Test client built successfully"

# Copy test node_modules to build/test for runtime dependencies
if [ ! -d "build/test/node_modules" ]; then
  echo "Copying test dependencies to build..."
  cmd "/C robocopy test/node_modules build/test/node_modules /E /MT /NFL /NDL /NJH /NJS /NC /NS" || [ $? -le 1 ]
  echo "✓ Test dependencies copied to build/test"
fi

# Copy node runtime to build
if [ ! -d "build/node" ]; then
  echo "Copying Node.js runtime to build..."
  cmd "/C robocopy node build/node /E /MT /NFL /NDL /NJH /NJS /NC /NS" || [ $? -le 1 ]
  echo "✓ Node.js runtime copied to build/node"
fi

# Run tests
echo "Running tests..."
cd build
../node/node.exe test/index.js
cd ..
echo "✓ Tests completed"
