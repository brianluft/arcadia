#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..
rm -rf 7zip *.zip build dist node server/node_modules test/node_modules dotnet/artifacts
