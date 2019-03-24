namespace Fake.StaticGen.Markdown

open Fake.Core
open Fake.StaticGen
open Markdig
open System

type MarkdownPage<'t> =
    { Frontmatter : Page<'t>
      RenderedMarkdown : string }

module Markdown =
    let splitFrontmatter markdown =
        let concatn lines = String.concat Environment.NewLine lines
        let lines = markdown |> String.splitStr Environment.NewLine
        
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
    let withPagesFromMarkdown markdownPages parseFrontmatter =
        markdownPages
        |> Seq.map (fun content ->
            let frontmatter, markdown = Markdown.splitFrontmatter content
            { Frontmatter = parseFrontmatter frontmatter
              RenderedMarkdown = Markdown.ToHtml markdown })
