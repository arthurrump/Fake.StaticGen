namespace Fake.StaticGen

open Fake.Core
open Fake.IO

open System
open System.Text

module Path =
    /// Get the lowest directory in the path
    let getLowestDirectory = 
        Path.getDirectory >> String.splitStr Path.directorySeparator >> List.last

module Url =
    /// Normalize a url to be lowercase, using forward slashes and with no slashes at the beginning or end
    let normalizeUrl (url : string) =
        url.Replace("\\", "/").Trim().TrimEnd('/').TrimStart('/').ToLowerInvariant()

    /// Normalize a relative url starting with a forward slash, otherwise the same as normalizeUrl
    let normalizeRelativeUrl url =
        "/" + (normalizeUrl url)

    /// Turn text into a "slug" that can be used as a part of a url by replacing spaces by dashes 
    /// and ignoring other special characters
    let slugify (text : string) =
        text.ToLowerInvariant()
            .Replace(" ", "-")
            .ToCharArray()
            |> Array.filter (fun c -> (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c = '-')
            |> String.Concat

open Url

[<AutoOpen>]
module private UrlHelpers =
    let pageUrlToFilePath (url : string) =
        let url = normalizeUrl url
        if System.IO.Path.HasExtension url then url else url + "/index.html"

type Page<'content> =
    { Url : string
      Content : 'content }

type StaticSite<'config, 'content> =
    { BaseUrl : string
      Config : 'config
      Pages : Page<'content> seq
      Files : Page<byte []> seq }

    member this.AbsoluteUrl relativeUrl =
        this.BaseUrl + "/" + (normalizeUrl relativeUrl)

type ISiteConfig =
    abstract member BaseUrl : string

module StaticSite =
    /// Create a new site with a base url and some configuration
    let fromConfig baseUrl config =
        { BaseUrl = normalizeUrl baseUrl
          Config = config
          Pages = Seq.empty
          Files = Seq.empty }

    /// Create a new site with a base url and some configuration parsed from a source file
    let fromConfigFile baseUrl filePath parse =
        Trace.tracefn "Reading %s" (Path.toRelativeFromCurrent filePath)
        filePath |> File.readAsString |> parse |> fromConfig baseUrl

    /// Create a new site with some configuration that includes the BaseUrl
    let fromIConfig (config : #ISiteConfig) =
        fromConfig config.BaseUrl config
    
    /// Create a new site with some configuration that includes the BaseUrl 
    /// parsed from a source file
    let fromIConfigFile filePath parse =
        Trace.tracefn "Reading %s" (Path.toRelativeFromCurrent filePath)
        filePath |> File.readAsString |> parse |> fromIConfig

    /// Add multiple pages
    let withPages pages site =
        let pages = pages |> Seq.map (fun p -> { p with Url = normalizeRelativeUrl p.Url })
        { site with Pages = Seq.append pages site.Pages |> Seq.cache }

    /// Add a new page
    let withPage content url site =
        let page = { Url = normalizeRelativeUrl url; Content = content }
        site |> withPages [ page ]

    /// Parse multiple source files and add them as pages
    let withPagesFromSources sourceFiles parse site =
        let pages = 
            sourceFiles 
            |> Seq.map (fun path -> 
                Trace.tracefn "Reading %s" (Path.toRelativeFromCurrent path)
                path |> File.readAsString |> parse path)
        site |> withPages pages

    /// Parse a source file and add it as a page
    let withPageFromSource sourceFile parse site =
        site |> withPagesFromSources [ sourceFile ] parse

    /// Add multiple files
    let withFiles files site =
        let files = files |> Seq.map (fun f -> { f with Url = normalizeRelativeUrl f.Url })
        { site with Files = Seq.append files site.Files |> Seq.cache }

    /// Add a file
    let withFile content url site =
        let file = { Url = normalizeRelativeUrl url; Content = content }
        site |> withFiles [ file ]

    /// Add a file with text
    let withFileFromString (content : string) =
        withFile (Encoding.UTF8.GetBytes(content))

    /// Copy multiple source files
    let withFilesFromSources sourceFiles urlMapper site =
        let files = 
            sourceFiles 
            |> Seq.map (fun path -> 
                Trace.tracefn "Reading %s" (Path.toRelativeFromCurrent path)
                let content = path |> File.readAsBytes
                { Url = urlMapper path
                  Content = content })
        site |> withFiles files

    /// Copy a source file
    let withFileFromSource sourceFile url site =
        site |> withFilesFromSources [ sourceFile ] (fun _ -> url)

    /// Create an overview page based on the list of all pages
    let withOverviewPage createOverview site =
        let overview = Seq.delay (fun _ -> seq [ createOverview site ])
        site |> withPages overview

    /// Create multiple overview pages based on the list of all pages
    let withOverviewPages createOverviewPages site =
        let overview = Seq.delay (fun _ -> createOverviewPages site)
        site |> withPages overview

    /// Create a paginated overview with a specified number of items per page
    let withPaginatedOverview itemsPerPage chooser createOverviewPages site =
        let overview = 
            Seq.delay (fun _ -> 
                site.Pages 
                |> Seq.choose chooser
                |> Seq.chunkBySize itemsPerPage
                |> createOverviewPages)
        site |> withPages overview

    /// Create an overview file based on the list of all pages, e.g. an RSS feed
    let withOverviewFile (createOverview : StaticSite<'a, 'b> -> Page<string>) site =
        let file = Seq.delay (fun _ -> 
            let file = createOverview site
            let content = Encoding.UTF8.GetBytes(file.Content)
            seq [ { Url = file.Url; Content = content } ])
        site |> withFiles file

    // Dry run generate, returning a map of file paths and contents, instead of writing them out to disk
    let generateDry outputPath (render : StaticSite<'a, 'b> -> Page<'b> -> string) site =
        site.Pages
        |> Seq.map (fun p ->
            { Url = pageUrlToFilePath p.Url
              Content = p |> render site |> Encoding.UTF8.GetBytes })
        |> Seq.append site.Files
        |> Seq.map (fun f ->
            let url = normalizeUrl f.Url
            let path = Path.combine outputPath url |> Path.normalizeFileName
            path, f.Content)

    /// Write the site files to the `outputPath`, using the render function to convert the pages into HTML
    let generate outputPath render site =
        Directory.delete outputPath
        site
        |> generateDry outputPath render
        |> Seq.iter (fun (path, content) -> 
            Trace.tracefn "Writing %s" (Path.toRelativeFromCurrent path)
            Directory.ensure (Path.getDirectory path)
            File.writeBytes path content)
