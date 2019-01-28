#r "paket:
source ../../src/Fake.StaticGen/bin/Debug/
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget Fake.Core.Target 
nuget Giraffe 3.5.1
nuget Fake.StaticGen 1.0.0 //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard" // Intellisense fix
#endif

open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.StaticGen
open Giraffe.GiraffeViewEngine

type SiteConfig =
    { Title : string }

type PageContent =
    | About of paragraphs : string []
    | Overview of title : string * Page<Post> list
    | Post of Post
and Post =
    { Title : string
      Paragraphs : string [] }

module Parsers =
    let post (input : string) =
        let lines = input.Split('\n')
        Post { Title = lines.[0]; Paragraphs = lines |> Array.tail }

    let about (input : string) =
        About (input.Split('\n'))

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
    |> renderHtmlDocument

let postOverview pages =
    let posts = 
        pages
        |> List.choose (fun page -> match page.Content with Post p -> Some { Url = page.Url; Content = p } | _ -> None)
    Overview ("Posts", posts)

let postUrl (title : string) =
    "/" + (title.Replace("-", "").Replace(" ", "-").ToLowerInvariant() 
           |> String.filter (fun c -> (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c = '-'))

let pageUrl _ page =
    match page with
    | About _ -> "/about.html"
    | Overview ("Posts", _) -> "/"
    | Overview (title, _) -> "/" + title
    | Post { Title = title } -> postUrl title // This is the only one that will ever match

Target.create "Build" <| fun _ ->
    StaticSite.fromConfig { Title = "Fake.StaticGen Simple Sample" }
    |> StaticSite.withPagesFromSources pageUrl (!! "content/*.post") Parsers.post
    |> StaticSite.withOverviewPage "/" postOverview
    |> StaticSite.withPageFromSource "/about.html" "content/about.page" Parsers.about
    |> StaticSite.withFileFromSource "/style.css" "style.css"
    |> StaticSite.generate "public" template

Target.runOrDefault "Build"

// Weirdnesses:
// - Due to the parser always returning a 'page, the input to the url function is a 'page,
//   even though it will always be of type Post, so the url function has redundant cases or
//   not fully matching.
//   -> Move the creation of urls to a function passed to the generate function
//      + All urls in one place
//        ~ They can also be close together in the main pipeline, so not that much of a benefit
//      - Urls are further away from the thing they refer to
//      - Would be hard to do for files, so that makes differences between Pages and Files bigger
//      - How to get the original file names into the url?
//   -> Make the parser return the url too
//      - Feels out of place, that's not what a parser is for
//   -> Have the parser return useful things and add a wrap function that would put it into 'page
//      - More functions, might not always apply
