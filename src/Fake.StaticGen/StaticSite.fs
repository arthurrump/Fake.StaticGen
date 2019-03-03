namespace Fake.StaticGen

open Fake.IO
open System.IO

[<AutoOpen>]
module private UrlHelpers =
    let normalizeUrl (url : string) =
        url.Replace("\\", "/").Trim().TrimEnd('/').TrimStart('/').ToLowerInvariant()

    let normalizeRelativeUrl url =
        "/" + (normalizeUrl url)

    let pageUrlToFilePath (url : string) =
        let url = normalizeUrl url
        if System.IO.Path.HasExtension url then url else url + "/index.html"

/// Function that takes the source file name and its contents and returns the parsed content and the url
type PageParser<'content> = string -> string -> ('content * string)

type Page<'content> =
    | FromSource of sourceFile : string * parser : PageParser<'content>
    | FromValue of content : 'content * url : string

    member this.Content = 
        match this with
        | FromSource (source, parser) -> File.readAsString source |> parser source |> fst
        | FromValue (content, _) -> content

    member this.Url =
        match this with
        | FromSource (source, parser) -> File.readAsString source |> parser source |> snd
        | FromValue (_, url) -> url
        |> normalizeRelativeUrl

type StaticSite<'config, 'content> =
    { BaseUrl : string
      Config : 'config
      Pages : Page<'content> list
      Files : Page<string> list }

    member this.AbsoluteUrl relativeUrl =
        this.BaseUrl + "/" + (normalizeUrl relativeUrl)

type ISiteConfig =
    abstract member BaseUrl : string

module StaticSite =
    /// Create a new site with a base url and some configuration
    let fromConfig baseUrl config =
        { BaseUrl = normalizeUrl baseUrl
          Config = config
          Pages = [] 
          Files = [] }

    /// Create a new site with a base url and some configuration parsed from a source file
    let fromConfigFile baseUrl filePath parse =
        filePath |> File.readAsString |> parse |> fromConfig baseUrl

    /// Create a new site with some configuration that includes the BaseUrl
    let fromIConfig (config : #ISiteConfig) =
        fromConfig config.BaseUrl config
    
    /// Create a new site with some configuration that includes the BaseUrl 
    /// parsed from a source file
    let fromIConfigFile filePath parse =
        filePath |> File.readAsString |> parse |> fromIConfig

    /// Add a new page
    let withPage content url site =
        let page = Page.FromValue (content, normalizeRelativeUrl url)
        { site with Pages = page::site.Pages }

    /// Add multiple pages
    let withPages pages site =
        { site with Pages = List.append pages site.Pages }

    /// Parse a source file and add it as a page
    let withPageFromSource sourceFile parse site =
        let page = Page.FromSource (sourceFile, parse)
        site |> withPages [ page ]

    /// Parse multiple source files and add them as pages
    let withPagesFromSources (sourceFiles : #seq<string>) parse site =
        let pages = 
            sourceFiles
            |> Seq.map (fun path -> Page.FromSource (path, parse))
            |> Seq.toList
        site |> withPages pages

    /// Add a file
    let withFile content url site =
        let file = Page.FromValue (content, normalizeRelativeUrl url)
        { site with Files = file::site.Files }

    /// Add multiple files
    let withFiles files site =
        { site with Files = List.append files site.Files }

    let private fileParser url : PageParser<string> =
        fun _ content -> (content, url)

    /// Copy a source file
    let withFileFromSource sourceFile url site =
        site |> withFiles [ Page.FromSource (sourceFile, fileParser url) ]

    /// Copy multiple source files
    let withFilesFromSources (sourceFiles : #seq<string>) urlMapper site =
        let files = 
            sourceFiles 
            |> Seq.map (fun path -> Page.FromSource (path, fileParser (urlMapper path)))
            |> Seq.toList
        site |> withFiles files

    /// Create an overview page based on the list of all pages
    let withOverviewPage createOverview site =
        let content, url = createOverview site
        site |> withPage content url

    /// Create multiple overview pages based on the list of all pages
    let withOverviewPages createOverviewPages site =
        let pages = 
            createOverviewPages site
            |> Seq.map (fun (content, url) -> Page.FromValue (content, url))
            |> Seq.toList
        site |> withPages pages

    /// Create a paginated overview with a specified number of items per page
    let withPaginatedOverview itemsPerPage chooser createOverviewPages site =
        let chunks =
            site.Pages 
            |> List.choose chooser
            |> List.chunkBySize itemsPerPage
        site |> withPages (createOverviewPages chunks)

    /// Create an overview file based on the list of all pages, e.g. an RSS feed
    let withOverviewFile createOverview site =
        let content, url = createOverview site
        site |> withFile content url

    // Dry run generate, returning a map of file paths and contents, instead of writing them out to disk
    let generateDry outputPath render site =
        site.Pages
        |> List.map (fun p ->
            pageUrlToFilePath p.Url,
            p |> render site)
        |> List.append (site.Files |> List.map (fun p -> pageUrlToFilePath p.Url, p.Content))
        |> List.map (fun (url, content) ->
            let url = normalizeUrl url
            let path = Path.combine outputPath url |> Path.normalizeFileName
            path, content)
        |> Map.ofList

    /// Write the site files to the `outputPath`, using the render function to convert the pages into HTML
    let generate outputPath render site =
        Directory.delete outputPath
        site
        |> generateDry outputPath render
        |> Map.iter (fun path content -> 
            Directory.ensure (Path.getDirectory path)
            File.writeString false path content)
