#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..
rm -rf dist node node-* server/node_modules test/node_modules
