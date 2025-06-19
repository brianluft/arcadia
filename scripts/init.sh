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

scripts/download.sh

# Function to setup Node.js for a specific architecture
setup_node_arch() {
    local arch=$1
    local folder_name=$2
    
    echo "Setting up Node.js for $arch..."
    
    # Node.js filename and download path
    local node_filename="node-v${NODE_VERSION}-win-${arch}"
    local node_zip="${node_filename}.zip"
    local download_path="downloads/${node_zip}"
    
    # Delete folder if it exists
    if [ -d "$folder_name" ]; then
        echo "Removing existing $folder_name folder..."
        rm -rf "$folder_name"
    fi
    
    # Extract Node.js
    echo "Extracting Node.js for $arch..."
    unzip -q "$download_path" -d .
    
    # Rename the extracted folder
    mv "$node_filename" "$folder_name"
    
    # Verify that node.exe and npm.cmd exist
    if [ -f "$folder_name/node.exe" ] && [ -f "$folder_name/npm.cmd" ]; then
        echo "✓ Node.js setup complete for $arch!"
        echo "✓ $folder_name/node.exe exists"
        echo "✓ $folder_name/npm.cmd exists"
    else
        echo "✗ Error: node.exe or npm.cmd not found in the $folder_name folder!"
        exit 1
    fi
}

# Setup Node.js for both architectures
setup_node_arch "x64" "node-x64"
setup_node_arch "arm64" "node-arm64"

# Setup the native architecture as the main "node" folder for building
echo "Setting up native Node.js ($NATIVE_ARCH) for building..."
if [ -d "node" ]; then
    echo "Removing existing node folder..."
    rm -rf node
fi

# Copy the native architecture node to the main "node" folder
cp -r "node-${NATIVE_ARCH}" node
echo "✓ Native Node.js ($NATIVE_ARCH) copied to node/ for building"

# Test Node.js
echo "Node.js version: $(node/node.exe --version)"
echo "npm version: $(node/npm.cmd --version)"

# 7-zip setup
SEVENZIP_ZIP="7za920.zip"
SEVENZIP_DOWNLOAD_PATH="downloads/${SEVENZIP_ZIP}"

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