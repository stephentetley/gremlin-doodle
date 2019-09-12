#r "netstandard"

// Start gremlin server with
// > .\gremlin-server.bat

open System.IO
open System.Collections.Generic

#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"
open Gremlin.Net.Driver
open Gremlin.Net.Driver.Remote
open Gremlin.Net.Process.Traversal

// Note - it looks like Gremlin server accepts both Windows and Unix style paths
let inputPath (fileName : string) : string = 
    Path.Combine(__SOURCE_DIRECTORY__, "../../data", fileName) |> Path.GetFullPath

let dictToMap (source : IDictionary<'key,'value>) : Map<'key,'value> = 
    source |> Seq.map (|KeyValue|) |> Map.ofSeq

let loadData () : int64 = 
    let path = inputPath "air-routes-small.xml"
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.Io(path).Read().Iterate()
    let ans = g.V().Count().Next()
    remoteConnection.Dispose() |> ignore
    ans


let demo01 () = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let ans = g.V().Count().Next()
    remoteConnection.Dispose() |> ignore
    ans


let demo02 () : Map<string, int64> = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.V().HasLabel("airport").GroupCount().By("country").ToList() |> Seq.toList |> List.map dictToMap
    remoteConnection.Dispose() |> ignore
    match List.tryHead a with
    | Some a  -> a
    | None -> Map.empty


let storeData (fileName : string)  = 
    let path = inputPath fileName
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.Io(path).Write().Iterate()
    remoteConnection.Dispose() |> ignore
    ()



