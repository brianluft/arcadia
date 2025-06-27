#!/bin/bash

cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

build/dotnet/ComputerUse.exe noop
