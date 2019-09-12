// Copyright (c) Stephen Tetley 2019
// License: BSD 3 Clause

namespace SLGremlin.Core

[<AutoOpen>]
module GremlinMonad = 
    

    open Gremlin.Net.Driver
    open Gremlin.Net.Driver.Remote
    open Gremlin.Net.Process.Traversal

    type ErrMsg = string


    // GremlinDb Monad - a Reader-Error monad
    type GremlinDb<'a> = 
        GremlinDb of (GraphTraversalSource -> Result<'a, ErrMsg>)

    let inline private apply1 (ma : GremlinDb<'a>) 
                              (gts : GraphTraversalSource) : Result<'a, ErrMsg> = 
        let (GremlinDb f) = ma in f gts

    let mreturn (x : 'a) : GremlinDb<'a> = GremlinDb (fun _ -> Ok x)


    let inline private bindM (ma : GremlinDb<'a>) 
                             (f : 'a -> GremlinDb<'b>) : GremlinDb<'b> =
        GremlinDb <| fun gts -> 
            match apply1 ma gts with
            | Ok a  -> apply1 (f a) gts
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
    let runGremlinDb (conn : DriverRemoteConnection) 
                      (action : GremlinDb<'a>) : Result<'a, ErrMsg> = 
        try 
            let gts = AnonymousTraversalSource.Traversal().WithRemote(conn)
            match action with | GremlinDb(f) -> f gts
        with
        | err -> Error (sprintf "*** Exception: %s" err.Message)

    let runWithGremlinServer (hostname : string) (port : int) (action : GremlinDb<'a>) : Result<'a, ErrMsg> =  
        let srvr = new GremlinServer(hostname = hostname, port = port)
        let conn = new DriverRemoteConnection(new GremlinClient(srvr))
        runGremlinDb conn action


    let withTraversal (fn : GraphTraversalSource -> GremlinDb<'a>) : GremlinDb<'a> = 
        GremlinDb <| fun gts -> 
            apply1 (fn gts) gts

    // ************************************************************************
    // Usual monadic operations


    // Common operations
    let fmapM (update : 'a -> 'b) (action : GremlinDb<'a>) : GremlinDb<'b> = 
        GremlinDb <| fun conn ->
          match apply1 action conn with
          | Ok a -> Ok (update a)
          | Error msg -> Error msg
       
    /// Operator for fmap.
    let ( |>> ) (action : GremlinDb<'a>) (update : 'a -> 'b) : GremlinDb<'b> = 
        fmapM update action

    /// Flipped fmap.
    let ( <<| ) (update : 'a -> 'b) (action : GremlinDb<'a>) : GremlinDb<'b> = 
        fmapM update action


    /// Haskell Applicative's (<*>)
    let apM (mf : GremlinDb<'a ->'b>) 
            (ma : GremlinDb<'a>) : GremlinDb<'b> = 
        gremlinDb { 
            let! fn = mf
            let! a = ma
            return (fn a) 
        }

    /// Operator for apM
    let ( <**> ) (ma : GremlinDb<'a -> 'b>) 
                 (mb : GremlinDb<'a>) : GremlinDb<'b> = 
        apM ma mb


    /// Perform two actions in sequence. 
    /// Ignore the results of the second action if both succeed.
    let seqL (ma : GremlinDb<'a>) (mb : GremlinDb<'b>) : GremlinDb<'a> = 
        gremlinDb { 
            let! a = ma
            let! b = mb
            return a
        }

    /// Operator for seqL
    let (.>>) (ma : GremlinDb<'a>) 
              (mb : GremlinDb<'b>) : GremlinDb<'a> = 
        seqL ma mb

    /// Perform two actions in sequence. 
    /// Ignore the results of the first action if both succeed.
    let seqR (ma : GremlinDb<'a>) (mb : GremlinDb<'b>) : GremlinDb<'b> = 
        gremlinDb { 
            let! a = ma
            let! b = mb
            return b
        }

    /// Operator for seqR
    let (>>.) (ma : GremlinDb<'a>) 
              (mb : GremlinDb<'b>) : GremlinDb<'b> = 
        seqR ma mb

    /// Bind operator
    let ( >>= ) (ma : GremlinDb<'a>) 
                (fn : 'a -> GremlinDb<'b>) : GremlinDb<'b> = 
        bindM ma fn

    /// Flipped Bind operator
    let ( =<< ) (fn : 'a -> GremlinDb<'b>) 
                (ma : GremlinDb<'a>) : GremlinDb<'b> = 
        bindM ma fn

    let kleisliL (mf : 'a -> GremlinDb<'b>)
                 (mg : 'b -> GremlinDb<'c>)
                 (source:'a) : GremlinDb<'c> = 
        gremlinDb { 
            let! b = mf source
            let! c = mg b
            return c
        }

    /// Flipped kleisliL
    let kleisliR (mf : 'b -> GremlinDb<'c>)
                 (mg : 'a -> GremlinDb<'b>)
                 (source:'a) : GremlinDb<'c> = 
        gremlinDb { 
            let! b = mg source
            let! c = mf b
            return c
        }

    
    /// Operator for kleisliL
    let (>=>) (mf : 'a -> GremlinDb<'b>)
              (mg : 'b -> GremlinDb<'c>)
              (source:'a) : GremlinDb<'c> = 
        kleisliL mf mg source


    /// Operator for kleisliR
    let (<=<) (mf : 'b -> GremlinDb<'c>)
              (mg : 'a -> GremlinDb<'b>)
              (source:'a) : GremlinDb<'c> = 
        kleisliR mf mg source

    // ************************************************************************
    // Errors

    let throwError (msg : string) : GremlinDb<'a> = 
        GremlinDb <| fun _ -> Error msg

    let swapError (newMessage : string) (ma : GremlinDb<'a>) : GremlinDb<'a> = 
        GremlinDb <| fun conn -> 
            match apply1 ma conn with
            | Ok a -> Ok a
            | Error _ -> Error newMessage

    /// Operator for flip swapError
    let ( <??> ) (action : GremlinDb<'a>) (msg : string) : GremlinDb<'a> = 
        swapError msg action
    
    let augmentError (update : string -> string) (ma:GremlinDb<'a>) : GremlinDb<'a> = 
        GremlinDb <| fun conn ->
            match apply1 ma conn with
            | Ok a -> Ok a
            | Error msg -> Error (update msg)

    /// Try to run a computation.
    /// On failure, recover or throw again with the handler.
    let attempt (ma:GremlinDb<'a>) (handler : ErrMsg -> GremlinDb<'a>) : GremlinDb<'a> = 
        GremlinDb <| fun conn ->
            match apply1 ma conn with
            | Ok a -> Ok a
            | Error msg -> apply1 (handler msg) conn




    // ************************************************************************
    // Traversals

    /// Implemented in CPS 
    let mapM (mf: 'a -> GremlinDb<'b>) 
             (source : 'a list) : GremlinDb<'b list> = 
        GremlinDb <| fun conn -> 
            let rec work (xs : 'a list) 
                         (fk : ErrMsg -> Result<'b list, ErrMsg>) 
                         (sk : 'b list -> Result<'b list, ErrMsg>) = 
                match xs with
                | [] -> sk []
                | y :: ys -> 
                    match apply1 (mf y) conn with
                    | Error msg -> fk msg
                    | Ok a1 -> 
                        work ys fk (fun acc ->
                        sk (a1::acc))
            work source (fun msg -> Error msg) (fun ans -> Ok ans)

    let forM (xs : 'a list) (fn : 'a -> GremlinDb<'b>) : GremlinDb<'b list> = mapM fn xs


    /// Implemented in CPS 
    let mapMz (mf: 'a -> GremlinDb<'b>) 
              (source : 'a list) : GremlinDb<unit> = 
        GremlinDb <| fun conn -> 
            let rec work (xs : 'a list) 
                         (fk : ErrMsg -> Result<unit, ErrMsg>) 
                         (sk : unit -> Result<unit, ErrMsg>) = 
                match xs with
                | [] -> sk ()
                | y :: ys -> 
                    match apply1 (mf y) conn with
                    | Error msg -> fk msg
                    | Ok _ -> 
                        work ys fk (fun acc ->
                        sk acc)
            work source (fun msg -> Error msg) (fun ans -> Ok ans)


    let forMz (xs : 'a list) (fn : 'a -> GremlinDb<'b>) : GremlinDb<unit> = mapMz fn xs

    let foldM (action : 'state -> 'a -> GremlinDb<'state>) 
                (state : 'state)
                (source : 'a list) : GremlinDb<'state> = 
        GremlinDb <| fun conn -> 
            let rec work (st : 'state) 
                            (xs : 'a list) 
                            (fk : ErrMsg -> Result<'state, ErrMsg>) 
                            (sk : 'state -> Result<'state, ErrMsg>) = 
                match xs with
                | [] -> sk st
                | x1 :: rest -> 
                    match apply1 (action st x1) conn with
                    | Error msg -> fk msg
                    | Ok st1 -> 
                        work st1 rest fk (fun acc ->
                        sk acc)
            work state source (fun msg -> Error msg) (fun ans -> Ok ans)



    let smapM (action : 'a -> GremlinDb<'b>) (source : seq<'a>) : GremlinDb<seq<'b>> = 
        GremlinDb <| fun conn ->
            let sourceEnumerator = source.GetEnumerator()
            let rec work (fk : ErrMsg -> Result<seq<'b>, ErrMsg>) 
                            (sk : seq<'b> -> Result<seq<'b>, ErrMsg>) = 
                if not (sourceEnumerator.MoveNext()) then 
                    sk Seq.empty
                else
                    let a1 = sourceEnumerator.Current
                    match apply1 (action a1) conn with
                    | Error msg -> fk msg
                    | Ok b1 -> 
                        work fk (fun sx -> 
                        sk (seq { yield b1; yield! sx }))
            work (fun msg -> Error msg) (fun ans -> Ok ans)

    let sforM (sx : seq<'a>) (fn : 'a -> GremlinDb<'b>) : GremlinDb<seq<'b>> = 
        smapM fn sx
    
    let smapMz (action : 'a -> GremlinDb<'b>) 
                (source : seq<'a>) : GremlinDb<unit> = 
        GremlinDb <| fun conn ->
            let sourceEnumerator = source.GetEnumerator()
            let rec work (fk : ErrMsg -> Result<unit, ErrMsg>) 
                            (sk : unit -> Result<unit, ErrMsg>) = 
                if not (sourceEnumerator.MoveNext()) then 
                    sk ()
                else
                    let a1 = sourceEnumerator.Current
                    match apply1 (action a1) conn with
                    | Error msg -> fk msg
                    | Ok _ -> 
                        work fk sk
            work (fun msg -> Error msg) (fun ans -> Ok ans)

    
    let sforMz (source : seq<'a>) (action : 'a -> GremlinDb<'b>) : GremlinDb<unit> = 
        smapMz action source
        
    let sfoldM (action : 'state -> 'a -> GremlinDb<'state>) 
                    (state : 'state)
                    (source : seq<'a>) : GremlinDb<'state> = 
        GremlinDb <| fun conn ->
            let sourceEnumerator = source.GetEnumerator()
            let rec work (st : 'state) 
                            (fk : ErrMsg -> Result<'state, ErrMsg>) 
                            (sk : 'state -> Result<'state, ErrMsg>) = 
                if not (sourceEnumerator.MoveNext()) then 
                    sk st
                else
                    let x1 = sourceEnumerator.Current
                    match apply1 (action st x1) conn with
                    | Error msg -> fk msg
                    | Ok st1 -> 
                        work st1 fk sk
            work state (fun msg -> Error msg) (fun ans -> Ok ans)


    /// Implemented in CPS 
    let mapiM (mf : int -> 'a -> GremlinDb<'b>) 
                (source : 'a list) : GremlinDb<'b list> = 
        GremlinDb <| fun conn -> 
            let rec work (xs : 'a list)
                         (count : int)
                         (fk : ErrMsg -> Result<'b list, ErrMsg>) 
                         (sk : 'b list -> Result<'b list, ErrMsg>) = 
                match xs with
                | [] -> sk []
                | y :: ys -> 
                    match apply1 (mf count y) conn with
                    | Error msg -> fk msg
                    | Ok a1 -> 
                        work ys (count+1) fk (fun acc ->
                        sk (a1::acc))
            work source 0 (fun msg -> Error msg) (fun ans -> Ok ans)


    /// Implemented in CPS 
    let mapiMz (mf : int -> 'a -> GremlinDb<'b>) 
              (source : 'a list) : GremlinDb<unit> = 
        GremlinDb <| fun conn -> 
            let rec work (xs : 'a list) 
                         (count : int)
                         (fk : ErrMsg -> Result<unit, ErrMsg>) 
                         (sk : unit -> Result<unit, ErrMsg>) = 
                match xs with
                | [] -> sk ()
                | y :: ys -> 
                    match apply1 (mf count y) conn with
                    | Error msg -> fk msg
                    | Ok _ -> 
                        work ys (count+1) fk sk
            work source 0 (fun msg -> Error msg) (fun ans -> Ok ans)

    

    let foriM (xs : 'a list) (fn : int -> 'a -> GremlinDb<'b>) : GremlinDb<'b list> = 
        mapiM fn xs

    let foriMz (xs : 'a list) (fn : int -> 'a -> GremlinDb<'b>) : GremlinDb<unit> = 
        mapiMz fn xs
