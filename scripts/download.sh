#!/bin/bash
set -euo pipefail

# Navigate to root of repository
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

# Configuration
NODE_VERSION="24.2.0"

echo "Downloading dependencies for Arcadia project..."
echo "Node.js version: $NODE_VERSION"

# Create downloads folder if it doesn't exist
mkdir -p downloads

# Function to download Node.js for a specific architecture
download_node_arch() {
    local arch=$1
    
    echo "Downloading Node.js for $arch..."
    
    # Node.js download URL and filename
    local node_filename="node-v${NODE_VERSION}-win-${arch}"
    local node_zip="${node_filename}.zip"
    local node_url="https://nodejs.org/dist/v${NODE_VERSION}/${node_zip}"
    local download_path="downloads/${node_zip}"
    
    # Download Node if it hasn't already been downloaded
    if [ ! -f "$download_path" ]; then
        echo "Downloading Node.js for $arch..."
        curl -L -o "$download_path" "$node_url"
        echo "Downloaded $node_zip"
    else
        echo "Node.js for $arch already downloaded: $download_path"
    fi
}

# Download Node.js for both architectures
download_node_arch "x64"
download_node_arch "arm64"

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