#!/bin/bash
set -euo pipefail

# Navigate to root of repository
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

# Configuration
NODE_VERSION="24.2.0"
NATIVE_ARCH=$(scripts/get-native-arch.sh)

echo "Setting up Arcadia project..."
echo "Node.js version: $NODE_VERSION"
echo "Native architecture: $NATIVE_ARCH"

# Create downloads folder if it doesn't exist
mkdir -p downloads

# Download Node.js for native architecture only
echo "Downloading Node.js for $NATIVE_ARCH..."

# Node.js download URL and filename
node_filename="node-v${NODE_VERSION}-win-${NATIVE_ARCH}"
node_zip="${node_filename}.zip"
node_url="https://nodejs.org/dist/v${NODE_VERSION}/${node_zip}"
download_path="downloads/${node_zip}"

# Download Node if it hasn't already been downloaded
if [ ! -f "$download_path" ]; then
    echo "Downloading Node.js for $NATIVE_ARCH..."
    curl -L -o "$download_path" "$node_url"
    echo "Downloaded $node_zip"
else
    echo "Node.js for $NATIVE_ARCH already downloaded: $download_path"
fi

# 7-zip download
SEVENZIP_ZIP="7za920.zip"
SEVENZIP_URL="https://www.7-zip.org/a/7za920.zip"
SEVENZIP_DOWNLOAD_PATH="downloads/${SEVENZIP_ZIP}"

# Download 7-zip if it hasn't already been downloaded
if [ ! -f "$SEVENZIP_DOWNLOAD_PATH" ]; then
    echo "Downloading 7-zip..."
    curl -L -o "$SEVENZIP_DOWNLOAD_PATH" "$SEVENZIP_URL"
    echo "Downloaded $SEVENZIP_ZIP"
else
    echo "7-zip already downloaded: $SEVENZIP_DOWNLOAD_PATH"
fi

echo "Download complete!"

# Setup Node.js for native architecture directly into node/ folder
echo "Setting up Node.js for $NATIVE_ARCH..."

# Delete node folder if it exists
if [ -d "node" ]; then
    echo "Removing existing node folder..."
    rm -rf node
fi

# Extract Node.js directly into node/ folder
echo "Extracting Node.js for $NATIVE_ARCH..."
unzip -q "$download_path" -d .

# Rename the extracted folder to node
mv "$node_filename" node

# Verify that node.exe and npm.cmd exist
if [ -f "node/node.exe" ] && [ -f "node/npm.cmd" ]; then
    echo "✓ Node.js setup complete for $NATIVE_ARCH!"
    echo "✓ node/node.exe exists"
    echo "✓ node/npm.cmd exists"
else
    echo "✗ Error: node.exe or npm.cmd not found in the node folder!"
    exit 1
fi

# Test Node.js
echo "Node.js version: $(node/node.exe --version)"
echo "npm version: $(node/npm.cmd --version)"

# 7-zip setup
# Create 7zip folder and extract files
if [ -d "7zip" ]; then
    echo "Removing existing 7zip folder..."
    rm -rf 7zip
fi

echo "Extracting 7-zip..."
mkdir -p 7zip
unzip -q "$SEVENZIP_DOWNLOAD_PATH" -d 7zip

# Test 7-zip installation
if [ -f "7zip/7za.exe" ]; then
    echo "✓ 7-zip setup complete!"
    echo "✓ 7zip/7za.exe exists"
else
    echo "✗ Error: 7za.exe not found in the 7zip folder!"
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