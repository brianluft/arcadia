#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

export PATH=$PWD/node:$PATH

# Parse command line arguments
BUILD_MODE="development"
TARGET_ARCH=""
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
        --arch)
            TARGET_ARCH="$2"
            shift 2
            ;;
        --arch=*)
            TARGET_ARCH="${1#*=}"
            shift
            ;;
        *)
            echo "Unknown parameter: $1"
            echo "Usage: $0 [--mode development|release] [--arch x64|arm64]"
            exit 1
            ;;
    esac
done

# Set default architecture if not specified
if [ -z "$TARGET_ARCH" ]; then
    TARGET_ARCH=$(scripts/get-native-arch.sh)
fi

if [ "$BUILD_MODE" != "development" ] && [ "$BUILD_MODE" != "release" ]; then
    echo "Error: BUILD_MODE must be 'development' or 'release'"
    exit 1
fi

if [ "$TARGET_ARCH" != "x64" ] && [ "$TARGET_ARCH" != "arm64" ]; then
    echo "Error: TARGET_ARCH must be 'x64' or 'arm64'"
    exit 1
fi

echo "Building in $BUILD_MODE mode for $TARGET_ARCH architecture..."

# Create build directories
mkdir -p build/server
mkdir -p build/test
mkdir -p build/node
mkdir -p build/database

# Build server TypeScript code
echo "Building server..."
cd server
npm run build
cd ..
echo "✓ Server built successfully"

# Copy server node_modules to build/server for runtime dependencies
if [ ! -d "build/server/node_modules" ]; then
  echo "Copying server dependencies to build..."
  cp -rf server/node_modules build/server/
  echo "✓ Server dependencies copied to build/server"
fi

# Copy config.jsonc to build/
echo "Copying config.jsonc to build..."
cp -f server/config.jsonc build/
echo "✓ Config file copied to build/"

# Build database C# project
echo "Building database project..."
cd database
if [ "$BUILD_MODE" = "development" ]; then
    echo "Building database in development mode (debug, framework dependent)..."
    dotnet build --configuration Debug --verbosity quiet --output ../build/database/
else
    echo "Building database in release mode (self-contained, ready-to-run, single-file)..."
    dotnet publish --configuration Release --verbosity quiet --self-contained --runtime win-${TARGET_ARCH} --property:PublishSingleFile=true --property:PublishReadyToRun=true --output ../build/database/
fi
cd ..
echo "✓ Database project built successfully"

# Test database program
echo "Testing database program..."
if [ "$BUILD_MODE" = "development" ]; then
    # In development mode, use dotnet run
    cd database
    dotnet run --configuration Debug -- --input ../test/files/db_test_input.json --output ../test/files/db_test_output.json --expect ../test/files/db_test_expected.json
    cd ..
else
    # In release mode, use the published executable
    ./build/database/Database.exe --input test/files/db_test_input.json --output test/files/db_test_output.json --expect test/files/db_test_expected.json
fi
echo "✓ Database tests passed"

# Build test TypeScript code
echo "Building test client..."
cd test
npm run build
cd ..
echo "✓ Test client built successfully"

# Copy test node_modules to build/test for runtime dependencies
if [ ! -d "build/test/node_modules" ]; then
  echo "Copying test dependencies to build..."
  cp -rf test/node_modules build/test/
  echo "✓ Test dependencies copied to build/test"
fi

# Copy node runtime to build
if [ ! -d "build/node" ]; then
  echo "Copying Node.js runtime to build..."
  cp -rf node/* build/node/
  echo "✓ Node.js runtime copied to build/node"
fi

# Run tests
echo "Running tests..."
cd build
../node/node.exe test/index.js
cd ..
echo "✓ Tests completed"
