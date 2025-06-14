#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
ARCH=$(./get-native-arch.sh)
cd ..

export PATH=$PWD/node:$PATH

# Create build directories
mkdir -p build/server
mkdir -p build/test
mkdir -p build/node

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

# Copy config.json to build/
echo "Copying config.json to build..."
cp -f server/config.json build/
echo "✓ Config file copied to build/"

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
