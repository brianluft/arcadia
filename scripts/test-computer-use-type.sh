#!/bin/bash
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

build/dotnet/ComputerUse.exe type --text "foo bar"
