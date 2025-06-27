#!/bin/bash

cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

# Build the ComputerUse project
echo "Building ComputerUse project..."
dotnet build dotnet/ComputerUse/ComputerUse.csproj -c Release

if [ $? -ne 0 ]; then
    echo "Build failed"
    exit 1
fi

echo "Running noop command test..."
dotnet/ComputerUse/bin/Release/net9.0-windows/ComputerUse.exe noop

echo "Noop command test completed" 