#!/bin/bash
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

mkdir -p temp

# Write the prompt file
echo "Open Notepad and type Hello World in it." > temp/prompt.txt

# Run the computer use command
build/dotnet/ComputerUse.exe run --configFile "$ARCADIA_CONFIG_FILE" --promptFile "temp/prompt.txt" --storageFolder "temp/" --outputFile "temp/output.txt" 