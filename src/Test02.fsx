

// Start gremlin server with
// > .\gremlin-server.bat

open System.Collections.Generic

#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"
open Gremlin.Net.Driver
open Gremlin.Net.Driver.Remote
open Gremlin.Net.Process.Traversal


let dictToMap (source : IDictionary<'key,'value>) : Map<'key,'value> = 
    source |> Seq.map (|KeyValue|) |> Map.ofSeq

let loadData () : unit = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.Io(@"e:/coding/fsharp/gremlin-doodle/data/air-routes-small.xml").Read() 
    remoteConnection.Dispose() |> ignore



let demo01 () = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    g.ToString() |> printfn "%s"
    remoteConnection.Dispose() |> ignore
    ()


let demo02 () : Map<string, int64> = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.V().HasLabel("airport").GroupCount().By("country").ToList() |> Seq.toList |> List.map dictToMap
    remoteConnection.Dispose() |> ignore
    match List.tryHead a with
    | Some a  -> a
    | None -> Map.empty



