#r "paket:
source ../../src/Fake.StaticGen/bin/Debug/
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget Fake.Core.Target 
nuget Fake.StaticGen 1.0.0 //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.StaticGen

type SiteConfig =
    { Title : string }

type Page =
    { Title : string
      Paragraphs : string list }

let parsePage title url _ input =
    { Url = url
      Content = 
        { Title = title
          Paragraphs = String.split '\n' input } }

let template (site : StaticSite<SiteConfig, Page>) page =
    sprintf
        """
        <!DOCTYPE html>
        <html><head><title>%s</title><link rel="stylesheet" href="/style.css"></head>
        <body>
            <h1>%s</h1>
            <nav><a href="/">Home</a> | <a href="/faq">FAQ</a></nav>
            <hr/>
            <h2>%s</h2>
            %s
        </body></html>
        """
        site.Config.Title site.Config.Title
        page.Content.Title
        (page.Content.Paragraphs |> List.map (sprintf "<p>%s</p>") |> String.concat "\n")

Target.create "Build" <| fun _ ->
    StaticSite.fromConfig "http://localhost:8080" { Title = "Fake.StaticGen Minimal Sample" }
    |> StaticSite.withPageFromSource "content/home.page" (parsePage "Home" "/")
    |> StaticSite.withPageFromSource "content/faq.page" (parsePage "Frequently Asked Questions" "/faq")
    |> StaticSite.withFileFromSource "style.css" "/style.css"
    |> StaticSite.generate "public" template

Target.runOrDefault "Build"
