module Post

#load "../.fake/build.fsx/intellisense.fsx"
#load "./models.fsx"

open Models
open Fake.StaticGen
open Giraffe.GiraffeViewEngine

let postUrl post =
    sprintf "/%04i/%02i/%02i/%s" post.PostedAt.Year post.PostedAt.Month post.PostedAt.Day post.Slug

let tagUrl tag =
    sprintf "/tags/%s" tag

let postOverviewUrl i = 
    if i = 0 then "/" 
    else sprintf "/posts/%i" i 

let postMain (post : Post) = [
    img [ _src post.HeaderImage ]
    h1 [] [ str post.Title ]
]

let postDetails (post : Post) = [
    time [ _datetime (post.PostedAt.ToShortDateString()) ] [ str (post.PostedAt.ToShortDateString()) ]
    ul [ _class "tags" ] (
        post.Tags 
        |> List.map (fun t -> 
            ul [] [ 
                a [ _href (tagUrl t) ] [ str t ]
            ])
    )
]

let template (post : Post) =
    article [ _class "post" ] [
        header [] [
            yield! postMain post
            yield! postDetails post
        ]
        post.Content
    ]

let overview (overview : Overview<Post>) =
    let pager =
        let next = overview.NextUrl |> Option.map (fun n -> a [ _href n ] [ rawText "Next &#x276F;" ])
        let previous = overview.PreviousUrl |> Option.map (fun p -> a [ _href p ] [ rawText "&#x276E; Previous" ])
        let buttons = [ previous; next ] |> List.choose id
        div [ _class "pager" ] buttons
    div [ _class "overview" ] [
        for page in overview.Pages ->
            section [ _class "post" ] [
                yield a [ _href page.Url ] (postMain page.Content) 
                yield! postDetails page.Content
            ]
        yield pager
    ]

let tagPage tag overview =
    hr []

let tagsOverview overview =
    hr []
