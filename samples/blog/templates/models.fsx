module Models

#load "../.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard" // Intellisense fix, see FAKE #1938
#endif

open System
open Fake.StaticGen
open Giraffe.GiraffeViewEngine
open Thoth.Json.Net

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
//     | TagPage of tag: string * Overview<Post>
//     | TagsOverview of string list
    | Photo of Photo
    | PhotoOverview of Overview<Photo>
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
      Pages : Page<'t> list }

let postsChooser (page : Page<PageType>) = 
    match page.Content with
    | Post post -> Some { Url = page.Url; Content = post }
    | _ -> None

// let tagPageChooser (page : Page<PageType>) =
//     match page.Content with
//     | TagPage (tag, posts) -> Some { Url = page.Url; Content = (tag, posts) }
//     | _ -> None