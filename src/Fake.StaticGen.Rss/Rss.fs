module Fake.StaticGen.Rss

open System

type RssAttribute =
    | KeyValue of string * string
    | Boolean of string

type Channel =
    { Title : string
      Link : string
      Description : string
      Items : Item list
      Language : string option
      Copyright : string option
      ManagingEditor : string option
      WebMaster : string option
      PubDate : DateTime option
      LastBuildDate : DateTime option
      Category : Category option
      Generator : string option
      Docs : string option
      Cloud : Cloud option
      TTL : string option
      Image : Image option
      Rating : string option
      TextInput : TextInput option
      SkipHours : string option
      SkipDays : string option }

and Image =
    { Url : string
      Title : string
      Link : string
      Width : int option
      Height : int option
      Description : string option }

and Cloud = 
    { Domain : string
      Port : int
      Path : string
      RegisterProcedure : string
      Protocol : string }

and TextInput = 
    { Title : string
      Description : string
      Name : string
      Link : string }

and Item =
    { Title : string option
      Link : string option
      Description : string option
      Author : string option
      Category : Category option
      Comments : string option
      Enclosure : Enclosure option
      Guid : Guid option
      PubDate : DateTime option
      Source : string option }

and Enclosure =
    { Url : string
      Length : string
      Type : string }

and Category =
    { Category : string
      Domain : string option }

and Guid =
    { Guid : string 
      IsPermaLink : bool option }

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
                   ?ttl : string,
                   ?image : Image,
                   ?rating : string,
                   ?textInput : TextInput,
                   ?skipHours : string,
                   ?skipDays : string ) =
            { Title = title
              Link = link
              Description = description
              Items = items
              Language = language
              Copyright = copyright
              ManagingEditor = managingEditor
              WebMaster = webMaster
              PubDate = pubDate
              LastBuildDate = lastBuildDate
              Category = category
              Generator = generator
              Docs = docs
              Cloud = cloud
              TTL = ttl
              Image = image
              Rating = rating
              TextInput = textInput
              SkipHours = skipHours
              SkipDays = skipDays }