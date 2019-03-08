open Fake.Tools.Git
#r "paket:
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.Core.SemVer
nuget Fake.Core.Target 
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Tools.Git //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.Tools

let withPatch patch version =
    { version with Patch = patch; Original = None }

let withPrerelease pre version =
    let pre = pre |> Option.bind (fun p -> PreRelease.TryParse p)
    { version with PreRelease = pre; Original = None }

let version = 
    let version = File.readAsString "version" |> SemVer.parse

    let height =
        let previousVersionChange = Git.CommandHelper.runSimpleGitCommand "." "log --format=%H -1 -- version"
        Git.Branches.revisionsBetween "." previousVersionChange "HEAD"

    let pre = 
        let branch = Git.Information.getBranchName "."
        if branch = "master" then 
            None 
        else 
            let commit = Git.Information.getCurrentSHA1 "." |> fun s -> s.Substring(0, 7)
            Some (branch + "-" + commit)

    version |> withPatch (uint32 height) |> withPrerelease pre

printfn "Version: %A" version

let buildOptionsWithVersion version (options : DotNet.BuildOptions) =
    { options with 
        MSBuildParams = 
            { options.MSBuildParams with 
                Properties = ("Version", string version)::options.MSBuildParams.Properties }}

Target.create "Build" <| fun _ ->
    DotNet.build (buildOptionsWithVersion version) "src/Fake.StaticGen/Fake.StaticGen.fsproj"

Target.runOrDefault "Build"
