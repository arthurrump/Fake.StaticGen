#r "paket:
source ../../src/Fake.StaticGen/bin/Debug/
source ../../src/Fake.StaticGen.Html/bin/Debug/
source ../../src/Fake.StaticGen.Rss/bin/Debug/
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget Fake.Core.Target 
nuget Thoth.Json.Net
nuget Markdig
nuget Fake.StaticGen 1.0.0
nuget Fake.StaticGen.Html 1.0.0
nuget Fake.StaticGen.Rss 1.0.0 //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard" // Intellisense fix, see FAKE #1938
  #r "netstandard"
#endif

open System
open System.IO
open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.StaticGen
open Fake.StaticGen.Html
open Fake.StaticGen.Rss
open Giraffe.GiraffeViewEngine
open Thoth.Json.Net
open Markdig

//          Model
// =======================

type SiteConfig =
    { Title : string
      Author : string }

    static member Decoder : Decode.Decoder<SiteConfig> =
        Decode.object (fun get ->
            { Title = get.Required.Field "title" Decode.string
              Author = get.Required.Field "author" Decode.string })

type PageType =
    | Post of Post
    | PostOverview of Overview<Post>
    // | TagPage of tag: string * Overview<Post>
    // | TagsOverview of string list
    // | Photo of Photo
    // | PhotoOverview of Overview<Photo>
    | About of XmlNode

and Post =
    { Title : string
      Slug : string
      PostedAt : DateTime
      Tags : string list
      HeaderImage : string
      Content : XmlNode }

and Photo =
    { Filename : string
      Title : string
      Caption : string }

and Overview<'t> = 
    { Index : int
      PreviousUrl : string option
      NextUrl : string option
      Pages : Page<'t> seq }

//          Posts
// =======================

let postsChooser (page : Page<PageType>) = 
    match page.Content with
    | Post post -> Some { Url = page.Url; Content = post }
    | _ -> None

let postUrl post = sprintf "/%04i/%02i/%02i/%s" post.PostedAt.Year post.PostedAt.Month post.PostedAt.Day post.Slug

let tagUrl tag = sprintf "/tags/%s" tag

let postOverviewUrl i = if i = 0 then "/" else sprintf "/posts/%i" i 

let postMain (post : Post) = [
    img [ _src post.HeaderImage ]
    h1 [] [ str post.Title ] ]

let postDetails (post : Post) = [
    time [ _datetime (post.PostedAt.ToShortDateString()) ] [ str (post.PostedAt.ToShortDateString()) ]
    ul [ _class "tags" ] (
        post.Tags |> List.map (fun t -> 
            ul [] [ a [ _href (tagUrl t) ] [ str t ] ]) ) ]

let postTemplate (post : Post) =
    article [ _class "post" ] [
        header [] [
            yield! postMain post
            yield! postDetails post ]
        post.Content ]

let postOverviewTemplate (overview : Overview<Post>) =
    let pager =
        let next = overview.NextUrl |> Option.map (fun n -> a [ _href n ] [ rawText "Next &#x276F;" ])
        let previous = overview.PreviousUrl |> Option.map (fun p -> a [ _href p ] [ rawText "&#x276E; Previous" ])
        let buttons = [ previous; next ] |> List.choose id
        div [ _class "pager" ] buttons
    div [ _class "overview" ] [
        for page in overview.Pages ->
            section [ _class "post" ] [
                yield a [ _href page.Url ] (postMain page.Content) 
                yield! postDetails page.Content ]
        yield pager ]

//          Tags
// ======================

// let tagPageChooser (page : Page<PageType>) =
//     match page.Content with
//     | TagPage (tag, posts) -> Some { Url = page.Url; Content = (tag, posts) }
//     | _ -> None

// let tagPage tag overview = hr []

// let tagsOverview overview = hr []

//          Photos
// ========================

// let photoPage photo = hr []

// let photoOverview overview = hr []

//          Layouts
// =========================

let siteTitle (site : SiteConfig) (page : Page<PageType>)=
    match page.Content with
    | Post p -> sprintf "%s | %s" p.Title site.Title
    | PostOverview _ -> site.Title
    // | TagPage (tag, _) -> sprintf "%s | %s" tag site.Title
    // | TagsOverview _ -> sprintf "Tags | %s" site.Title
    // | Photo p -> sprintf "%s | Photography | %s" p.Title site.Title
    // | PhotoOverview _ -> sprintf "Photography | %s" site.Title
    | About _ -> sprintf "About | %s" site.Title

let at text url = a [ _href url ] [ str text ] 

// let taglist pages =
//     let tagLinks =
//         pages 
//         |> List.choose tagPageChooser
//         |> List.groupBy (fun p -> fst p.Content)
//         |> List.map (fun (tag, pages) ->
//             let mainPage = pages |> List.minBy (fun { Content = _, ov } -> ov.Index)
//             li [] [ a [ _href mainPage.Url ] [ str tag ] ])
//     [ a [ _class "aside-title"; _href "/tags" ] [ str "Tags" ]
//       ul [ ] tagLinks ]

let layout site page =
    let conf = site.Config
    html [ _lang "en" ] [
        head [] [
            title [] [ str (siteTitle conf page) ]
            meta [ _name "author"; _content (conf.Author) ]
            link [ _rel "stylesheet"; _href "/style.css" ]
            meta [ _name "generator"; _content "Fake.StaticGen" ]
            meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
            link [ _rel "canonical"; _content (site.AbsoluteUrl page.Url) ]
        ]
        body [] [
            header [] [
                nav [] [
                    a [ _id "title"; _href "/" ] [ str conf.Title ]
                    a [ _href "/" ] [ str "Blog" ]
                    a [ _href "/photos" ] [ str "Photography" ]
                    a [ _href "/about" ] [ str "About" ]
                ]
            ]
            section [ _id "main" ] [
                yield
                    match page.Content with
                    | Post p -> postTemplate p
                    | PostOverview o -> postOverviewTemplate o
                    // | TagPage (tag, overview) -> Post.tagPage tag overview
                    // | TagsOverview o -> Post.tagsOverview o
                    // | Photo p -> photoTemplate p
                    // | PhotoOverview o -> photoOverviewTemplate o
                    | About content -> content
            ]
            // aside [ _id "taglist" ] (taglist site.Pages)
            footer [] [
                str "Powered by "; at "Fake.StaticGen" "https://github.com/arthurrump/Fake.StaticGen"
            ]
        ]
    ]

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
    |> Seq.mapi (fun i posts ->
        let content = 
            { Index = i
              PreviousUrl = if i = 0 then None else Some (postOverviewUrl (i - 1))
              NextUrl = if i = Seq.length pages - 1 then None else Some (postOverviewUrl (i + 1))
              Pages = posts }
        { Url = postOverviewUrl i; Content = PostOverview content })

let postsRss (site : StaticSite<SiteConfig, PageType>) =
    let posts = site.Pages |> Seq.choose postsChooser
    Rss.Channel(
        title = site.Config.Title,
        link = site.BaseUrl,
        description = site.Config.Title,
        managingEditor = site.Config.Author,
        generator = "Fake.StaticGen",
        items = [
            for post in posts ->
                Rss.Item(
                    title = post.Content.Title,
                    link = site.AbsoluteUrl post.Url,
                    guid = Rss.Guid(site.AbsoluteUrl post.Url, true)) ])

Target.create "Build" <| fun _ ->
    StaticSite.fromConfigFile "https://example.com" "content/config.json" decodeConfig
    |> StaticSite.withPagesFromSources (!! "content/posts/*.md") parsePost
    |> StaticSite.withPaginatedOverview 3 postsChooser postsOverview
    |> StaticSite.withRssFeed postsRss "/rss.xml"
    |> StaticSite.generateFromHtml "public" layout 

Target.runOrDefault "Build"
