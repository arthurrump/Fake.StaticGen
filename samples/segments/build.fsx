#r "paket:
source ../../packages
nuget Fake.StaticGen 0.2.8-ce-preview-ce-b7456d8-dirty
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.6.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget System.Net.Http
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard" // Intellisense fix, see FAKE #1938
  #r "netstandard"
#endif

open Fake.Core
open Fake.StaticGen

let site = staticsite {
    config {| Title = "Website" |}

    page "/" "Welcome to the homepage"
    page "/about" "# About this site\nThis is a test."
    page "/search" "That's not implemented in here"

    fileStr "/robots.txt" "*"

    segment "PageCount" (fun helpers pages _ ->
        sprintf "Number of pages on %s: %i\n" helpers.Config.Value.Title (pages |> Seq.length))

    segment "FileCount" (fun helpers _ files ->
        sprintf "Number of files on %s: %i\n" helpers.Config.Value.Title (files |> Seq.length))

    segment "AllCounts" (fun helpers _ _ ->
        helpers.Segments.["PageCount"] + helpers.Segments.["FileCount"])

    template (fun helpers page ->
        helpers.Segments.["AllCounts"] + "\n" + page.Content)
}

Target.create "Generate" <| fun _ ->
    site |> StaticSite.generate "https://example.com/" "public"

Target.runOrDefault "Generate"