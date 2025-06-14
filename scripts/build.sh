#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
ARCH=$(./get-native-arch.sh)
cd ..

export PATH=$PATH:$PWD/node

# Create dist directories
mkdir -p dist/server
mkdir -p dist/test
mkdir -p dist/node

# Build server TypeScript code
if [ -d "server" ]; then
    echo "Building server..."
    cd server
    ../node/npm.cmd run build
    cd ..
    echo "✓ Server built successfully"
    
    # Copy server node_modules to dist/server for runtime dependencies
    if [ -d "server/node_modules" ]; then
        echo "Copying server dependencies to dist..."
        cp -r server/node_modules dist/server/
        echo "✓ Server dependencies copied to dist/server"
    fi
    
    # Copy config.json to dist/
    if [ -f "server/config.json" ]; then
        echo "Copying config.json to dist..."
        cp server/config.json dist/
        echo "✓ Config file copied to dist/"
    fi
fi

# Build test TypeScript code
if [ -d "test" ]; then
    echo "Building test client..."
    cd test
    ../node/npm.cmd run build
    cd ..
    echo "✓ Test client built successfully"
    
    # Copy test node_modules to dist/test for runtime dependencies
    if [ -d "test/node_modules" ]; then
        echo "Copying test dependencies to dist..."
        cp -r test/node_modules dist/test/
        echo "✓ Test dependencies copied to dist/test"
    fi
fi

# Copy node runtime to dist
if [ -d "node" ]; then
    echo "Copying Node.js runtime to dist..."
    cp -r node/* dist/node/
    echo "✓ Node.js runtime copied to dist/node"
fi

# Run tests
if [ -d "test" ] && [ -f "dist/test/index.js" ]; then
    echo "Running tests..."
    cd dist
    ../node/node.exe test/index.js
    cd ..
    echo "✓ Tests completed"
fi