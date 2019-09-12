# Gremlin

## Startup

TinkerPop Gremlin server 

> cd C:\programs\apache-tinkerpop-gremlin-server-3.4.3\bin
> $env:Path = "C:\Program Files\Java\jdk1.8.0_73\bin;"
> .\gremlin-server.bat conf/gremlin-server-modern.yaml


TinkerPop Gremlin console

> cd C:\programs\apache-tinkerpop-gremlin-console-3.4.3\bin
> $env:Path = "C:\Program Files\Java\jdk1.8.0_73\bin;"
> .\gremlin.bat

OrientDB Gremlin console

> cd C:\programs\orientdb-tp3-3.0.23\bin
> $env:Path = "C:\Program Files\Java\jdk1.8.0_73\bin;"
> .\gremlin.bat


## OrientDB Gremlin

Start up a blank test database:

> gremlin> :plugin use tinkerpop.orientdb
> gremlin> graph = OrientGraph.open()

Instantiate a traversal, commonly ``g``:

> gremlin> g = graph.traversal()

All vertices / edges

> gremlin> g.V()
> gremlin> g.E()

With OrientDB you can add vertices directly with to the graph, although
this is considered bad practice (Lawrence, p164 - pdf 174):

> gremlin> v1 = graph.addVertex();

> gremlin> v1 = graph.addVertex();

> gremlin> e = v1.addEdge('friend', v2)

## Loading graphs

> gremlin> graph = TinkerGraph.open()
> gremlin> g = graph.traversal()
> gremlin> g.V().count()
> ==> 0
> gremlin> graph.io(graphml()).readGraph('e:/coding/fsharp/gremlin-doodle/data/air-routes-small.xml')
> gremlin> g.V().count()
> ==> 47


