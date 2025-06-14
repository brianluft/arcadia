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
echo "Building server..."
cd server
npm run build
cd ..
echo "✓ Server built successfully"

# Copy server node_modules to dist/server for runtime dependencies
if [ ! -d "dist/server/node_modules" ]; then
  echo "Copying server dependencies to dist..."
  cp -rf server/node_modules dist/server/
  echo "✓ Server dependencies copied to dist/server"
fi

# Copy config.json to dist/
echo "Copying config.json to dist..."
cp -f server/config.json dist/
echo "✓ Config file copied to dist/"

# Build test TypeScript code
echo "Building test client..."
cd test
npm run build
cd ..
echo "✓ Test client built successfully"

# Copy test node_modules to dist/test for runtime dependencies
if [ ! -d "dist/test/node_modules" ]; then
  echo "Copying test dependencies to dist..."
  cp -rf test/node_modules dist/test/
  echo "✓ Test dependencies copied to dist/test"
fi

# Copy node runtime to dist
if [ ! -d "dist/node" ]; then
  echo "Copying Node.js runtime to dist..."
  cp -rf node/* dist/node/
  echo "✓ Node.js runtime copied to dist/node"
fi

# Run tests
echo "Running tests..."
cd dist
../node/node.exe test/index.js
cd ..
echo "✓ Tests completed"
