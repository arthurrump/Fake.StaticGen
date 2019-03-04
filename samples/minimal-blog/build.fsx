// Content for this blog was provided by https://pdos.csail.mit.edu/archive/scigen/

#r "paket:
source ../../src/Fake.StaticGen/bin/Debug/
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget Fake.Core.Target 
nuget Fake.StaticGen 1.0.0 //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard" // Intellisense fix, see FAKE #1938
  #r "netstandard"
#endif

open System.IO
open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.StaticGen

type SiteConfig =
    { Title : string }

type PageType =
    | Post of Post
    | Overview of Overview
and Post =
    { Title : string
      Paragraphs : string list }
and Overview = 
    { Index : int
      Previous : string option
      Next : string option
      Posts : Page<Post> seq }

let parsePost (file : string) input =
    let lines = String.split '\n' input
    let url = Path.GetFileNameWithoutExtension file
    { Url = url
      Content = 
        Post { Title = lines.Head
               Paragraphs = lines.Tail } }

let postsChooser page = 
    match page.Content with
    | Post post -> Some { Url = page.Url; Content = post }
    | _ -> None

let postsOverview pages =
    let url i = if i = 0 then "/" else sprintf "/posts/%i" i 
    pages
    |> Seq.mapi (fun i posts ->
        let content = 
            { Index = i
              Previous = if i = 0 then None else Some (url (i - 1))
              Next = if i = Seq.length pages - 1 then None else Some (url (i + 1))
              Posts = posts }
        { Url = url i; Content = Overview content })

let template (site : StaticSite<SiteConfig, PageType>) page =
    let pageTitle =
        match page.Content with
        | Overview _ -> "Posts"
        | Post { Title = title } -> title

    let content =
        match page.Content with
        | Post p -> p.Paragraphs |> List.map (sprintf "<p>%s</p>") |> String.concat "\n"
        | Overview o ->
            let posts =
                [ for post in o.Posts ->
                    sprintf """<section><a href="%s"><h3>%s</h3></a><p>%s</p></section>""" 
                        post.Url post.Content.Title post.Content.Paragraphs.Head ] |> String.concat "\n"
            let pager =
                let next = o.Next |> Option.map (fun n -> sprintf """<a href="%s">Next &#x276F;</a>""" n)
                let previous = o.Previous |> Option.map (fun p -> sprintf """<a href="%s">&#x276E; Previous</a>""" p)
                let buttons = [ previous; next ] |> List.choose id |> String.concat " | "
                sprintf """<div class="pager">%s</div>""" buttons
            posts + "\n" + pager
    
    sprintf
        """
        <!DOCTYPE html>
        <html><head><meta http-equiv="Content-Type" content="text/html; charset=utf-8">
                    <title>%s</title><link rel="stylesheet" href="/style.css"></head>
        <body>
            <h1>%s</h1>
            <hr/>
            <h2>%s</h2>
            %s
        </body></html>
        """
        site.Config.Title site.Config.Title
        pageTitle
        content

Target.create "Build" <| fun _ ->
    StaticSite.fromConfig "http://localhost:8080" { Title = "Fake.StaticGen Minimal Blog Sample" }
    |> StaticSite.withPagesFromSources (!! "content/*.post") parsePost
    |> StaticSite.withPaginatedOverview 3 postsChooser postsOverview
    |> StaticSite.withFileFromSource "style.css" "/style.css"
    |> StaticSite.generate "public" template

Target.runOrDefault "Build"
