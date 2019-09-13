// Start gremlin server with no config (empty database)
// > .\gremlin-server.bat

open System.IO

#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"


#load "..\src\SLGremlin\Core\GremlinMonad.fs"
open SLGremlin.Core


let inputPath (fileName : string) : string = 
    Path.Combine(__SOURCE_DIRECTORY__, "../data", fileName) |> Path.GetFullPath

let demo01 () = 
    runWithGremlinServer "localhost" 8182
        << withTraversal <| fun g -> g.V().Count().Next()


let loadData () = 
    // @"e:/coding/fsharp/gremlin-doodle/data/air-routes-small.xml"
    let path = inputPath "air-routes-small.xml"
    runWithGremlinServer "localhost" 8182
        <| gremlinDb { 
                let! _ = withTraversal <| fun g -> g.Io(path).Read().Iterate() 
                return! withTraversal <| fun g -> g.V().Count().Next()
            }

