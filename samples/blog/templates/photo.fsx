module Photo

#load "../.fake/build.fsx/intellisense.fsx"
#load "./models.fsx"

open Models
open Fake.StaticGen
open Giraffe.GiraffeViewEngine

let template post =
    hr []

let overview overview =
    hr []
