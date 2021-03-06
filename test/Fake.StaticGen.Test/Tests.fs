﻿module Fake.StaticGen.Test

open Expecto
open System

open Fake.StaticGen.Markdown

let concatrn lines = String.concat "\r\n" lines
let concatn lines = String.concat "\n" lines
let concatne lines = String.concat Environment.NewLine lines

let frontmatterTests =
    testList "Markdown Frontmatter splitting" [
        test "Split YAML frontmatter" {
            let markdown = 
                [ "---"
                  "key: value"
                  "some text: '---'"
                  "---"
                  ""
                  "# Hello world!" ] |> concatn

            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "key: value"; "some text: '---'" ] |> concatne |> Some) "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatne) "Correct content"
        } 

        test "Split TOML frontmatter" {
            let markdown = 
                [ "+++"
                  "key = \"+++\""
                  "number = 5"
                  "+++"
                  ""
                  "# Hello world!" ] |> concatrn

            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "key = \"+++\""; "number = 5" ] |> concatne |> Some) "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatne) "Correct content"
        }

        test "Split oneline JSON frontmatter" {
            let markdown =
                [ """{ "test": "value", "other": 24 }"""
                  ""
                  "# Hello world!" ] |> concatn

            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter (Some """{ "test": "value", "other": 24 }""") "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatne) "Correct content"
        }

        test "Split multiline JSON frontmatter" {
            let markdown =
                [ "{"
                  """  "test": "value","""
                  """  "other": 24"""
                  "}"
                  ""
                  "# Hello world!" ] |> concatn
            
            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "{"; """  "test": "value","""; """  "other": 24"""; "}" ] |> concatne |> Some) 
                "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatne) "Correct content"
        }

        test "Split nested-object JSON frontmatter" {
            let markdown =
                [ "{"
                  """  "test": {"""
                  """    "other": 24"""
                  "  }"
                  "}"
                  ""
                  "# Hello world!" ] |> concatrn
            
            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "{"; """  "test": {"""; """    "other": 24"""; "  }"; "}" ] |> concatne |> Some)
                "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatne) "Correct content"
        }

        test "Split with no frontmatter" {
            let markdown =
                [ "# Hello world!"
                  ""
                  "Lorem ipsum dolor sit amet etc..." ] |> concatn
            
            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter None "Correct frontmatter"
            Expect.equal content markdown "Correct content"
        }

        test "Split unclosed TOML frontmatter" {
            let markdown =
                [ "+++"
                  "value = \"hello\""
                  "other = 23" ] |> concatn

            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "value = \"hello\""; "other = 23" ] |> concatne |> Some) "Correct frontmatter"
            Expect.equal content "" "Correct content"
        }
    ]

let tests =
    testList "All tests" [ frontmatterTests ]

[<EntryPoint>]
let main args = 
    runTestsWithArgs defaultConfig args tests
