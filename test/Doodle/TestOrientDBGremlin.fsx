

// Start gremlin server with
// > .\gremlin-server.bat

open System.Collections.Generic

#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"
open Gremlin.Net.Driver
open Gremlin.Net.Driver.Remote
open Gremlin.Net.Process.Traversal
open Gremlin.Net.Structure.IO.GraphSON

#I @"C:\Users\stephen\.nuget\packages\Newtonsoft.Json\11.0.2\lib\netstandard2.0"
#r @"Newtonsoft.Json.dll"
open Newtonsoft.Json.Linq

// OrientDB server must be running on port 8182

// For a proper treatment of record ID see link below, although it looks like we can 
// confortably treat them as strings
// https://orientdb.com/docs/2.0/orientdb.wiki/Tutorial-Record-ID.html


let oRecordIdReader = 
    { new IGraphSONDeserializer 
        with member x.Objectify(token : JToken, reader : GraphSONReader) : obj = 
                        let identifier = token.ToObject<string>() :> obj
                        identifier }

let graphsonReader : GraphSON3Reader = 
    let name = GraphSONUtil.FormatTypeName(namespacePrefix = "orient", typeName = "ORecordId")
    let dict = (new Dictionary<string, IGraphSONDeserializer>())
    dict.Add(name, oRecordIdReader) |> ignore
    new GraphSON3Reader(dict)
           


let makeConnection (password : string) : DriverRemoteConnection = 
    let server = new GremlinServer(hostname = "localhost", port = 8182, username="root", password=password)
    let client = new GremlinClient(gremlinServer = server, graphSONReader = graphsonReader)
    new DriverRemoteConnection(client = client)


let loadData (password : string) : unit = 
    let remoteConnection = makeConnection (password)
    
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.AddV("node").Property("id", "A").Next()
    let b = g.AddV("node").Property("id", "B").Next()
    g.V(a).AddE("hasParent").To(b).Iterate() |> ignore
    remoteConnection.Dispose() |> ignore



let demo01 (password)  = 
    let remoteConnection = makeConnection (password)
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.V().ToList() |> List.ofSeq
    remoteConnection.Dispose() |> ignore
    a
    


let demo02 (password)  = 
    let remoteConnection = makeConnection (password)
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.V().Count().Next()
    remoteConnection.Dispose() |> ignore
    a
    
// Exists for me...
let demo03 (password)  = 
    let remoteConnection = makeConnection (password)
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.V("#48:12").Next()
    remoteConnection.Dispose() |> ignore
    a

// Does not exist
let demo03a (password)  = 
    let remoteConnection = makeConnection (password)
    let g = AnonymousTraversalSource.Traversal().WithRemote(remoteConnection)
    let a = g.V("#02:1000").Next()
    remoteConnection.Dispose() |> ignore
    a
    
    
