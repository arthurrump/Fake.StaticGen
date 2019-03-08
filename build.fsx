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

module Version =
    let withPatch patch version =
        { version with Patch = patch; Original = None }

    let withPrerelease pre version =
        let pre = pre |> Option.bind (fun p -> PreRelease.TryParse p)
        { version with PreRelease = pre; Original = None }

    let getVersion () = 
        Trace.trace "Determining version based on Git history"
        let repo = "."
        let version = File.readAsString "version" |> SemVer.parse

        let height =
            let versionChanged =
                Git.FileStatus.getChangedFilesInWorkingCopy repo "HEAD" 
                |> Seq.exists (fun (_, f) -> f = "version")
            
            if versionChanged then 
                0
            else
                let previousVersionChange = Git.CommandHelper.runSimpleGitCommand repo "log --format=%H -1 -- version"
                let height = Git.Branches.revisionsBetween repo previousVersionChange "HEAD"
                if Git.Information.isCleanWorkingCopy repo then height else height + 1

        let preview = 
            let branch = Git.Information.getBranchName repo
            if branch = "master" then 
                None 
            else 
                let commit = Git.Information.getCurrentSHA1 repo |> fun s -> s.Substring(0, 7)
                Some (branch + "-" + commit)

        version |> withPatch (uint32 height) |> withPrerelease preview

let buildOptionsWithVersion version (options : DotNet.BuildOptions) =
    { options with 
        MSBuildParams = 
            { options.MSBuildParams with 
                Properties = ("Version", string version)::options.MSBuildParams.Properties }}

Target.create "Build" <| fun _ ->
    let version = Version.getVersion ()
    DotNet.build (buildOptionsWithVersion version) "Fake.StaticGen.sln"

Target.runOrDefault "Build"
