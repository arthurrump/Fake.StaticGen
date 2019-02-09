namespace Fake.StaticGen.Rss

open System
open System.Text

open XmlEngine

type Channel = private Channel of XmlNode
type Image = private Image of XmlNode
type Cloud = private Cloud of XmlNode
type TextInput = private TextInput of XmlNode
type Item = private Item of XmlNode
type Enclosure = private Enclosure of XmlNode
type Category = private Category of XmlNode
type Guid = private Guid of XmlNode
type Source = private Source of XmlNode

type Rss =
    static member Channel
            (title : string,
             link : string,
             description : string,
             items : Item list,
             ?language : string,
             ?copyright : string,
             ?managingEditor : string,
             ?webMaster : string,
             ?pubDate : DateTime,
             ?lastBuildDate : DateTime,
             ?category : Category,
             ?generator : string,
             ?docs : string,
             ?cloud : Cloud,
             ?ttl : int,
             ?image : Image,
             ?rating : string,
             ?textInput : TextInput,
             ?skipHours : string,
             ?skipDays : string) =
        eTag "channel" [
            yield eTag "title" [ str title ]
            yield eTag "link" [ str link ]
            yield eTag "description" [ str description ]
            match language with Some x -> yield eTag "language" [ str x ] | _ -> ()
            match copyright with Some x -> yield eTag "copyright" [ str x ] | _ -> ()
            match managingEditor with Some x -> yield eTag "managingEditor" [ str x ] | _ -> ()
            match webMaster with Some x -> yield eTag "webMaster" [ str x ] | _ -> ()
            match pubDate with Some x -> yield eTag "pubDate" [ str (x.ToString("r")) ] | _ -> ()
            match lastBuildDate with Some x -> yield eTag "lastBuildDate" [ str (x.ToString("r")) ] | _ -> ()
            match category with Some (Category x) -> yield x | _ -> ()
            match generator with Some x -> yield eTag "generator" [ str x ] | _ -> ()
            match docs with Some x -> yield eTag "docs" [ str x ] | _ -> ()
            match cloud with Some (Cloud x) -> yield x | _ -> ()
            match ttl with Some x -> yield eTag "ttl" [ str (string x) ] | _ -> ()
            match image with Some (Image x) -> yield x | _ -> ()
            match rating with Some x -> yield eTag "rating" [ str x ] | _ -> ()
            match textInput with Some (TextInput x) -> yield x | _ -> ()
            match skipHours with Some x -> yield eTag "skipHours" [ str x ] | _ -> ()
            match skipDays with Some x -> yield eTag "skipDays" [ str x ] | _ -> ()
            yield eTag "items" (items |> List.map (fun (Item i) -> i))
        ]
        |> Channel

    static member Image
            (url : string,
             title : string,
             link : string,
             ?width : int,
             ?height : int,
             ?description : string) =
        eTag "image" [
            yield eTag "url" [ str url ]
            yield eTag "title" [ str title ]
            yield eTag "link" [ str link ]
            match width with Some x -> yield eTag "width" [ str (string x) ] | _ -> ()
            match height with Some x -> yield eTag "height" [ str (string x) ] | _ -> ()
            match description with Some x -> yield eTag "description" [ str x ] | _ -> ()
        ]
        |> Image

    static member Cloud
            (domain : string,
             port : int,
             path : string,
             registerProcedure : string,
             protocol : string) =
        voidTag "cloud" [
            attr "domain" domain
            attr "port" (string port)
            attr "path" path
            attr "registerProcedure" registerProcedure
            attr "protocol" protocol
        ]
        |> Cloud

    static member TextInput
            (title : string,
             description : string,
             name : string,
             link : string) =
        eTag "textInput" [
            eTag "title" [ str title ]
            eTag "description" [ str description ]
            eTag "name" [ str name ]
            eTag "link" [ str link ]
        ]
        |> TextInput

    static member Item
            (?title : string,
             ?link : string,
             ?description : string,
             ?author : string,
             ?category : Category,
             ?comments : string,
             ?enclosure : Enclosure,
             ?guid : Guid,
             ?pubDate : DateTime,
             ?source : Source) =
        eTag "item" [
            match title with Some x -> yield eTag "title" [ str x ] | _ -> ()
            match link with Some x -> yield eTag "link" [ str x ] | _ -> ()
            match description with Some x -> yield eTag "description" [ str x ] | _ -> ()
            match author with Some x -> yield eTag "author" [ str x ] | _ -> ()
            match category with Some (Category x) -> yield x | _ -> ()
            match comments with Some x -> yield eTag "comments" [ str x ] | _ -> ()
            match enclosure with Some (Enclosure x) -> yield x | _ -> ()
            match guid with Some (Guid x) -> yield x | _ -> ()
            match pubDate with Some x -> yield eTag "pubDate" [ str (x.ToString("r")) ] | _ -> ()
            match source with Some (Source x) -> yield x | _ -> ()
        ]
        |> Item

    static member Enclosure
            (url : string,
             length : int,
             ``type`` : string) =
        voidTag "enclosure" [
            attr "url" url
            attr "length" (string length)
            attr "type" ``type``
        ]
        |> Item

    static member Category
            (category : string,
             ?domain : string) =
        tag "category" 
            [ match domain with Some x -> yield attr "domain" x | _ -> () ]
            [ str category ]
        |> Category
    
    static member Guid
            (guid : string,
             ?isPermaLink : bool) =
        tag "guid"
            [ match isPermaLink with Some x -> yield attr "isPermaLink" (string x) | _ -> () ]
            [ str guid ]
        |> Guid

    static member Source
            (source : string,
             url : string) =
        tag "source"
            [ attr "url" url ]
            [ str source ]
        |> Source

module Rss =
    let renderFeed (Channel channel) =
        let rssNode = tag "rss" [ attr "version" "2.0" ] [ channel ]
        renderXmlDocument rssNode

module StaticSite =
    open Fake.StaticGen

    let withRssFeed createChannel url site =
        let channel = createChannel site
        let file = { Url = url; Content = channel |> Rss.renderFeed }
        site |> StaticSite.withFiles [ file ]
