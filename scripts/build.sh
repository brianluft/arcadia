#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
ARCH=$(./get-native-arch.sh)
cd ..

export PATH=$PATH:$PWD/node

# Create dist directories
mkdir -p dist/server
mkdir -p dist/node

# Build server TypeScript code
if [ -d "server" ]; then
    echo "Building server..."
    cd server
    ../node/npm.cmd run build
    cd ..
    echo "✓ Server built successfully"
fi

# Copy node runtime to dist
if [ -d "node" ]; then
    echo "Copying Node.js runtime to dist..."
    cp -r node/* dist/node/
    echo "✓ Node.js runtime copied to dist/node"
fi