module Fake.StaticGen.Html

open Fake.StaticGen
open Fake.StaticGen.Html.ViewEngine

type SiteBuilder<'config, 'components, 'page> with
    [<CustomOperation("htmlTemplate")>]
    member this.HtmlTemplate (state, template) =
        let htmlRenderTemplate site page = template site page |> renderHtmlDocument
        this.Template(state, htmlRenderTemplate)

module StaticSite =
    let site : SiteBuilderState<{|Title:string|}, obj, string> = 
        staticsite {
            config {| Title = "Arthur's Blog" |}

            page "/" "Welcome home!"

            htmlTemplate (fun helpers page -> 
                html [] [ 
                    head [] [ title [] [ str helpers.Config.Value.Title ] ]
                    body [] [ str page.Content ] 
                ])
        }
