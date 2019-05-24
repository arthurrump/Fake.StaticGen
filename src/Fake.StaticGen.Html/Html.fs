namespace Fake.StaticGen.Html

open Fake.StaticGen
open Fake.StaticGen.Html.ViewEngine

[<AutoOpen>]
module Html =
    type SiteBuilder<'config, 'components, 'page> with
        [<CustomOperation("htmlTemplate")>]
        member this.HtmlTemplate (state, template) =
            let htmlRenderTemplate site page = template site page |> renderHtmlDocument
            this.Template(state, htmlRenderTemplate)
