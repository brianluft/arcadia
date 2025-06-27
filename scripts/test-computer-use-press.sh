#!/bin/bash
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

build/dotnet/ComputerUse.exe key-press --key R --win
sleep 1
build/dotnet/ComputerUse.exe key-press --key Escape 