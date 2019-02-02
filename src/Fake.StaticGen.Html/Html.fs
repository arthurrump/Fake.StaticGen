namespace Fake.StaticGen.Html

open Fake.StaticGen
open Giraffe.GiraffeViewEngine

module StaticSite =
    let generateFromHtml outputPath render =
        let renderFromHtml site page =
            render site page |> renderHtmlDocument
        StaticSite.generate outputPath renderFromHtml
