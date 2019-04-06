module Fake.StaticGen.Test

open Expecto
open System

open Fake.StaticGen.Markdown

let concatn lines = String.concat Environment.NewLine lines

let tests =
    testList "Markdown" [
        test "Split YAML frontmatter" {
            let markdown = 
                [ "---"
                  "key: value"
                  "some text: '---'"
                  "---"
                  ""
                  "# Hello world!" ] |> concatn

            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "key: value"; "some text: '---'" ] |> concatn |> Markdown.Yaml) "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatn) "Correct content"
        } 

        test "Split TOML frontmatter" {
            let markdown = 
                [ "+++"
                  "key = \"+++\""
                  "number = 5"
                  "+++"
                  ""
                  "# Hello world!" ] |> concatn

            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "key = \"+++\""; "number = 5" ] |> concatn |> Markdown.Toml) "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatn) "Correct content"
        }

        test "Split oneline JSON frontmatter" {
            let markdown =
                [ """{ "test": "value", "other": 24 }"""
                  ""
                  "# Hello world!" ] |> concatn

            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter (Markdown.Json """{ "test": "value", "other": 24 }""") "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatn) "Correct content"
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
            Expect.equal frontmatter ([ "{"; """  "test": "value","""; """  "other": 24"""; "}" ] |> concatn |> Markdown.Json) 
                "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatn) "Correct content"
        }

        test "Split nested-object JSON frontmatter" {
            let markdown =
                [ "{"
                  """  "test": {"""
                  """    "other": 24"""
                  "  }"
                  "}"
                  ""
                  "# Hello world!" ] |> concatn
            
            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "{"; """  "test": {"""; """    "other": 24"""; "  }"; "}" ] |> concatn |> Markdown.Json)
                "Correct frontmatter"
            Expect.equal content ([ ""; "# Hello world!" ] |> concatn) "Correct content"
        }

        test "Split with no frontmatter" {
            let markdown =
                [ "# Hello world!"
                  ""
                  "Lorem ipsum dolor sit amet etc..." ] |> concatn
            
            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter Markdown.None "Correct frontmatter"
            Expect.equal content markdown "Correct content"
        }

        test "Split unclosed TOML frontmatter" {
            let markdown =
                [ "+++"
                  "value = \"hello\""
                  "other = 23" ] |> concatn

            let frontmatter, content = Markdown.splitFrontmatter markdown
            Expect.equal frontmatter ([ "value = \"hello\""; "other = 23" ] |> concatn |> Markdown.Toml) "Correct frontmatter"
            Expect.equal content "" "Correct content"
        }
    ]

[<EntryPoint>]
let main args = 
    runTestsWithArgs defaultConfig args tests
