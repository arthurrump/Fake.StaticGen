namespace Fake.StaticGen.Rss

open System

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
    /// <summary>The main channel element of the feed, containing information about the channel (metadata) and its contents.</summary>
    /// <param name="title">The name of the channel. It's how people refer to your service. If you have an HTML website that contains the same information as your RSS file, the title of your channel should be the same as the title of your website.</param>
    /// <param name="link">The URL to the HTML website corresponding to the channel.</param>
    /// <param name="description">Phrase or sentence describing the channel.</param>
    /// <param name="items">The list of items in the channel.</param>
    /// <param name="language">The language the channel is written in. This allows aggregators to group all Italian language sites, for example, on a single page. Allowable values are W3C language codes.</param>
    /// <param name="copyright">Copyright notice for content in the channel.</param>
    /// <param name="managingEditor">Email address for person responsible for editorial content.</param>
    /// <param name="webMaster">Email address for person responsible for technical issues relating to channel.</param>
    /// <param name="pubDate">The publication date for the content in the channel. For example, the New York Times publishes on a daily basis, the publication date flips once every 24 hours. That's when the pubDate of the channel changes. All date-times in RSS conform to the Date and Time Specification of RFC 822, with the exception that the year may be expressed with two characters or four characters (four preferred).</param>
    /// <param name="lastBuildDate">The last time the content of the channel changed.</param>
    /// <param name="category">Specify one or more categories that the channel belongs to.</param>
    /// <param name="generator">A string indicating the program used to generate the channel, e.g. Fake.StaticGen.</param>
    /// <param name="docs">A URL that points to the documentation for the format used in the RSS file. It's probably a pointer to the specification. It's for people who might stumble across an RSS file on a Web server 25 years from now and wonder what it is.</param>
    /// <param name="cloud">Allows processes to register with a cloud to be notified of updates to the channel, implementing a lightweight publish-subscribe protocol for RSS feeds.</param>
    /// <param name="ttl">ttl stands for time to live. It's a number of minutes that indicates how long a channel can be cached before refreshing from the source.</param>
    /// <param name="image">Specifies a GIF, JPEG or PNG image that can be displayed with the channel.</param>
    /// <param name="rating">The PICS rating for the channel.</param>
    /// <param name="textInput">Specifies a text input box that can be displayed with the channel.</param>
    /// <param name="skipHours">A hint for aggregators telling them which hours they can skip. This element contains up to 24 <hour> sub-elements whose value is a number between 0 and 23, representing a time in GMT, when aggregators, if they support the feature, may not read the channel on hours listed in the <skipHours> element. The hour beginning at midnight is hour zero.</param>
    /// <param name="skipDays">A hint for aggregators telling them which days they can skip. This element contains up to seven <day> sub-elements whose value is Monday, Tuesday, Wednesday, Thursday, Friday, Saturday or Sunday. Aggregators may not read the channel during days listed in the <skipDays> element.</param>
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
             ?categories : Category list,
             ?generator : string,
             ?docs : string,
             ?cloud : Cloud,
             ?ttl : int,
             ?image : Image,
             ?rating : string,
             ?textInput : TextInput,
             ?skipHours : int list,
             ?skipDays : string list) =
        Channel <|
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
            match categories with Some cats -> yield! cats |> List.map (fun (Category c) -> c) | _ -> ()
            match generator with Some x -> yield eTag "generator" [ str x ] | _ -> ()
            match docs with Some x -> yield eTag "docs" [ str x ] | _ -> ()
            match cloud with Some (Cloud x) -> yield x | _ -> ()
            match ttl with Some x -> yield eTag "ttl" [ str (string x) ] | _ -> ()
            match image with Some (Image x) -> yield x | _ -> ()
            match rating with Some x -> yield eTag "rating" [ str x ] | _ -> ()
            match textInput with Some (TextInput x) -> yield x | _ -> ()
            match skipHours with Some x -> yield eTag "skipHours" [ for h in x -> eTag "hour" [ str (string h) ] ] | _ -> ()
            match skipDays with Some x -> yield eTag "skipDays" [ for d in x -> eTag "day" [ str d ] ] | _ -> ()
            yield eTag "items" (items |> List.map (fun (Item i) -> i))
        ]

    /// <summary>Specifies a GIF, JPEG or PNG image that can be displayed with the channel.</summary>
    /// <param name="url">The URL of a GIF, JPEG or PNG image that represents the channel.</param>
    /// <param name="title">Describes the image, it's used in the ALT attribute of the HTML <img> tag when the channel is rendered in HTML.</param>
    /// <param name="link">The URL of the site, when the channel is rendered, the image is a link to the site. (Note, in practice the image <title> and <link> should have the same value as the channel's <title> and <link>.</param>
    /// <param name="width">The width of the image in pixels. Maximum value for width is 144, default value is 88.</param>
    /// <param name="height">The height of the image in pixels. Maximum value for height is 400, default value is 31.</param>
    /// <param name="description">Contains text that is included in the TITLE attribute of the link formed around the image in the HTML rendering.</param>
    static member Image
            (url : string,
             title : string,
             link : string,
             ?width : int,
             ?height : int,
             ?description : string) =
        Image <|
        eTag "image" [
            yield eTag "url" [ str url ]
            yield eTag "title" [ str title ]
            yield eTag "link" [ str link ]
            match width with Some x -> yield eTag "width" [ str (string x) ] | _ -> ()
            match height with Some x -> yield eTag "height" [ str (string x) ] | _ -> ()
            match description with Some x -> yield eTag "description" [ str x ] | _ -> ()
        ]

    /// <summary>Specifies a web service that supports the rssCloud interface which can be implemented in HTTP-POST, XML-RPC or SOAP 1.1. Its purpose is to allow processes to register with a cloud to be notified of updates to the channel, implementing a lightweight publish-subscribe protocol for RSS feeds. See also: http://www.rssboard.org/rsscloud-interface</summary>
    static member Cloud
            (domain : string,
             port : int,
             path : string,
             registerProcedure : string,
             protocol : string) =
        Cloud <|
        voidTag "cloud" [
            attr "domain" domain
            attr "port" (string port)
            attr "path" path
            attr "registerProcedure" registerProcedure
            attr "protocol" protocol
        ]

    /// <summary>The purpose of the <textInput> element is something of a mystery. You can use it to specify a search engine box. Or to allow a reader to provide feedback. Most aggregators ignore it.</summary>
    /// <param name="title">The label of the Submit button in the text input area.</param>
    /// <param name="description">Explains the text input area.</param>
    /// <param name="name">The name of the text object in the text input area.</param>
    /// <param name="link">The URL of the CGI script that processes text input requests.</param>
    static member TextInput
            (title : string,
             description : string,
             name : string,
             link : string) =
        TextInput <|
        eTag "textInput" [
            eTag "title" [ str title ]
            eTag "description" [ str description ]
            eTag "name" [ str name ]
            eTag "link" [ str link ]
        ]

    /// <summary>An item may represent a "story" -- much like a story in a newspaper or magazine; if so its description is a synopsis of the story, and the link points to the full story. An item may also be complete in itself, if so, the description contains the text (entity-encoded HTML is allowed; see examples), and the link and title may be omitted. All elements of an item are optional, however at least one of title or description must be present.</summary>
    /// <param name="title">The title of the item.</param>
    /// <param name="link">The URL of the item.</param>
    /// <param name="description">The item synopsis. Uses standard HTML encoding.</param>
    /// <param name="author">It's the email address of the author of the item. For newspapers and magazines syndicating via RSS, the author is the person who wrote the article that the <item> describes. For collaborative weblogs, the author of the item might be different from the managing editor or webmaster. For a weblog authored by a single individual it would make sense to omit the <author> element.It's the email address of the author of the item. For newspapers and magazines syndicating via RSS, the author is the person who wrote the article that the <item> describes. For collaborative weblogs, the author of the item might be different from the managing editor or webmaster. For a weblog authored by a single individual it would make sense to omit the <author> element.</param>
    /// <param name="categories">Includes the item in one or more categories.</param>
    /// <param name="comments">If present, it is the url of the comments page for the item.</param>
    /// <param name="enclosure">Describes a media object that is attached to the item.</param>
    /// <param name="guid">A string that uniquely identifies the item.</param>
    /// <param name="pubDate">Its value is a date, indicating when the item was published. If it's a date in the future, aggregators may choose to not display the item until that date.</param>
    /// <param name="source">The RSS channel that the item came from.</param>
    static member Item
            (?title : string,
             ?link : string,
             ?description : string,
             ?author : string,
             ?categories : Category list,
             ?comments : string,
             ?enclosure : Enclosure,
             ?guid : Guid,
             ?pubDate : DateTime,
             ?source : Source) =
        Item <|
        eTag "item" [
            match title with Some x -> yield eTag "title" [ str x ] | _ -> ()
            match link with Some x -> yield eTag "link" [ str x ] | _ -> ()
            match description with Some x -> yield eTag "description" [ str x ] | _ -> ()
            match author with Some x -> yield eTag "author" [ str x ] | _ -> ()
            match categories with Some cats -> yield! cats |> List.map (fun (Category c) -> c) | _ -> ()
            match comments with Some x -> yield eTag "comments" [ str x ] | _ -> ()
            match enclosure with Some (Enclosure x) -> yield x | _ -> ()
            match guid with Some (Guid x) -> yield x | _ -> ()
            match pubDate with Some x -> yield eTag "pubDate" [ str (x.ToString("r")) ] | _ -> ()
            match source with Some (Source x) -> yield x | _ -> ()
        ]

    /// <summary>Describes a media object that is attached to the item.</summary>
    /// <param name="url">Where the enclosure is located. Must be an http url.</param>
    /// <param name="length">How big the enclosure is in bytes.</param>
    /// <param name="type">What its type is, a standard MIME type.</param>
    static member Enclosure
            (url : string,
             length : int,
             ``type`` : string) =
        Item <|
        voidTag "enclosure" [
            attr "url" url
            attr "length" (string length)
            attr "type" ``type``
        ]

    /// <summary>A category for an item or channel.</summary>
    /// <param name="category">A forward-slash-separated string that identifies a hierarchic location in the indicated taxonomy. Processors may establish conventions for the interpretation of categories.</param>
    /// <param name="domain">A string that identifies a categorization taxonomy</param>
    static member Category
            (category : string,
             ?domain : string) =
        Category <|
        tag "category" 
            [ match domain with Some x -> yield attr "domain" x | _ -> () ]
            [ str category ]
    
    /// <summary>guid stands for globally unique identifier. It's a string that uniquely identifies the item. When present, an aggregator may choose to use this string to determine if an item is new. There are no rules for the syntax of a guid. Aggregators must view them as a string. It's up to the source of the feed to establish the uniqueness of the string.</summary>
    /// <param name="guid">The provided guid.</param>
    /// <param name="isPermaLink">If true, the reader may assume that it is a permalink to the item, that is, a url that can be opened in a Web browser, that points to the full item described by the <item> element. The default value is true. If its value is false, the guid may not be assumed to be a url, or a url to anything in particular.</param>
    static member Guid
            (guid : string,
             ?isPermaLink : bool) =
        Guid <|
        tag "guid"
            [ match isPermaLink with Some x -> yield attr "isPermaLink" (string x) | _ -> () ]
            [ str guid ]

    /// <summary>The purpose of this element is to propagate credit for links, to publicize the sources of news items. It can be used in the Post command of an aggregator. It should be generated automatically when forwarding an item from an aggregator to a weblog authoring tool.</summary>
    /// <param name="source">The name of the RSS channel that the item came from, derived from its <title>.</param>
    /// <param name="url">Links to the XMLization of the source.</param>
    static member Source
            (source : string,
             url : string) =
        Source <|
        tag "source"
            [ attr "url" url ]
            [ str source ]

module Rss =
    /// Create an RSS feed string from a Channel object
    let renderFeed (Channel channel) =
        let rssNode = tag "rss" [ attr "version" "2.0" ] [ channel ]
        renderXmlDocument rssNode

module StaticSite =
    open Fake.StaticGen

    /// Add an RSS feed with a function that takes the site and returns an RSS Channel object
    let withRssFeed createChannel url site =
        let channel = createChannel site
        let file = { Url = url; Content = channel |> Rss.renderFeed }
        site |> StaticSite.withFiles [ file ]
