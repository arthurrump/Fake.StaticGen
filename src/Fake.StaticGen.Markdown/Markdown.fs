namespace Fake.StaticGen.Markdown

open Fake.Core
open Fake.StaticGen
open Markdig
open System

module Markdown =
    let splitFrontmatter (markdown : string) =
        let concatn lines = String.concat Environment.NewLine lines
        let lines = markdown.Split([| "\r\n"; "\n"; "\r" |], StringSplitOptions.None) |> List.ofArray
        
        let splitLines splitStr lines =
            let first = lines |> Seq.takeWhile (fun l -> l <> splitStr)
            let second = 
                if Seq.length first = Seq.length lines 
                then Seq.empty 
                else lines |> Seq.skip (Seq.length first + 1)
            first |> concatn, second |> concatn

        match lines with
        | "---"::tail ->
            let f, c = tail |> splitLines "---"
            Some f, c
        | "+++"::tail -> 
            let f, c = tail |> splitLines "+++"
            Some f, c
        | first::_ when first.StartsWith "{" -> 
            let rec splitJson json (rest : string list) openBrackets =
                match rest with
                | [] -> json |> List.rev, []
                | r::tail ->
                    let chars = r.ToCharArray() |> Array.countBy id |> Map.ofArray
                    let opens = chars |> Map.tryFind '{' |> Option.defaultValue 0
                    let closes = chars |> Map.tryFind '}' |> Option.defaultValue 0
                    let openBrackets = openBrackets + opens - closes
                    if openBrackets < 1 then
                        (r::json) |> List.rev, tail
                    else
                        splitJson (r::json) tail openBrackets
            let f, c = splitJson [] lines 0
            Some (f |> concatn), c |> concatn
        | _ -> 
            None, markdown

module StaticSite =
    /// Add multiple pages from markdown files with a custom Markdig MarkdownPipeline
    let withPagesFromCustomMarkdown pipeline sourceFiles parse =
        let parseMarkdown path content =
            let frontmatter, markdown = Markdown.splitFrontmatter content
            let rendered = Markdown.ToHtml (markdown, pipeline)
            parse path frontmatter rendered
        StaticSite.withPagesFromSources sourceFiles parseMarkdown

    /// Add multiple pages from markdown files
    let withPagesFromMarkdown sourceFiles parse =
        withPagesFromCustomMarkdown (MarkdownPipelineBuilder().Build()) sourceFiles parse

    /// Add a page from a markdown file with a custom Markdig MarkdownPipeline
    let withPageFromCustomMarkdown pipeline sourceFile parse =
        withPagesFromCustomMarkdown pipeline [ sourceFile ] parse

    /// Add a page from a markdown file
    let withPageFromMarkdown sourceFile parse =
        withPagesFromMarkdown [ sourceFile ] parse
