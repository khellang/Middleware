#!/usr/bin/env bash
set -e

PAKET_EXE=.paket/paket.exe
FAKE_EXE=packages/FAKE/tools/FAKE.exe

run() {
  if [ "$OS" != "Windows_NT" ]
  then
    mono "$@"
  else
    "$@"
  fi
}

run $PAKET_EXE restore

chmod +x packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/*

run $FAKE_EXE build.fsx "$@"
