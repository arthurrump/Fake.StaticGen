// ---------------------------
// Attribution to original authors of this code
// ---------------------------
// This code has been originally ported from Suave to Giraffe and then
// adapted in this project for general XML.
//
// Giraffe: Licensed under the Apache License, Version 2.0
//          Copyright 2018 Dustin Moris Gorski and contributors
// Suave:   Licensed under the Apache License, Version 2.0
//          Copyright 2018 Ademar Gonzalez, Henrik Feldt and contributors
// 
// You can find the original implementations here:
// https://github.com/SuaveIO/suave/blob/master/src/Experimental/Html.fs
// https://github.com/giraffe-fsharp/Giraffe/blob/master/src/Giraffe/GiraffeViewEngine.fs

module Fake.StaticGen.XmlEngine

open System.Net
open System.Text

type XmlAttribute =
    | KeyValue of string * string
    | Boolean  of string

type XmlElement   = string * XmlAttribute[]    // Name * XML attributes

type XmlNode =
    | ParentNode  of XmlElement * XmlNode list // An XML element which contains nested XML elements
    | VoidElement of XmlElement                // An XML element which cannot contain nested XML (e.g. <hr /> or <br />)
    | Text        of string                    // Text content

// ---------------------------
// Helper functions
// ---------------------------

let inline private encode v = WebUtility.HtmlEncode v

// ---------------------------
// Building blocks
// ---------------------------

let attr (key : string) (value : string) = KeyValue (key, encode value)
let flag (key : string) = Boolean key

let tag (tagName    : string)
        (attributes : XmlAttribute list)
        (contents   : XmlNode list) =
    ParentNode ((tagName, Array.ofList attributes), contents)

let voidTag (tagName    : string)
            (attributes : XmlAttribute list) =
    VoidElement (tagName, Array.ofList attributes)

let inline eTag name = tag name []

/// **Description**
/// The `rawText` function will create an object of type `XmlNode` where the content will be rendered in its original form (without encoding).
/// **Special Notice**
/// Please be aware that the the usage of `rawText` is mainly designed for edge cases where someone would purposefully want to inject XML code into a rendered view. 
/// If not used carefully this could potentially lead to serious security vulnerabilities and therefore should be used only when explicitly required.
/// Most cases and particularly any user provided content should always be output via the `encodedText` function.
let rawText     (content : string) = Text content

/// **Description**
/// The `encodedText` function will output a string where the content has been HTML encoded.
let encodedText (content : string) = Text (encode content)
let emptyText                      = rawText ""
let comment     (content : string) = rawText (sprintf "<!-- %s -->" content)

/// **Description**
/// An alias for the `encodedText` function.
let str = encodedText

// ---------------------------
// Build HTML/XML views
// ---------------------------

[<RequireQualifiedAccess>]
module ViewBuilder =
    let inline private (+=) (sb : StringBuilder) (text : string) = sb.Append(text)
    let inline private (+!) (sb : StringBuilder) (text : string) = sb.Append(text) |> ignore

    let inline private selfClosingBracket (isHtml : bool) =
        if isHtml then ">" else " />"

    let rec private buildNode (isHtml : bool) (sb : StringBuilder) (node : XmlNode) : unit =

        let buildElement closingBracket (elemName, attributes : XmlAttribute array) =
            match attributes with
            | [||] -> do sb += "<" += elemName +! closingBracket
            | _    ->
                do sb += "<" +! elemName

                attributes
                |> Array.iter (fun attr ->
                    match attr with
                    | KeyValue (k, v) -> do sb += " " += k += "=\"" += v +! "\""
                    | Boolean k       -> do sb += " " +! k)

                do sb +! closingBracket

        let inline buildParentNode (elemName, attributes : XmlAttribute array) (nodes : XmlNode list) =
            do buildElement ">" (elemName, attributes)
            for node in nodes do buildNode isHtml sb node
            do sb += "</" += elemName +! ">"

        match node with
        | Text text             -> do sb +! text
        | ParentNode (e, nodes) -> do buildParentNode e nodes
        | VoidElement e         -> do buildElement (selfClosingBracket isHtml) e

    let buildXmlNode  = buildNode false
    let buildHtmlNode = buildNode true

    let buildXmlNodes  sb (nodes : XmlNode list) = for n in nodes do buildXmlNode sb n
    let buildHtmlNodes sb (nodes : XmlNode list) = for n in nodes do buildHtmlNode sb n

// ---------------------------
// Render XML views
// ---------------------------

let renderXmlNode (node : XmlNode) : string =
    let sb = new StringBuilder() in ViewBuilder.buildXmlNode sb node
    sb.ToString()

let renderXmlNodes (nodes : XmlNode list) : string =
    let sb = new StringBuilder() in ViewBuilder.buildXmlNodes sb nodes
    sb.ToString()

let renderXmlDocument (node : XmlNode) : string =
    let sb = StringBuilder().AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""")
    ViewBuilder.buildXmlNode sb node
    sb.ToString()
