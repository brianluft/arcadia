#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

# Create temp directory
mkdir -p temp

echo "Testing screenshot command..."

echo "1. Full screen screenshot"
build/dotnet/ComputerUse.exe screenshot --outputFile "temp/screenshot_fullscreen.png"
if [ $? -ne 0 ]; then
    echo "Error: Screenshot failed"
    exit 1
fi

echo "2. Zoom path A1"
build/dotnet/ComputerUse.exe screenshot --zoomPath "A1" --outputFile "temp/screenshot_A1.png"
if [ $? -ne 0 ]; then
    echo "Error: Screenshot failed"
    exit 1
fi

echo "3. Zoom path A1,A1 (double zoom)"
build/dotnet/ComputerUse.exe screenshot --zoomPath "A1,A1" --outputFile "temp/screenshot_A1_A1.png"
if [ $? -ne 0 ]; then
    echo "Error: Screenshot failed"
    exit 1
fi

echo "4. Zoom path B1"
build/dotnet/ComputerUse.exe screenshot --zoomPath "B1" --outputFile "temp/screenshot_B1.png"
if [ $? -ne 0 ]; then
    echo "Error: Screenshot failed"
    exit 1
fi

echo "5. Zoom path B1,C2"
build/dotnet/ComputerUse.exe screenshot --zoomPath "B1,C2" --outputFile "temp/screenshot_B1_C2.png"
if [ $? -ne 0 ]; then
    echo "Error: Screenshot failed"
    exit 1
fi

echo "Screenshot tests completed successfully!" 