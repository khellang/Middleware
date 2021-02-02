#r "paket:
version 6.0.0-beta8
framework: netstandard20
source https://api.nuget.org/v3/index.json
nuget Be.Vlaanderen.Basisregisters.Build.Pipeline 5.0.2 //"

#load "packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/build-generic.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open ``Build-generic``

let assemblyVersionNumber = (sprintf "%s.0")
let nugetVersionNumber = (sprintf "%s")

let buildSource = build assemblyVersionNumber
let buildTest = buildTest assemblyVersionNumber
let publishSource = publish assemblyVersionNumber
let pack = packSolution nugetVersionNumber

supportedRuntimeIdentifiers <- [ "linux-x64" ]

// Library ------------------------------------------------------------------------
Target.create "Lib_Build" (fun _ ->
  buildSource "Be.Vlaanderen.Basisregisters.ProblemDetails"
  buildTest "Be.Vlaanderen.Basisregisters.ProblemDetails.Tests"
)

Target.create "Lib_Test" (fun _ ->
  [
    "test" @@ "Be.Vlaanderen.Basisregisters.ProblemDetails.Tests" ]
  |> List.iter testWithDotNet
)

Target.create "Lib_Publish" (fun _ -> publishSource "Be.Vlaanderen.Basisregisters.ProblemDetails")
Target.create "Lib_Pack" (fun _ -> pack "Be.Vlaanderen.Basisregisters.ProblemDetails")

// --------------------------------------------------------------------------------
Target.create "PublishAll" ignore
Target.create "PackageAll" ignore

// Publish ends up with artifacts in the build folder
"DotNetCli"
==> "Clean"
==> "Restore"
==> "Lib_Build"
==> "Lib_Test"
==> "Lib_Publish"
==> "PublishAll"

// Package ends up with local NuGet packages
"PublishAll"
==> "Lib_Pack"
==> "PackageAll"

Target.runOrDefault "Lib_Build"
