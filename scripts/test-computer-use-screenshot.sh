#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

echo "Building ComputerUse project..."
cd dotnet
dotnet build --configuration Release --verbosity quiet
cd ..

# Create temp directory
mkdir -p temp

echo "Testing screenshot command..."

echo "1. Full screen screenshot"
./dotnet/artifacts/bin/ComputerUse/release/ComputerUse.exe screenshot --outputFile "temp/screenshot_fullscreen.png"

echo "2. Zoom path A1"
./dotnet/artifacts/bin/ComputerUse/release/ComputerUse.exe screenshot --zoomPath "A1" --outputFile "temp/screenshot_A1.png"

echo "3. Zoom path A1,A1 (double zoom)"
./dotnet/artifacts/bin/ComputerUse/release/ComputerUse.exe screenshot --zoomPath "A1,A1" --outputFile "temp/screenshot_A1_A1.png"

echo "4. Zoom path B1"
./dotnet/artifacts/bin/ComputerUse/release/ComputerUse.exe screenshot --zoomPath "B1" --outputFile "temp/screenshot_B1.png"

echo "5. Zoom path B1,C2"
./dotnet/artifacts/bin/ComputerUse/release/ComputerUse.exe screenshot --zoomPath "B1,C2" --outputFile "temp/screenshot_B1_C2.png"

echo "Screenshot tests completed successfully!" 