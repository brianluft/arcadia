#!/bin/bash
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

mkdir -p temp

# Write the prompt file
echo "Open the Start menu using the mouse. Click the Notepad icon. Type Hello World in the Notepad window." > temp/prompt.txt

# Run the computer use command
build/dotnet/ComputerUse.exe run --configFile "$ARCADIA_CONFIG_FILE" --promptFile "temp/prompt.txt" --storageFolder "temp/" --outputFile "temp/output.txt" 