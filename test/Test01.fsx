﻿// Start gremlin server with
// > .\gremlin-server.bat conf/gremlin-server-modern.yaml


#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"


#load "..\src\SLGremlin\Core\GremlinMonad.fs"
open SLGremlin.Core

let demo01 () = 
    runWithGremlinServer "localhost" 8182
        <| withTraversal (fun g -> g.V().Count().Next() |> mreturn)


let loadData () = 
    runWithGremlinServer "localhost" 8182
        <| gremlinDb { 
                let! _ = withTraversal (fun g -> g.Io(@"e:/coding/fsharp/gremlin-doodle/data/air-routes-small.xml").Read().Iterate() |> mreturn)
                return! withTraversal (fun g -> g.V().Count().Next() |> mreturn)
            }

