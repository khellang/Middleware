#!/usr/bin/env bash
set -e

dotnet tool restore
dotnet paket restore
chmod +x packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/*

if [ $# -eq 0 ]
then
  FAKE_ALLOW_NO_DEPENDENCIES=true dotnet fake build
else
  FAKE_ALLOW_NO_DEPENDENCIES=true dotnet fake build -t "$@"
fi
