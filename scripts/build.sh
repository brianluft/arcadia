#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
ARCH=$(./get-native-arch.sh)
#TODO