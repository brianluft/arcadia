#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

export PATH=$PWD/node:$PATH

# Format server code with prettier
echo "Formatting server code..."
cd server
../node/npm.cmd run prettier -- --write "src/**/*.{ts,js,json}"
cd ..

# Format test code with prettier
echo "Formatting test code..."
cd test
../node/npm.cmd run prettier -- --write "src/**/*.{ts,js,json}"
cd ..

# Format database code with csharpier
echo "Formatting database code..."
cd database
dotnet csharpier format src
cd ..

# Fix line endings
echo "Fixing line endings..."
find . -type f \( -name "*.ts" -o -name "*.json" -o -name "*.md" -o -name "*.mdc" -o -name "*.sh" \) -not -path "*/node_modules/*" | xargs dos2unix -q
find . -type f \( -name "*.cs" -o -name "*.csproj" -o -name "*.resx" -o -name "*.sln" \) -not -path "*/obj/*" | xargs unix2dos -q
