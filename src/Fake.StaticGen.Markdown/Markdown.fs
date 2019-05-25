module Fake.StaticGen.Markdown

open Fake.Core
open Fake.StaticGen
open Markdig
open System

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

let [<Literal>] private pipelineKey = "Fake.StaticGen.Markdown.Pipeline"
let private defaultPipeline = MarkdownPipelineBuilder().Build()
let private getPipeline state =
    state.ExtensionValues 
    |> Map.tryFind pipelineKey 
    |> Option.map unbox 
    |> Option.defaultValue defaultPipeline

type SiteBuilder<'config, 'components, 'page> with
    [<CustomOperation("markdownPipeline")>]
    /// Set the <see cref="MarkdownPipeline"/> used to render markdown pages
    member __.MarkdownPipeline(state, pipeline : MarkdownPipeline) =
        { state with ExtensionValues = state.ExtensionValues |> Map.add pipelineKey (box pipeline) }

    /// Add multiple pages from markdown files
    [<CustomOperation("markdownPageSources")>]
    member this.MarkdownPageSources(state, urlMapper, sourceFiles, parse) =
        let parseMarkdown path content =
            let frontmatter, markdown = splitFrontmatter content
            let rendered = Markdown.ToHtml (markdown, getPipeline state)
            parse path frontmatter rendered

        let pageSources =
            sourceFiles
            |> Seq.map (fun path ->
                { Source = path
                  Url = urlMapper path
                  Parser = parseMarkdown })

        this.PageSources(state, pageSources)

    /// Add a page from a markdown file
    [<CustomOperation("markdownPageSource")>]
    member this.MarkdownPageSource(state, url, sourceFile, parse) =
        this.MarkdownPageSources(state, (fun _ -> url), [ sourceFile ], parse)
