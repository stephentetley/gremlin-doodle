// Start gremlin server with no config (empty database)
// > .\gremlin-server.bat

open System.IO
open System.Collections.Generic


#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"
open Gremlin.Net.Structure

#load "..\src\SLGremlin\Core\GremlinMonad.fs"
open SLGremlin.Core


let inputPath (fileName : string) : string = 
    Path.Combine(__SOURCE_DIRECTORY__, "../data", fileName) |> Path.GetFullPath

let runSimple action = 
    runWithGremlinServer "localhost" 8182 action

let test01 () = 
    withTraversal (fun g -> g.V().Count().Next() |> mreturn)

let airports = 
    [ ("manchester airport", "MAN")
    ; ("leeds bradford airport", "LBA")
    ]

let loadData () : GremlinDb<int64> = 
    // @"e:/coding/fsharp/gremlin-doodle/data/air-routes-small.xml"
    let path = inputPath "air-routes-small.xml"
    let addAirport name code = 
        withTraversal <| fun g -> g.AddV("airport").Property("name", name).Property("code", code).Next() |> mreturn 

    gremlinDb { 
        let! _ = forMz airports (fun (a,b) -> addAirport a b)
        return! withTraversal (fun g -> g.V().Count().Next() |> mreturn)
    }

let getAirports () : GremlinDb<Vertex list> = 
    withTraversal <| fun g -> (g.V().HasLabel("airport").ToList() |> Seq.toList |> mreturn)


let getAirports2 () : GremlinDb<string list> = 
    withTraversal <| fun g -> (g.V().HasLabel("airport").Values().ToList() |> Seq.toList |> mreturn)

let deleteAll () : GremlinDb<Vertex> = 
    withTraversal <| fun g -> (g.V().Drop().Next() |> mreturn)