#r "paket:
source ../../src/Fake.StaticGen/bin/Debug/
source ../../src/Fake.StaticGen.Html/bin/Debug/
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget Fake.Core.Target 
nuget Thoth.Json.Net
nuget Markdig
nuget Fake.StaticGen 1.0.0
nuget Fake.StaticGen.Html 1.0.0 //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard" // Intellisense fix, see FAKE #1938
#endif

#load "./templates/layout.fsx"

open System
open System.IO
open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.StaticGen
open Fake.StaticGen.Html
open Giraffe.GiraffeViewEngine
open Thoth.Json.Net
open Markdig
open Models
open Post

let decodeConfig input =
    match Decode.fromString SiteConfig.Decoder input with
    | Ok config -> config
    | Error mes -> failwith mes

let parsePost path (input : string) =
    let getRes res = match res with Ok v -> v | Error mes -> failwith mes
    let slug = Path.GetFileNameWithoutExtension path
    let parts = input.Split([|"---"|], StringSplitOptions.RemoveEmptyEntries)
    let frontmatter = parts.[0]
    let markdown = parts.[1]
    let title = frontmatter |> Decode.fromString (Decode.field "title" Decode.string)
    let postedAt = frontmatter |> Decode.fromString (Decode.field "date" Decode.datetime)
    let tags = frontmatter |> Decode.fromString (Decode.field "tags" (Decode.list Decode.string))
    let headerImage = frontmatter |> Decode.fromString (Decode.field "header-image" Decode.string)
    let content = Markdown.ToHtml(markdown)
    let post = 
        { Title = title |> getRes
          Slug = slug
          PostedAt = postedAt |> getRes
          Tags = tags |> getRes
          HeaderImage = headerImage |> getRes
          Content = content |> rawText }
    { Url = postUrl post
      Content = Post post }

let postsOverview pages =
    pages
    |> List.mapi (fun i posts ->
        let content = 
            { Index = i
              PreviousUrl = if i = 0 then None else Some (postOverviewUrl (i - 1))
              NextUrl = if i = pages.Length - 1 then None else Some (postOverviewUrl (i + 1))
              Pages = posts }
        { Url = postOverviewUrl i; Content = PostOverview content })

Target.create "Build" <| fun _ ->
    StaticSite.fromConfigFile "content/config.json" decodeConfig
    |> StaticSite.withPagesFromSources (!! "content/posts/*.md") parsePost
    |> StaticSite.withPaginatedOverview 3 postsChooser postsOverview
    |> StaticSite.generateFromHtml "public" Layout.layout 

Target.runOrDefault "Build"
