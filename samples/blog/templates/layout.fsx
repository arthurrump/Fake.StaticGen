module Layout

#load "../.fake/build.fsx/intellisense.fsx"
#load "./models.fsx"
#load "./post.fsx"
#load "./photo.fsx"

open Models
open Fake.StaticGen
open Giraffe.GiraffeViewEngine

let siteTitle (site : SiteConfig) page =
    match page.Content with
    | Post p -> sprintf "%s | %s" p.Title site.Title
    | PostOverview _ -> site.Title
    // | TagPage (tag, _) -> sprintf "%s | %s" tag site.Title
    // | TagsOverview _ -> sprintf "Tags | %s" site.Title
    | Photo p -> sprintf "%s | Photography | %s" p.Title site.Title
    | PhotoOverview _ -> sprintf "Photography | %s" site.Title
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
                    | Post p -> Post.template p
                    | PostOverview o -> Post.overview o
                    // | TagPage (tag, overview) -> Post.tagPage tag overview
                    // | TagsOverview o -> Post.tagsOverview o
                    | Photo p -> Photo.template p
                    | PhotoOverview o -> Photo.overview o
                    | About content -> content
            ]
            // aside [ _id "taglist" ] (taglist site.Pages)
            footer [] [
                str "Powered by "; at "Fake.StaticGen" "https://github.com/arthurrump/Fake.StaticGen"
            ]
        ]
    ]