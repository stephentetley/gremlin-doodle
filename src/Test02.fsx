

// Start gremlin server with
// > .\gremlin-server.bat

open System.Collections.Generic

#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"
open Gremlin.Net.Driver
open Gremlin.Net.Driver.Remote
open Gremlin.Net.Process.Traversal
open Gremlin.Net

let dictToMap (source : IDictionary<_,_>) : Map<_,_> = 
    source |> Seq.map (|KeyValue|) |> Map.ofSeq

let loadData () : GraphTraversal<string,string> = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.Io(@"e:/coding/fsharp/gremlin-doodle/data/air-routes-small.xml").Read() 
    remoteConnection.Dispose() |> ignore
    a


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



