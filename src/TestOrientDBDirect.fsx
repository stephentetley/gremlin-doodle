#r "netstandard"


#I @"C:\Users\stephen\.nuget\packages\system.security.cryptography.x509certificates\4.3.2\runtimes\win\lib\netstandard1.6"
#r @"System.Security.Cryptography.X509Certificates.dll"



// OrientDB.NETStandard-1.5

#I @"C:\Users\stephen\.nuget\packages\orientdb.netstandard-1.5\0.1.13\lib\netstandard1.5"
#r @"OrientDB-Net.binary.Innov8tive.dll"

open Orient.Client
open OrientDB_Net.binary.Innov8tive.API


// DOES NOT WORK
// THE QUERIES TIME OUT WITH NO ANSWER

let connOpts (password : string) : ConnectionOptions = 
    let opts  = new ConnectionOptions()
    opts.HostName <- "localhost"
    opts.Port <- 8182
    opts.DatabaseName <- "demodb"    
    opts.UserName <- "root"
    opts.Password <- password
    opts.DatabaseType <- ODatabaseType.Graph
    opts

let orientDB (password : string) : Orient.Client.ODatabase = 
    let opts = connOpts (password)
    new ODatabase( options = opts ) 


let demo01 (password : string) = 
    try
        let odb = orientDB (password)
        odb.CountRecords |> printfn "Count Records %i"    
        odb.Close()
        ()
    with
    | excn -> printfn "%s" excn.Message

let makeOServer (password : string) = 
    new OServer(hostname = "localhost", port = 8182, userName = "root", userPassword = password)

let demo02 (password : string) = 
    let oserver = makeOServer (password)
    let ans = oserver.DatabaseExist("demodb", OStorageType.Memory) 
    oserver.Close() 
    ans

