#!/bin/bash
set -euo pipefail

# Navigate to the root of the repository
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..
PROJECT_ROOT=$PWD

# Use native architecture (no command line arguments needed)
TARGET_ARCH=$(scripts/get-native-arch.sh)

# Check that the node folder exists
if [ ! -d "node" ]; then
    echo "Error: Node.js folder 'node' not found. Run 'scripts/init.sh' first."
    exit 1
fi

echo "Publishing for native architecture: $TARGET_ARCH"
echo "Using Node.js from: node"

echo "Cleaning build/ and dist/ directories..."
rm -rf build/ dist/

echo "Running build..."
if ! scripts/build.sh --mode release; then
    echo "Build failed, aborting publish"
    exit 1
fi

echo "Creating production dist/ directory..."
mkdir -p dist/server
mkdir -p dist/node
mkdir -p dist/dotnet

echo "Copying server files to dist/server/ (excluding node_modules)..."
cp -r build/server/*.js dist/server/
cp server/package.json dist/server/
cp server/package-lock.json dist/server/
echo "✓ Server files copied to dist/server"

echo "Copying dotnet files to dist/dotnet/..."
cp -r build/dotnet/* dist/dotnet/
echo "✓ Dotnet files copied to dist/dotnet"

echo "Installing production dependencies in dist/server/..."
cd dist/server
export PATH=$PROJECT_ROOT/node:$PATH
npm ci --omit=dev
cd ../..
echo "✓ Production dependencies installed"

echo "Copying config file to dist/..."
cp build/config.jsonc dist/
echo "✓ Config file copied"

echo "Copying INSTALLING.html to dist/..."
cp server/INSTALLING.html dist/
echo "✓ INSTALLING.html copied"

echo "Copying LICENSE to dist/..."
cp LICENSE dist/LICENSE.txt
echo "✓ LICENSE copied"

echo "Copying Node.js runtime for $TARGET_ARCH to dist/node/..."
cp -r node/* dist/node/
echo "✓ Node.js runtime copied to dist/node"

# Create architecture-specific zip filename
ZIP_FILENAME="arcadia-${TARGET_ARCH}.zip"

echo "Creating $ZIP_FILENAME..."
rm -f "$ZIP_FILENAME"
(cd dist && ../7zip/7za.exe a -tzip "../$ZIP_FILENAME" *) > /dev/null

echo "Successfully created $ZIP_FILENAME"
echo "Archive size: $(ls -lh "$ZIP_FILENAME" | awk '{print $5}')"

echo "Publish complete for $TARGET_ARCH!"
