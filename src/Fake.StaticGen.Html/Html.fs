namespace Fake.StaticGen.Html

open Fake.StaticGen
open Giraffe.GiraffeViewEngine

module StaticSite =
    /// Generate the site and write it to the `outputPath` using an HTML 
    /// template defined with GiraffeViewEngine's HTML DSL
    let generateFromHtml outputPath template =
        let renderFromHtml site page =
            template site page |> renderHtmlDocument
        StaticSite.generate outputPath renderFromHtml
