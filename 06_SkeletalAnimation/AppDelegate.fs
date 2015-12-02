namespace SkeletalAnimation

open System

open UIKit
open Foundation
open Urho
open Urho.iOS

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit UIApplicationDelegate ()

    override val Window = null with get, set

    // This method is invoked when the application is ready to run.
    override this.FinishedLaunching (app, options) =
        UrhoEngine.Init()
        let o = Urho.ApplicationOptions.Default
     
        try
            let g = new SkeletalAnimation(o)
            let r = g.Run() 
            System.Console.WriteLine(r.ToString())
        with
        | x -> System.Console.WriteLine(x.ToString())

        true
