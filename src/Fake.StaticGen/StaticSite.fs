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

type Helpers<'config, 'components> = 
    { Config : 'config option
      Components : Map<string, 'components>
      BaseUrl : string }

    member this.AbsoluteUrl relativeUrl =
        this.BaseUrl + "/" + (normalizeUrl relativeUrl)

type Page<'content> =
    { Url : string
      Content : 'content }

type File = Page<byte []>

type SourceParser<'content> = string -> string -> 'content

type PageSource<'content> =
    { Url : string 
      Source : string 
      Parser : SourceParser<'content> }

type FileSource = 
    { Url : string
      Source : string }

type Template<'config, 'comp, 'content> = Helpers<'config, 'comp> -> Page<'content> -> string

[<RequireQualifiedAccess>]
type Config<'config> =
    | Object of 'config
    | File of path : string * parse : (string -> 'config)

[<NoComparison>]
type SiteBuilderState<'config, 'components, 'page> = 
    { Config : Config<'config> option
      Template : Template<'config, 'components, 'page>
      Pages : Page<'page> seq
      PageSources : PageSource<'page> seq
      Files : File seq
      FileSources : FileSource seq }

type SiteBuilder<'config, 'components, 'page> internal () =

    member __.Yield (_) =
        { Config = None
          Template = fun _ page -> sprintf "No template set. Page contents:\n%A" page.Content
          Pages = Seq.empty
          PageSources = Seq.empty
          Files = Seq.empty
          FileSources = Seq.empty }

    [<CustomOperation("config")>]
    member __.Config (state, config : 'config) : SiteBuilderState<'config, 'components, 'page> =
        { state with Config = Some (Config.Object config) }
    
    [<CustomOperation("configFile")>]
    member __.ConfigFile (state, sourceFile : string, parse : (string -> 'config)) : SiteBuilderState<'config, 'components, 'page> =
        { state with Config = Some (Config.File (sourceFile, parse)) }

    [<CustomOperation("template")>]
    member __.Template (state, template) : SiteBuilderState<'config, 'components, 'page> =
        { state with Template = template }

    [<CustomOperation("page")>]
    member __.Page (state, url : string, content : 'page) : SiteBuilderState<'config, 'components, 'page> =
        { state with Pages = state.Pages |> Seq.append [ { Url = url; Content = content } ] }

    [<CustomOperation("pageSource")>]
    member __.PageSource (state, pageSource) : SiteBuilderState<'config, 'components, 'page> =
        { state with PageSources = state.PageSources |> Seq.append [ pageSource ] }

    [<CustomOperation("pages")>]
    member __.Pages (state, pages) : SiteBuilderState<'config, 'components, 'page> =
        { state with Pages = state.Pages |> Seq.append pages }

    [<CustomOperation("pageSources")>]
    member __.PageSources (state, pageSources) : SiteBuilderState<'config, 'components, 'page> =
        { state with PageSources = state.PageSources |> Seq.append pageSources }

    [<CustomOperation("file")>]
    member __.File (state, url, content) : SiteBuilderState<'config, 'components, 'file> =
        { state with Files = state.Files |> Seq.append [ { Url = url; Content = content } ] }

    [<CustomOperation("fileStr")>]
    member __.File (state, url, content : string) : SiteBuilderState<'config, 'components, 'file> =
        let bytes = Encoding.UTF8.GetBytes(content)
        { state with Files = state.Files |> Seq.append [ { Url = url; Content = bytes } ] }

    [<CustomOperation("fileSource")>]
    member __.FileSource (state, fileSource : FileSource) : SiteBuilderState<'config, 'components, 'file> =
        { state with FileSources = state.FileSources |> Seq.append [ fileSource ] }

    [<CustomOperation("files")>]
    member __.Files (state, files : #seq<File>) : SiteBuilderState<'config, 'components, 'file> =
        { state with Files = state.Files |> Seq.append files }

    [<CustomOperation("fileSources")>]
    member __.FileSources (state, fileSources : #seq<FileSource>) : SiteBuilderState<'config, 'components, 'file> =
        { state with FileSources = state.FileSources |> Seq.append fileSources }

[<AutoOpen>]
module SiteBuilder =
    let staticsite<'config, 'components, 'page> = SiteBuilder<'config, 'components, 'page>()

module StaticSite =
    let private prepareWrite url =
        let path = pageUrlToFilePath url
        Trace.tracefn "Writing %s" (Path.toRelativeFromCurrent path)
        Directory.ensure (Path.getDirectory path)
        path

    let private writePage url content =
        let path = prepareWrite url
        File.writeString false path content

    let private writeFile url content =
        let path = prepareWrite url
        File.writeBytes path content

    /// Write the site files to the `outputPath`, using the render function to convert the pages into HTML
    let generate baseUrl outputPath site =
        let config =
            site.Config 
            |> Option.map (fun conf -> 
                match conf with
                | Config.Object c -> c
                | Config.File (path, parse) -> File.readAsString path |> parse)

        let helpers = 
            { Config = config
              Components = Map.empty
              BaseUrl = baseUrl }
        
        Directory.delete outputPath

        site.Pages
        |> Seq.iter (fun page -> site.Template helpers page |> writePage page.Url)

        site.PageSources
        |> Seq.iter (fun pagesrc ->
            let content = File.readAsString pagesrc.Source |> pagesrc.Parser pagesrc.Source
            let page = { Url = pagesrc.Url; Content = content }
            site.Template helpers page |> writePage page.Url)

        site.Files
        |> Seq.iter (fun file -> writeFile file.Url file.Content)

        site.FileSources
        |> Seq.iter (fun filesrc -> File.readAsBytes filesrc.Source |> writeFile filesrc.Url)
