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

// uid * name *  parent
let terminals = 
    [ (1, "Terminal 1", "MAN")
    ; (2, "Terminal 2", "MAN")
    ; (3, "Terminal 3", "MAN")
    ; (4, "No name", "LBA")
    ]

let loadData () : GremlinDb<int64> = 
    // @"e:/coding/fsharp/gremlin-doodle/data/air-routes-small.xml"
    let path = inputPath "air-routes-small.xml"
    let addAirport name code = 
        withTraversal <| fun g -> g.AddV("airport").Property("name", name).Property("code", code).Next() |> mreturn 
    
    let addTerminal (ix :int) (name :string) (parentCode : string) : GremlinDb<Vertex> = 
        withTraversal <| fun g -> g.AddV("terminal").Property("name", name).Next() |> mreturn

    let addLink (child : Vertex) (parentCode : string) = 
        withTraversal <| fun g -> g.V().Has("code", parentCode).AddE("contains").To(child).Next() |> mreturn

    gremlinDb { 
        let! _ = forMz airports (fun (a,b) -> addAirport a b)
        let! _ = forMz terminals (fun (a,b, c) -> addTerminal a b c >>= fun t -> addLink t c)
        return! withTraversal (fun g -> g.V().Count().Next() |> mreturn)
    }

let getAirports () : GremlinDb<Vertex list> = 
    withTraversal <| fun g -> (g.V().HasLabel("airport").ToList() |> Seq.toList |> mreturn)


let getAirports2 () : GremlinDb<string list> = 
    withTraversal <| fun g -> (g.V().HasLabel("airport").Values().ToList() |> Seq.toList |> mreturn)

let deleteAll () : GremlinDb<Vertex> = 
    withTraversal <| fun g -> (g.V().Drop().Next() |> mreturn)

let dumpToFile (path : string) : GremlinDb<unit> = 
    withTraversal <| fun g -> g.Io(path).Write().Iterate() |> mreturn |>> (fun _ -> ())

// > kids1 "MAN" |> runSimple ;;
let kids1 (code : string) = 
    withTraversal <| fun g -> g.V().Has("code", code).Out().Path().By("name").ToList() |> mreturn