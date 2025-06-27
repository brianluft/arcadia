#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

# Create temp directory
mkdir -p temp

build/dotnet/ComputerUse.exe screenshot --outputFile "temp/screenshot_fullscreen.png"
if [ $? -ne 0 ]; then
    echo "Error: Screenshot failed"
    exit 1
fi

build/dotnet/ComputerUse.exe screenshot --zoomPath "B1" --outputFile "temp/screenshot_B1.png"
if [ $? -ne 0 ]; then
    echo "Error: Screenshot failed"
    exit 1
fi

build/dotnet/ComputerUse.exe screenshot --zoomPath "B1,C2" --outputFile "temp/screenshot_B1_C2.png"
if [ $? -ne 0 ]; then
    echo "Error: Screenshot failed"
    exit 1
fi
