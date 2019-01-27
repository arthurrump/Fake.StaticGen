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

type Post =
    { Title : string
      Paragraphs : string [] }

type PageDetails =
    { PageTitle : string }

open Giraffe.GiraffeViewEngine
open Fake.StaticGen
open Fake.IO.Globbing.Operators

let postUrl (post : Post) =
    "/" + post.Title.Replace("-", "").Replace(" ", "-").ToLowerInvariant()

module Parsers =
    let post : Parser<Post> = fun input ->
        let lines = input.Split('\n')
        { Title = lines |> Array.head
          Paragraphs = lines |> Array.tail }

    let about : Parser<PageDetails * string []> = fun input ->
        { PageTitle = "About" }, input.Split('\n')

module Templates =
    let about : Template<SiteConfig, PageDetails, string []> = fun _ page paragraphs ->
        section []
          [ yield h2 [] [ str page.PageTitle ]
            for par in paragraphs -> p [] [ str par ] ]

    let post : Template<SiteConfig, PageDetails, Post> = fun _ _ post ->
        article [] 
          [ yield h2 [] [ str post.Title ]
            for par in post.Paragraphs -> p [] [ str par ] ]

    let postsOverview : Template<SiteConfig, PageDetails, Post list> = fun _ page posts ->
        section [] 
          [ yield h2 [] [ str page.PageTitle ]
            for po in posts -> 
              article [] 
                [ a [ _href (postUrl po) ] [ h2 [] [ str po.Title ] ] 
                  p [] [ str (po.Paragraphs.[0]) ]
                  hr [] ] ]

    let layout (config : SiteConfig) _ (page : Page<PageDetails>) =
        html []
          [ head [] 
              [ title [] [ str config.Title ]
                link [ _rel "stylesheet"
                       _href "/style.css" ] ] 
            body [] 
              [ h1 [] [ str config.Title ]
                nav [] 
                  [ str "Navigation: "
                    a [ _href "/" ] [ str "Posts" ]
                    str " - "
                    a [ _href "/about.html" ] [ str "About" ] ]
                div [ _class "content" ] [ page.Content ] ] ]

Target.create "Build" <| fun _ ->
    StaticSite.fromConfig { Title = "Fake.StaticGen Simple Sample" }
    |> StaticSite.withLayout Templates.layout
    |> StaticSite.withPostsFromFiles (!! "content/*.post") Parsers.post
    |> StaticSite.renderPosts postUrl (fun p -> { PageTitle = p.Title }) Templates.post
    |> StaticSite.withOverviewPage "/" { PageTitle = "Posts" } Templates.postsOverview
    |> StaticSite.withPageFromFile "/about.html" "content/about.page" Parsers.about Templates.about
    |> StaticSite.withFileFromPath "/style.css" "style.css"
    |> StaticSite.generate "public"

Target.runOrDefault "Build"
