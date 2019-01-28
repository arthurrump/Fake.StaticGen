open Fake.Core
#r "paket:
source ../../src/Fake.StaticGen/bin/Debug/
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget Fake.Core.Target 
nuget Giraffe 3.5.1
nuget Fake.StaticGen 1.0.0 //"
#load "./.fake/build.fsx/intellisense.fsx"

type SiteConfig =
    { Title : string }

type PageDetails =
    | About
    | Overview of string
    | Post of title : string * firstParagraph : string

open Giraffe.GiraffeViewEngine
open Fake.StaticGen
open Fake.IO.Globbing.Operators

let postUrl (title : string) =
    "/" + (title.Replace("-", "").Replace(" ", "-").ToLowerInvariant() 
           |> String.filter (fun c -> (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c = '-'))

let url page =
    match page with
    | Post (title, _) -> postUrl title
    | _ -> ""

module Parsers =
    let post : Parser<PageDetails * string []> = fun input ->
        let lines = input.Split('\n')
        Post (lines.[0], lines.[1]), lines |> Array.tail

    let about : Parser<PageDetails * string []> = fun input ->
        About, input.Split('\n')

module Templates =
    let page _ page paragraphs =
        let title = 
            match page with
            | About -> "About"
            | Overview title -> title
            | Post (title, _) -> title
        section []
          [ yield h2 [] [ str title ]
            for par in paragraphs -> p [] [ str par ] ]

    let overview _ (Overview title) pages =
        let posts = pages |> List.choose (fun p -> match p.Details with Post (t, p) -> Some (t, p) | _ -> None)
        section [] 
          [ yield h2 [] [ str title ]
            for title, content in posts -> 
              article [] 
                [ a [ _href (postUrl title) ] [ h2 [] [ str title ] ] 
                  p [] [ str content ]
                  hr [] ] ]

    let layout site (page : Page<PageDetails>) =
        html []
          [ head [] 
              [ title [] [ str site.Config.Title ]
                link [ _rel "stylesheet"
                       _href "/style.css" ] ] 
            body [] 
              [ h1 [] [ str site.Config.Title ]
                nav [] 
                  [ str "Navigation: "
                    a [ _href "/" ] [ str "Posts" ]
                    str " - "
                    a [ _href "/about.html" ] [ str "About" ] ]
                div [ _class "content" ] [ page.Content ] ] ]

Target.create "Build" <| fun _ ->
    StaticSite.fromConfig { Title = "Fake.StaticGen Simple Sample" }
    |> StaticSite.withPagesFromFiles url (!! "content/*.post") Parsers.post Templates.page
    |> StaticSite.withOverviewPage "/" (Overview "Posts") Templates.overview
    |> StaticSite.withPageFromFile "/about.html" "content/about.page" Parsers.about Templates.page
    |> StaticSite.withFileFromPath "/style.css" "style.css"
    |> StaticSite.withLayout Templates.layout
    |> StaticSite.generate "public"

Target.runOrDefault "Build"

// Weirdnesses:
// - Templates need to match on types that that template shouldn't handle anyway
//   -> Or get a compiler warning that there are unmatched things
// - Content cannot be used effectively in overview, because its already a big block of Html
//   -> Needs duplication to details
