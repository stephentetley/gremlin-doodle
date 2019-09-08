

// Start gremlin server with
// > .\gremlin-server.bat

open System.Collections.Generic

#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"
open Gremlin.Net.Driver
open Gremlin.Net.Driver.Remote
open Gremlin.Net.Process.Traversal


let loadData () : unit = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.AddV("node").Property("id", "A").Next()
    let b = g.AddV("node").Property("id", "B").Next()
    g.V(a).AddE("hasParent").To(b).Iterate() |> ignore
    remoteConnection.Dispose() |> ignore



let demo01 ()  = 
    let remoteConnection = new DriverRemoteConnection(new GremlinClient(new GremlinServer("localhost", 8182)))
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.V().Has("node", "id", "A").Next()
    remoteConnection.Dispose() |> ignore
    a



