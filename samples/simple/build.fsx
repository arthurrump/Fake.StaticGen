// Content for the posts was provided by http://cipsum.com

#r "paket:
source ../../packages
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget Fake.Core.Target 
nuget Fake.StaticGen
nuget Fake.StaticGen.Html //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard" // Intellisense fix, see FAKE #1938
  #r "netstandard"
#endif

open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.StaticGen
open Fake.StaticGen.Html
open Fake.StaticGen.Html.ViewEngine

type SiteConfig =
    { Title : string }

type PageContent =
    | About of paragraphs : string []
    | Overview of title : string * Page<Post> seq
    | Post of Post
and Post =
    { Title : string
      Paragraphs : string [] }

let postUrl (title : string) =
    "/" + (title.Replace("-", "").Replace(" ", "-").ToLowerInvariant() 
           |> String.filter (fun c -> (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c = '-'))

module Parsers =
    let post _ (input : string) =
        let lines = input.Split('\n')
        let content = Post { Title = lines.[0]; Paragraphs = lines |> Array.tail }
        let url = postUrl lines.[0]
        { Url = url; Content = content }

    let about _ (input : string) =
        { Url = "about.html"
          Content = About (input.Split('\n')) }

let template (site : StaticSite<SiteConfig, PageContent>) page =
    let content title paragraphs =
        section []
          [ yield h2 [] [ str title ]
            for par in paragraphs -> p [] [ str par ] ]

    let overview title posts =
        section [] 
          [ yield h2 [] [ str title ]
            for post in posts -> 
              article [] 
                [ a [ _href post.Url ] [ h2 [] [ str post.Content.Title ] ] 
                  p [] [ str post.Content.Paragraphs.[0] ]
                  hr [] ] ]

    let content =
        match page.Content with
        | About text -> content "About" text
        | Post post -> content post.Title post.Paragraphs
        | Overview (title, posts) -> overview title posts

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
            div [ _class "content" ] [ content ] ] ]

let postOverview url site =
    let posts = 
        site.Pages
        |> Seq.choose (fun page -> match page.Content with Post p -> Some { Url = page.Url; Content = p } | _ -> None)
    { Url = url
      Content = Overview ("Posts", posts) }

Target.create "Build" <| fun _ ->
    StaticSite.fromConfig "http://localhost:8080" { Title = "Fake.StaticGen Simple Sample" }
    |> StaticSite.withPagesFromSources (!! "content/*.post") Parsers.post
    |> StaticSite.withOverviewPage (postOverview "/")
    |> StaticSite.withPageFromSource "content/about.page" Parsers.about
    |> StaticSite.withFileFromSource "style.css" "/style.css"
    |> StaticSite.generateFromHtml "public" template

Target.runOrDefault "Build"
