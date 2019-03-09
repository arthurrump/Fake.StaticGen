#r "paket:
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.Core.SemVer
nuget Fake.Core.Target 
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Tools.Git //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.Tools

let [<Literal>] solution = "Fake.StaticGen.sln"
let packagesLocation = Path.combine __SOURCE_DIRECTORY__ "packages"
let [<Literal>] repo = "."

module Version =
    let withPatch patch version =
        { version with Patch = patch; Original = None }

    let appendPrerelease suffix version =
        let pre = 
            match suffix, version.PreRelease with
            | Some s, Some p -> PreRelease.TryParse (sprintf "%O-%s" p s)
            | Some s, None -> PreRelease.TryParse s
            | None, p -> p
        { version with PreRelease = pre; Original = None }

    let getCleanVersion () = 
        Trace.trace "Determining version based on Git history"
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

        version |> withPatch (uint32 height) 
        
    let getVersionWithPrerelease () =
        let preview = 
            let branch = 
                let gb = Git.Information.getBranchName repo
                // Try get branch from Azure DevOps, if it's not properly set in Git
                if gb = "NoBranch" 
                then System.Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME") |> Option.ofObj
                else Some gb
            let isTagged () = Git.CommandHelper.directRunGitCommand repo "describe --exact-match HEAD"
            if branch = Some "master" || isTagged () then 
                None 
            else 
                let commit = Git.Information.getCurrentSHA1 repo |> fun s -> s.Substring(0, 7)
                let dirty = if Git.Information.isCleanWorkingCopy repo then None else Some "dirty"
                [ branch; Some commit; dirty ] |> List.choose id |> String.concat "-" |> Some

        getCleanVersion () |> appendPrerelease preview

    let version = lazy getVersionWithPrerelease ()

[<AutoOpen>]
module MSBuildParamHelpers =
    let withVersion version (param : MSBuild.CliArguments) =
        { param with Properties = ("Version", string version)::param.Properties }

    let withNoWarn warnings (param : MSBuild.CliArguments) =
        { param with 
            NoWarn = 
                param.NoWarn 
                |> Option.defaultValue []
                |> List.append warnings
                |> Some }

    let withDefaults version =
        withVersion version >> withNoWarn [ "FS2003" ]

Target.create "Clean" <| fun _ ->
    DotNet.exec id "clean" solution |> ignore
    DotNet.exec id "clean" (sprintf "%s -c Release" solution) |> ignore
    Directory.delete packagesLocation

Target.create "Version" <| fun _ ->
    Trace.tracefn "Version: %O" (Version.version.Force ())
    System.Console.WriteLine("\n##vso[build.updatebuildnumber]{0}", Version.version.Value)

Target.create "Build" <| fun _ ->
    let version = Version.version.Value
    DotNet.build (fun o -> { o with MSBuildParams = o.MSBuildParams |> withDefaults version }) solution

Target.create "Pack" <| fun _ ->
    let version = Version.version.Value
    solution |> DotNet.pack (fun o -> 
        { o with 
            MSBuildParams = o.MSBuildParams |> withDefaults version 
            Common = { o.Common with CustomParams = Some "--include-symbols" }
            NoBuild = true
            OutputPath = Some packagesLocation }) 

"Version" ==> "Build" ==> "Pack"

Target.create "Tag" <| fun _ ->
    let version = Version.getCleanVersion ()
    Git.CommandHelper.gitCommand repo (sprintf "tag -a v%O -m \"Version %O\"" version version)
    System.Console.WriteLine("\n##vso[release.updatereleasename]{0}", version)
    System.Console.WriteLine("\n##vso[task.setvariable variable=tagVersion]{0}", version)

Target.runOrDefault "Pack"
