open Fake.StaticGen
open Fake.StaticGen.Sass

[<EntryPoint>]
let main argv =
    StaticSite.fromConfig ()
    |> StaticSite.withSass "$mycolor: #000;\r\n body { color: $mycolor; }" "/style.css"
    |> StaticSite.generateDry "output" (fun _ page -> page.Content)
    |> printfn "%A"
    0
