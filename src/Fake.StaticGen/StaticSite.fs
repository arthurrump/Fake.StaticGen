namespace Fake.StaticGen

open Giraffe.GiraffeViewEngine
open Fake.IO

type Page<'details> =
    { Url : string
      Details : 'details
      Content : XmlNode }

type File =
    { Url : string
      Content : string }

type StaticSite<'config, 'page> =
    { Config : 'config
      Pages : Page<'page> list
      Files : File list }

type Parser<'t> = string -> 't
type FileTemplate<'config, 't> = 'config -> 't -> string
type HtmlTemplate<'config, 'details, 't> = 'config -> 'details -> 't -> XmlNode

module StaticSite =
    let fromConfig config =
        { Config = config 
          Pages = []
          Files = [] }
    
    let fromConfigFile (filePath : string) (parse : Parser<'config>) =
        filePath |> File.readAsString |> parse |> fromConfig

    let withPage (url : string) (details : 'page) (content : XmlNode) site =
        let page = { Url = url; Details = details; Content = content }
        { site with Pages = page::site.Pages }

    let withPages (pages : Page<'page> list) site =
        { site with Pages = List.append pages site.Pages }

    let withPageFromFile (url : string) (filePath : string) (parse : Parser<'details * 't>) (render : HtmlTemplate<'config, 'details, 't>) site =
        let details, content = 
            File.readAsString filePath |> parse
        site |> withPage url details (render site.Config details content)

    let withPagesFromFiles (url : 'details -> string) (files : #seq<string>) (parse : Parser<'details * 't>) (render : HtmlTemplate<'config, 'details, 't>) site =
        let pages = 
            files
            |> Seq.map (File.readAsString >> parse)
            |> Seq.map (fun (details, t) -> 
                { Url = url details
                  Details = details
                  Content = render site.Config details t })
            |> Seq.toList
        site |> withPages pages

    let withFile (url : string) (content : string) site =
        { site with Files = { Url = url; Content = content }::site.Files }

    let withFileFromPath (url : string) (filePath : string) site =
        site |> withFile url (File.readAsString filePath)

    let withFiles (files : File list) site =
        { site with Files = List.append files site.Files }

    let withOverviewPage (url : string) (pageDetails : 'page) (render : HtmlTemplate<'config, 'page, Page<'page> list>) site =
        let content = render site.Config pageDetails site.Pages
        site |> withPage url pageDetails content

    // For example for RSS feeds
    let withOverviewFile (url : string) (createFile : FileTemplate<'config, Page<'page> list>) site =
        let content = createFile site.Config site.Pages
        site |> withFile url content

    let withLayout layout site =
        { site with 
            Pages =
                site.Pages
                |> List.map (fun page -> { page with Content = layout site page }) }

    let private normalizeUrl (url : string) =
        url.Replace("\\", "/").Trim().TrimEnd('/').TrimStart('/').ToLowerInvariant()

    let private pageUrlToFullUrl (url : string) =
        let url = normalizeUrl url
        if url.EndsWith(".html") then url else url.TrimEnd('/') + "/index.html"

    let generate (outputPath : string) site =
        Directory.delete outputPath
        site.Pages
        |> List.map (fun p -> 
            { Url = pageUrlToFullUrl p.Url 
              Content = p.Content |> renderHtmlDocument })
        |> List.append site.Files
        |> List.iter (fun p -> 
            let url = normalizeUrl p.Url
            let path = Path.combine outputPath url |> Path.normalizeFileName
            Directory.ensure (Path.getDirectory path)
            File.writeString false path p.Content)
