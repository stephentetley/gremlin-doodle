// Copyright (c) Stephen Tetley 2019
// License: BSD 3 Clause

namespace SLGremlin.Core

[<AutoOpen>]
module GremlinMonad = 
    

    open Gremlin.Net.Driver.Remote


    type ErrMsg = string


    // GremlinDb Monad - a Reader-Error monad
    type GremlinDb<'a> = 
        GremlinDb of (DriverRemoteConnection -> Result<'a, ErrMsg>)

    let inline private apply1 (ma : GremlinDb<'a>) 
                              (conn : DriverRemoteConnection) : Result<'a, ErrMsg> = 
        let (GremlinDb f) = ma in f conn

    let mreturn (x : 'a) : GremlinDb<'a> = GremlinDb (fun _ -> Ok x)


    let inline private bindM (ma : GremlinDb<'a>) 
                             (f : 'a -> GremlinDb<'b>) : GremlinDb<'b> =
        GremlinDb <| fun conn -> 
            match apply1 ma conn with
            | Ok a  -> apply1 (f a) conn
            | Error msg -> Error msg

    let failM (msg : string) : GremlinDb<'a> = 
        GremlinDb (fun _ -> Error msg)
    
    let inline private altM (ma : GremlinDb<unit>) 
                            (mb : GremlinDb<'b>) : GremlinDb<'b> = 
        GremlinDb <| fun conn -> 
                match apply1 ma conn, apply1 mb conn with
                | Ok _, Ok b -> Ok b
                | Error msg, _ -> Error msg
                | _, Error msg -> Error msg
    
    
    let inline private delayM (fn : unit -> GremlinDb<'a>) : GremlinDb<'a> = 
        bindM (mreturn ()) fn 
    
    type GremlinDbBuilder() = 
        member self.Return x        = mreturn x
        member self.Bind (p,f)      = bindM p f
        member self.Zero ()         = failM "Zero"
        member self.Combine (p,q)   = altM p q
        member self.Delay fn        = delayM fn
        member self.ReturnFrom(ma)  = ma
    
    
    let (gremlinDb : GremlinDbBuilder) = new GremlinDbBuilder()

    // GremlinDb specific operations
    let runSqliteDb (conn : DriverRemoteConnection) 
                      (action : GremlinDb<'a>): Result<'a, ErrMsg> = 
        try 
            match action with | GremlinDb(f) -> f conn
        with
        | err -> Error (sprintf "*** Exception: %s" err.Message)