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

# Node.js download. Upstream: "https://nodejs.org/dist/v${NODE_VERSION}/${NODE_ZIP}"
NODE_FILENAME="node-v${NODE_VERSION}-win-${NATIVE_ARCH}"
NODE_ZIP="${NODE_FILENAME}.zip"
if [ "$NATIVE_ARCH" == "x64" ]; then
    NODE_ZIP_SHA256="9427C71B19D05F1905F151F1E67FCD535A4F671D66358DBF5B934A49C371E500"
else
    NODE_ZIP_SHA256="140F820338538E3883AA78E3E6E0483D201C7F2BE0B07CDA64BD535A71B139FE"
fi
NODE_URL="https://brianluft-mirror.com/node/${NODE_ZIP}"
NODE_DOWNLOAD_PATH="downloads/${NODE_ZIP}"
if [ ! -f "$NODE_DOWNLOAD_PATH" ]; then
    echo "Downloading Node.js from $NODE_URL"
    curl -L -o "$NODE_DOWNLOAD_PATH" "$NODE_URL"
    echo "Downloaded $NODE_ZIP"
else
    echo "Node.js for $NATIVE_ARCH already downloaded: $NODE_DOWNLOAD_PATH"
fi

DOWNLOADED_HASH=$(sha256sum "$NODE_DOWNLOAD_PATH" | cut -d' ' -f1 | tr '[:lower:]' '[:upper:]')
if [ "$DOWNLOADED_HASH" = "$NODE_ZIP_SHA256" ]; then
    echo "✓ Node.js download hash verification passed"
else
    echo "✗ Node.js download hash verification failed!"
    echo "Expected: $NODE_ZIP_SHA256"
    echo "Got:      $DOWNLOADED_HASH"
    exit 1
fi

# 7-zip download. Upstream: https://www.7-zip.org/a/7za920.zip
SEVENZIP_ZIP="7za920.zip"
SEVENZIP_ZIP_SHA256="2A3AFE19C180F8373FA02FF00254D5394FEC0349F5804E0AD2F6067854FF28AC"
SEVENZIP_URL="https://brianluft-mirror.com/7zip/7za920.zip"
SEVENZIP_DOWNLOAD_PATH="downloads/${SEVENZIP_ZIP}"
if [ ! -f "$SEVENZIP_DOWNLOAD_PATH" ]; then
    echo "Downloading 7-zip from $SEVENZIP_URL"
    curl -L -o "$SEVENZIP_DOWNLOAD_PATH" "$SEVENZIP_URL"
    echo "Downloaded $SEVENZIP_ZIP"
else
    echo "7-zip already downloaded: $SEVENZIP_DOWNLOAD_PATH"
fi

DOWNLOADED_HASH=$(sha256sum "$SEVENZIP_DOWNLOAD_PATH" | cut -d' ' -f1 | tr '[:lower:]' '[:upper:]')
if [ "$DOWNLOADED_HASH" = "$SEVENZIP_ZIP_SHA256" ]; then
    echo "✓ 7-zip download hash verification passed"
else
    echo "✗ 7-zip download hash verification failed!"
    echo "Expected: $SEVENZIP_ZIP_SHA256"
    echo "Got:      $DOWNLOADED_HASH"
    exit 1
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
unzip -q "$NODE_DOWNLOAD_PATH" -d .

# Rename the extracted folder to node
mv "$NODE_FILENAME" node

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
echo "Installing server dependencies..."
(cd server && npm install)
echo "✓ Server dependencies installed"

# Install test dependencies
echo "Installing test dependencies..."
(cd test && npm install)
echo "✓ Test dependencies installed"

# Install dotnet tools
echo "Installing dotnet tools..."
(cd dotnet && dotnet tool restore)
echo "✓ dotnet tools installed"

echo "Setup complete!" 
