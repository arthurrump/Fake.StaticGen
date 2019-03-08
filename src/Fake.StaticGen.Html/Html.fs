namespace Fake.StaticGen.Html

open Fake.StaticGen
open Fake.StaticGen.Html.ViewEngine

module StaticSite =
    /// Generate the site and write it to the `outputPath` using an HTML 
    /// template defined with a DSL based on the GiraffeViewEngine
    let generateFromHtml outputPath template =
        let renderFromHtml site page =
            template site page |> renderHtmlDocument
        StaticSite.generate outputPath renderFromHtml
