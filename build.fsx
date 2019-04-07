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
let [<Literal>] versionFile = "version"

module GitHelpers =
    let isTagged () = 
        Git.CommandHelper.directRunGitCommand repo "describe --exact-match HEAD"

    let previousChangeCommit file = 
        Git.CommandHelper.runSimpleGitCommand repo ("log --format=%H -1 -- " + file)

    let fileChanged file =
        Git.FileStatus.getChangedFilesInWorkingCopy repo "HEAD" 
        |> Seq.exists (fun (_, f) -> f = file)

module AzureDevOps =
    let tryGetSourceBranch () =
        System.Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME") |> Option.ofObj

    let updateBuildNumber version =
        sprintf "\n##vso[build.updatebuildnumber]%O" version
        |> System.Console.WriteLine

    let setVariable name value =
        sprintf "\n##vso[task.setvariable variable=%s]%s" name value
        |> System.Console.WriteLine

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
        let version = File.readAsString versionFile |> SemVer.parse

        let height =
            if GitHelpers.fileChanged versionFile then 
                0
            else
                let previousVersionChange = GitHelpers.previousChangeCommit versionFile
                let height = Git.Branches.revisionsBetween repo previousVersionChange "HEAD"
                if Git.Information.isCleanWorkingCopy repo then height else height + 1

        version |> withPatch (uint32 height) 
        
    let getVersionWithPrerelease () =
        let preview = 
            let branch = 
                let gb = Git.Information.getBranchName repo
                if gb = "NoBranch" then AzureDevOps.tryGetSourceBranch () else Some gb
            if branch = Some "master" || GitHelpers.isTagged () then 
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
    AzureDevOps.updateBuildNumber Version.version.Value

Target.create "Build" <| fun _ ->
    let version = Version.version.Value
    DotNet.build (fun o -> { o with MSBuildParams = o.MSBuildParams |> withDefaults version }) solution

Target.create "Pack" <| fun _ ->
    let version = Version.version.Value
    solution |> DotNet.pack (fun o -> 
        { o with 
            MSBuildParams = o.MSBuildParams |> withDefaults version 
            NoBuild = true
            OutputPath = Some packagesLocation }) 

"Version" ==> "Build" ==> "Pack"

let tag version = sprintf "v%O" version

Target.create "Tag" <| fun _ ->
    if Git.Information.isCleanWorkingCopy repo then
        let version = Version.getCleanVersion ()
        try
            Git.CommandHelper.gitCommand repo (sprintf "tag -a %s -m \"Version %O\"" (tag version) version)
        with
        | _ when (GitHelpers.isTagged ()) -> 
            Trace.tracefn "Commit was already tagged."
        AzureDevOps.setVariable "gitTag" (tag version)
    else
        failwith "Can't tag a dirty working directory."

Target.create "PushTag" <| fun param ->
    let remote = param.Context.Arguments |> List.tryExactlyOne
    match remote with
    | Some rem ->
        let version = Version.version.Value
        Git.Branches.pushTag repo rem (tag version)
    | None ->
        failwith "Please specify the remote as an argument."

"Tag" ==> "PushTag"
"Version" ==> "PushTag"
"Tag" ?=> "Version"

Target.runOrDefaultWithArguments "Pack"
