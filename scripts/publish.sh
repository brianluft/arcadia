#!/bin/bash
set -euo pipefail

# Navigate to the root of the repository
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..
PROJECT_ROOT=$PWD

# Parse command line arguments
TARGET_ARCH=""
if [ $# -eq 1 ]; then
    TARGET_ARCH="$1"
elif [ $# -eq 0 ]; then
    # Default to native architecture if no argument provided
    TARGET_ARCH=$(scripts/get-native-arch.sh)
else
    echo "Usage: $0 [x64|arm64]"
    echo "If no architecture is specified, uses native architecture"
    exit 1
fi

# Validate architecture argument
if [ "$TARGET_ARCH" != "x64" ] && [ "$TARGET_ARCH" != "arm64" ]; then
    echo "Error: Invalid architecture '$TARGET_ARCH'. Must be 'x64' or 'arm64'"
    exit 1
fi

# Check that the required node folder exists
NODE_FOLDER="node-${TARGET_ARCH}"
if [ ! -d "$NODE_FOLDER" ]; then
    echo "Error: Node.js folder '$NODE_FOLDER' not found. Run 'scripts/init.sh' first."
    exit 1
fi

echo "Publishing for architecture: $TARGET_ARCH"
echo "Using Node.js from: $NODE_FOLDER"

echo "Cleaning build/ and dist/ directories..."
rm -rf build/ dist/

echo "Running build..."
if ! scripts/build.sh; then
    echo "Build failed, aborting publish"
    exit 1
fi

echo "Creating production dist/ directory..."
mkdir -p dist/server
mkdir -p dist/node

echo "Copying server files to dist/server/ (excluding node_modules)..."
cp -r build/server/*.js dist/server/
cp server/package.json dist/server/
cp server/package-lock.json dist/server/
echo "✓ Server files copied to dist/server"

echo "Installing production dependencies in dist/server/..."
cd dist/server
export PATH=$PATH:$PROJECT_ROOT/node
npm ci --omit=dev
cd ../..
echo "✓ Production dependencies installed"

echo "Copying config file to dist/..."
cp build/config.json dist/
echo "✓ Config file copied"

echo "Copying INSTALLING.html to dist/..."
cp INSTALLING.html dist/
echo "✓ INSTALLING.html copied"

echo "Copying Node.js runtime for $TARGET_ARCH to dist/node/..."
cp -r "$NODE_FOLDER"/* dist/node/
echo "✓ Node.js runtime copied to dist/node"

# Create architecture-specific zip filename
ZIP_FILENAME="arcadia-${TARGET_ARCH}.zip"

echo "Creating $ZIP_FILENAME..."
rm -f "$ZIP_FILENAME"
(cd dist && ../7zip/7za.exe a -tzip "../$ZIP_FILENAME" *) > /dev/null

echo "Successfully created $ZIP_FILENAME"
echo "Archive size: $(ls -lh "$ZIP_FILENAME" | awk '{print $5}')"

echo "Publish complete for $TARGET_ARCH!"
