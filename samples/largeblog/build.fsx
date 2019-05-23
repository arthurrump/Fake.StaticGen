#r "paket:
source https://api.nuget.org/v3/index.json
nuget FSharp.Core 4.5.2 // Locked to be in sync with FAKE runtime
nuget Fake.IO.FileSystem 
nuget System.Net.Http
nuget Fake.Core.Target 
nuget Fake.StaticGen //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard" // Intellisense fix, see FAKE #1938
  #r "netstandard"
#endif

open System
open System.Net.Http
open Fake.Core
open Fake.IO

Target.create "CreatePosts" <| fun _ ->
    Directory.ensure "content"
    let rnd = Random()
    use http = new HttpClient()
    Seq.init 1000 id
    |> Seq.map (fun _ -> 
        async {
            let date = DateTime(2008, 02, 04).AddDays(float(rnd.Next(4000)))
            let! content = http.GetStringAsync("https://jaspervdj.be/lorem-markdownum/markdown.txt") |> Async.AwaitTask
            let title = content.Substring(2, content.IndexOf("\n") - 2)
            let path = "content/" + date.ToString("yyyyMMdd") + "-" + Fake.StaticGen.Url.slugify title + ".md"
            File.create path
            let write = File.writeString true path
            write "+++\ntitle = \""; write title; write "\"\ndate = "; write (date.ToString("yyyy-MM-dd")); write "\n+++\n\n"
            write content
        })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore

Target.runOrDefault "CreatePosts"