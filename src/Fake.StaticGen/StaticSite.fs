namespace Fake.StaticGen

open Fake.IO

type Page<'content> =
    { Url : string
      Content : 'content }

type StaticSite<'config, 'content> =
    { Config : 'config
      Pages : Page<'content> list
      Files : Page<string> list }

module StaticSite =
    /// Create a new site with some configuration
    let fromConfig config =
        { Config = config 
          Pages = []
          Files = [] }
    
    /// Create a new site with some configuration parsed from a source file
    let fromConfigFile filePath parse =
        filePath |> File.readAsString |> parse |> fromConfig

    /// Add a new page
    let withPage url content site =
        let page = { Url = url; Content = content }
        { site with Pages = page::site.Pages }

    /// Add multiple pages
    let withPages pages site =
        { site with Pages = List.append pages site.Pages }

    /// Parse a source file and add it as a page
    let withPageFromSource url sourceFile parse site =
        let content = 
            File.readAsString sourceFile |> parse
        site |> withPage url content

    /// Parse multiple source files and add them as pages
    let withPagesFromSources url (sourceFiles : #seq<string>) parse site =
        let pages = 
            sourceFiles
            |> Seq.map (fun path -> 
                let content = path |> File.readAsString |> parse
                { Url = url path content
                  Content = content })
            |> Seq.toList
        site |> withPages pages

    /// Add a file
    let withFile url content site =
        { site with Files = { Url = url; Content = content }::site.Files }

    /// Add multiple files
    let withFiles files site =
        { site with Files = List.append files site.Files }

    /// Copy a source file
    let withFileFromSource url sourceFile site =
        site |> withFile url (File.readAsString sourceFile)

    /// Copy multiple source files
    let withFilesFromSources url (sourceFiles : #seq<string>) site =
        let files = 
            sourceFiles 
            |> Seq.map (fun path -> 
                let content = path |> File.readAsString
                { Url = url path content
                  Content = content })
            |> Seq.toList
        site |> withFiles files

    /// Create an overview page based on the list of all pages
    let withOverviewPage url createOverview site =
        let content = createOverview site.Pages
        site |> withPage url content

    /// Create an overview file based on the list of all pages, e.g. an RSS feed
    let withOverviewFile url createOverview site =
        let content = createOverview site.Pages
        site |> withFile url content

    let private normalizeUrl (url : string) =
        url.Replace("\\", "/").Trim().TrimEnd('/').TrimStart('/').ToLowerInvariant()

    let private pageUrlToFullUrl (url : string) =
        let url = normalizeUrl url
        if url.EndsWith(".html") then url else url.TrimEnd('/') + "/index.html"

    /// Write the site files to the `outputPath`, using the render function to convert the pages into HTML
    let generate outputPath render site =
        Directory.delete outputPath
        site.Pages
        |> List.map (fun p -> 
            { Url = pageUrlToFullUrl p.Url 
              Content = p |> render site })
        |> List.append site.Files
        |> List.iter (fun p -> 
            let url = normalizeUrl p.Url
            let path = Path.combine outputPath url |> Path.normalizeFileName
            Directory.ensure (Path.getDirectory path)
            File.writeString false path p.Content)
