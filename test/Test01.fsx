// Start gremlin server with
// > .\gremlin-server.bat conf/gremlin-server-modern.yaml


#I @"C:\Users\stephen\.nuget\packages\gremlin.net\3.4.3\lib\netstandard2.0"
#r @"Gremlin.Net.dll"


#load "..\src\SLGremlin\Core\GremlinMonad.fs"
open SLGremlin.Core


