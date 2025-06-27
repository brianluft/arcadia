#!/bin/bash

cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

build/dotnet/ComputerUse.exe confirm-screenshot --x 0 --y 0 --w 100 --h 100 