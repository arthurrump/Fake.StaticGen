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
    let withPage content url site =
        let page = { Url = url; Content = content }
        { site with Pages = page::site.Pages }

    /// Add multiple pages
    let withPages pages site =
        { site with Pages = List.append pages site.Pages }

    /// Parse a source file and add it as a page
    let withPageFromSource sourceFile parse site =
        let page = File.readAsString sourceFile |> parse sourceFile
        site |> withPages [ page ]

    /// Parse multiple source files and add them as pages
    let withPagesFromSources (sourceFiles : #seq<string>) parse site =
        let pages = 
            sourceFiles
            |> Seq.map (fun path -> path |> File.readAsString |> parse path)
            |> Seq.toList
        site |> withPages pages

    /// Add a file
    let withFile content url site =
        let file = { Url = url; Content = content }
        { site with Files = file::site.Files }

    /// Add multiple files
    let withFiles files site =
        { site with Files = List.append files site.Files }

    /// Copy a source file
    let withFileFromSource sourceFile url site =
        site |> withFile (File.readAsString sourceFile) url

    /// Copy multiple source files
    let withFilesFromSources (sourceFiles : #seq<string>) urlMapper site =
        let files = 
            sourceFiles 
            |> Seq.map (fun path -> 
                let content = path |> File.readAsString
                { Url = urlMapper path
                  Content = content })
            |> Seq.toList
        site |> withFiles files

    /// Create an overview page based on the list of all pages
    let withOverviewPage createOverview site =
        let page = createOverview site.Pages
        site |> withPages [ page ]

    /// Create multiple overview pages based on the list of all pages
    let withOverviewPages createOverviewPages site =
        site |> withPages (createOverviewPages site.Pages)

    /// Create a paginated overview with a specified number of items per page
    let withPaginatedOverview itemsPerPage chooser createOverviewPages site =
        let chunks =
            site.Pages 
            |> List.choose chooser
            |> List.chunkBySize itemsPerPage
        site |> withPages (createOverviewPages chunks)

    /// Create an overview file based on the list of all pages, e.g. an RSS feed
    let withOverviewFile createOverview site =
        let file = createOverview site.Pages
        site |> withFiles [ file ]

    let private normalizeUrl (url : string) =
        url.Replace("\\", "/").Trim().TrimEnd('/').TrimStart('/').ToLowerInvariant()

    let private pageUrlToFullUrl (url : string) =
        let url = normalizeUrl url
        if url.EndsWith(".html") then url else url.TrimEnd('/') + "/index.html"

    // Dry run generate, returning a map of file paths and contents, instead of writing them out to disk
    let generateDry outputPath render site =
        site.Pages
        |> List.map (fun p ->
            { Url = pageUrlToFullUrl p.Url
              Content = p |> render site })
        |> List.append site.Files
        |> List.map (fun f ->
            let url = normalizeUrl f.Url
            let path = Path.combine outputPath url |> Path.normalizeFileName
            path, f.Content)
        |> Map.ofList

    /// Write the site files to the `outputPath`, using the render function to convert the pages into HTML
    let generate outputPath render site =
        Directory.delete outputPath
        site
        |> generateDry outputPath render
        |> Map.iter (fun path content -> 
            Directory.ensure (Path.getDirectory path)
            File.writeString false path content)
