#!/bin/bash
set -euo pipefail

# Navigate to the root of the repository
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..
PROJECT_ROOT=$PWD

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

echo "Copying Node.js runtime to dist/node/..."
cp -r node/* dist/node/
echo "✓ Node.js runtime copied to dist/node"

echo "Creating arcadia.zip..."
rm -f arcadia.zip
(cd dist && ../7zip/7za.exe a -tzip ../arcadia.zip *) > /dev/null

echo "Successfully created arcadia.zip"
echo "Archive size: $(ls -lh arcadia.zip | awk '{print $5}')"

echo "Publish complete!"
