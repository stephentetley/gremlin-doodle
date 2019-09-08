

// Start gremlin server with
// > .\gremlin-server.bat conf/gremlin-server-modern.yaml


#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"
open Gremlin.Net.Driver
open Gremlin.Net.Driver.Remote
open Gremlin.Net.Process.Traversal
open Gremlin.Net.Structure

let demo01 () = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let ans : Vertex list = g.V().Has("person", "name", "marko").Out("knows").ToList() |> List.ofSeq
    remoteConnection.Dispose() |> ignore
    ans


