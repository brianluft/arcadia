#!/bin/bash

# Navigate to the root of the repository
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

echo "🧹 Cleaning dist/ directory..."
rm -rf dist/

echo "🔨 Running build..."
if ! scripts/build.sh; then
    echo "❌ Build failed, aborting publish"
    exit 1
fi

echo "🗑️ Deleting dist/test/ directory..."
rm -rf dist/test/

echo "📦 Copying node/ to dist/node/..."
cp -r node/ dist/node/

echo "🗜️ Creating arcadia.zip..."
rm -f arcadia.zip
7zip/7za.exe a -tzip arcadia.zip dist/* > /dev/null

if [ $? -eq 0 ]; then
    echo "✅ Successfully created arcadia.zip"
    echo "📋 Archive size: $(ls -lh arcadia.zip | awk '{print $5}')"
else
    echo "❌ Failed to create arcadia.zip"
    exit 1
fi

echo "�� Publish complete!" 