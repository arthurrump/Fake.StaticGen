module Fake.StaticGen.Rss

open System.Net

type RssAttribute =
    | KeyValue of string * string
    | Boolean of string

type RssElement = string * RssAttribute[]

type RssNode =
    | ParentNode of RssElement * RssNode list
    | VoidElement of RssElement
    | Text of string

let inline private encode v = WebUtility.HtmlEncode v

let attr key value = KeyValue (key, value)
let flag key = Boolean key

let tag name attributes contents = ParentNode ((name, Array.ofList attributes), contents)

let voidTag name attributes = VoidElement (name, Array.ofList attributes)

let rawText content = Text content

let encodedText content = Text (encode content)
let emptyText = rawText ""
let comment content = rawText (sprintf "<!-- %s -->" content)

let str = encodedText

// Root channel tag
let channel = tag "channel"

// Required channel elements
let title = tag "title"
let link = tag "link"
let description = tag "description"

// Optional channel elements
let language = tag "language"
let copyright = tag "copyright"
let managingEditor = tag "managingEditor"
let webMaster = tag "webMaster"
let pubDate = tag "pubDate"
let lastBuildDate = tag "lastBuildDate"
let category = tag "category"
let generator = tag "generator"
let docs = tag "docs"
let cloud = tag "cloud"
let ttl = tag "ttl"
let image = tag "image"
let rating = tag "rating"
let textInput = tag "textInput"
let skipHours = tag "skipHours"
let skipDays = tag "skipDays"

// Required elements on image
let url = tag "url"
// title
// link

// Optional elements on image
let width = tag "width"
let height = tag "height"
// description

// Attributes on cloud
let _domain = attr "domain"
let _port = attr "port"
let _path = attr "path"
let _registerProcedure = attr "registerProcedure"
let _protocol = attr "protocol"

// Required elements on textInput
// title
// description
let name = tag "name"
// link

let item = tag "item"

// Optional elements on item
// title
// link
// description
let author = tag "author"
// category
let comments = tag "comments"
let enclosure = tag "enclosure"
let guid = tag "guid"
// pubDate
let source = tag "source"

// Required attribute on source
let _url = attr "url"

// Required attributes on enclosure
// url
let _length = attr "length"
let _type = attr "type"

// Optional attribute on category
// domain

// Optional attribute on guid
let _isPermaLink = attr "isPermaLink"
