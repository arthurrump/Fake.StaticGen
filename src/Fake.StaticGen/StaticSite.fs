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

type StaticSite<'config, 'post, 'page> =
    { Config : 'config
      Layout : 'config -> 'post list -> Page<'page> -> XmlNode
      Posts : 'post list
      Pages : Page<'page> list
      Files : File list }

type Parser<'t> = string -> 't
type FileTemplate<'config, 't> = 'config -> 't -> string
type HtmlTemplate<'config, 'details, 't> = 'config -> 'details -> 't -> XmlNode

module StaticSite =
    let fromConfig config =
        { Config = config 
          Layout = (fun _ _ p -> p.Content)
          Posts = []
          Pages = []
          Files = [] }
    
    let fromConfigFile (filePath : string) (parse : Parser<'config>) =
        filePath |> File.readAsString |> parse |> fromConfig

    let withPage (url : string) (details : 'page) (content : XmlNode) site =
        let page = { Url = url; Details = details; Content = content }
        { site with Pages = page::site.Pages }

    let withPageFromFile (url : string) (filePath : string) (parse : Parser<'details * 't>) (render : HtmlTemplate<'config, 'details, 't>) site =
        let details, content = 
            File.readAsString filePath |> parse
        site |> withPage url details (render site.Config details content)

    let withPosts (posts : 'post list) site =
        { site with Posts = List.append posts site.Posts }

    let withPostsFromFiles (files : #seq<string>) (parse : Parser<'post>) site =
        let posts = 
            files
            |> Seq.map (File.readAsString >> parse)
            |> Seq.toList
        site |> withPosts posts

    let withFile (url : string) (content : string) site =
        { site with Files = { Url = url; Content = content }::site.Files }

    let withFileFromPath (url : string) (filePath : string) site =
        site |> withFile url (File.readAsString filePath)

    let withFiles (files : File list) site =
        { site with Files = List.append files site.Files }

    let withOverviewPage (url : string) (pageDetails : 'details) (render : HtmlTemplate<'config, 'details, 'post list>) site =
        let content = render site.Config pageDetails site.Posts
        site |> withPage url pageDetails content

    // For example for RSS feeds
    let withOverviewFile (url : string) (createFile : FileTemplate<'config, 'post list>) site =
        let content = createFile site.Config site.Posts
        site |> withFile url content

    let renderPosts (getUrl : 'post -> string) (getDetails : 'post -> 'details) (render : HtmlTemplate<'config, 'details, 'post>) site =
        let pages = 
            site.Posts 
            |> List.map (fun p -> 
                let url = getUrl p
                let details = getDetails p
                let content = render site.Config details p
                { Url = url; Details = details; Content = content })
        { site with Pages = List.append pages site.Pages }

    let withLayout layout site =
        { site with Layout = layout }

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
              Content = site.Layout site.Config site.Posts p |> renderHtmlDocument })
        |> List.append site.Files
        |> List.iter (fun p -> 
            let url = normalizeUrl p.Url
            let path = Path.combine outputPath url |> Path.normalizeFileName
            Directory.ensure (Path.getDirectory path)
            File.writeString false path p.Content)
