#!/bin/bash
set -euo pipefail

# Navigate to root of repository
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

# Configuration
NODE_VERSION="24.2.0"
ARCH=$(scripts/get-native-arch.sh)

echo "Setting up Arcadia project..."
echo "Node.js version: $NODE_VERSION"
echo "Architecture: $ARCH"

# Create downloads folder if it doesn't exist
mkdir -p downloads

# Node.js download URL and filename
NODE_FILENAME="node-v${NODE_VERSION}-win-${ARCH}"
NODE_ZIP="${NODE_FILENAME}.zip"
NODE_URL="https://nodejs.org/dist/v${NODE_VERSION}/${NODE_ZIP}"
DOWNLOAD_PATH="downloads/${NODE_ZIP}"

# Download Node if it hasn't already been downloaded
if [ ! -f "$DOWNLOAD_PATH" ]; then
    echo "Downloading Node.js..."
    curl -L -o "$DOWNLOAD_PATH" "$NODE_URL"
    echo "Downloaded $NODE_ZIP"
else
    echo "Node.js already downloaded: $DOWNLOAD_PATH"
fi

# Delete "node" folder if it exists
if [ -d "node" ]; then
    echo "Removing existing node folder..."
    rm -rf node
fi

# Extract Node.js
echo "Extracting Node.js..."
unzip -q "$DOWNLOAD_PATH" -d .

# Rename the extracted folder to "node"  
mv "$NODE_FILENAME" node

# Verify that node.exe and npm.cmd exist
if [ -f "node/node.exe" ] && [ -f "node/npm.cmd" ]; then
    echo "✓ Node.js setup complete!"
    echo "✓ node/node.exe exists"
    echo "✓ node/npm.cmd exists"
    
    # Test Node.js
    echo "Node.js version: $(node/node.exe --version)"
    echo "npm version: $(node/npm.cmd --version)"
else
    echo "✗ Error: node.exe or npm.cmd not found in the node folder!"
    exit 1
fi

# Use local node
export PATH=$PWD/node:$PATH

# Install server dependencies
if [ -d "server" ]; then
    echo "Installing server dependencies..."
    cd server
    npm install
    cd ..
    echo "✓ Server dependencies installed"
fi

# Install test dependencies
if [ -d "test" ]; then
    echo "Installing test dependencies..."
    cd test
    npm install
    cd ..
    echo "✓ Test dependencies installed"
fi

echo "Setup complete!" 