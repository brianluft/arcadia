#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

export PATH=$PATH:$PWD/node

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

# Fix line endings
echo "Fixing line endings..."
find . -type f \( -name "*.ts" -o -name "*.json" -o -name "*.md" -o -name "*.mdc" -o -name "*.sh" \) -not -path "*/node_modules/*" | xargs dos2unix -q
