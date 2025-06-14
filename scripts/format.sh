#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

export PATH=$PATH:$PWD/node

# Format server code with prettier
if [ -d "server" ]; then
    echo "Formatting server code..."
    cd server
    ../node/npm.cmd run prettier -- --write "src/**/*.{ts,js,json}"
    cd ..
fi

# Format test code with prettier
if [ -d "test" ]; then
    echo "Formatting test code..."
    cd test
    ../node/npm.cmd run prettier -- --write "src/**/*.{ts,js,json}"
    cd ..
fi
