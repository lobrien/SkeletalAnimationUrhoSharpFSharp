namespace SkeletalAnimation

open System
open Urho
open Urho.iOS
open Urho.Gui
open Urho.Resources
open Assert

type Sample (options : ApplicationOptions) as this = 
    inherit Application(options)

    let MonoDebugHud = new MonoDebugHud(this)

    let rand = new Random()

    let InitTouchInput () =
        let layout = this.ResourceCache.GetXmlFile("UI/ScreenJoystick_Samples.xml")
        if not (String.IsNullOrEmpty(this.JoystickLayoutPatch)) then
            let patchXmlFile = new XmlFile()
            patchXmlFile.FromString(this.JoystickLayoutPatch) |> ignore
            layout.Patch(patchXmlFile)
        let screenJoystickIndex = this.Input.AddScreenJoystick(layout, this.ResourceCache.GetXmlFile("UI/DefaultStyle.xml"))
        this.Input.SetScreenJoystickVisible(screenJoystickIndex, true)

    let MoveCameraByTouches timeStep (maybeCamera : Camera option) = 
        let MoveCameraByTouch (touch : TouchState) = 
            if touch.Delta.X <> 0 || touch.Delta.Y <> 0 then
                match maybeCamera with 
                | Some camera -> 
                    //TODO: Confirm -- shouldn't Graphics.Width be used here (yaw == side-to-side, no?)
                    this.Yaw <- this.Yaw + this.TouchSensitivity * camera.Fov / float32 this.Graphics.Height * float32 touch.Delta.X
                    this.Pitch <- this.Pitch + this.TouchSensitivity * camera.Fov / float32 this.Graphics.Height * float32 touch.Delta.Y
                    this.CameraNode.Value.Rotation <- new Quaternion(this.Pitch, this.Yaw, 0.f)
                | None ->
                    let cursor = this.UI.Cursor
                    if cursor <> null && cursor.IsVisible() then
                        cursor.Position <- touch.Position
                    
        if this.Input.NumTouches > 0u then
            for i in 0u .. (this.Input.NumTouches - 1u) do
                let state = this.Input.GetTouch(i)
                if state.TouchedElement <> null then
                    MoveCameraByTouch state                 
       
    member this.JoystickLayoutPatch = String.Empty

    member this.PixelSize = 0.01
    member this.TouchSensitivity : float32 = 2.f
    member val CameraNode : Node option = None with get, set
    member val Yaw = 0.f with get, set
    member val Pitch = 0.f with get, set

    member val console : UrhoConsole = null with get, set
    member val debugHud : DebugHud = null with get, set
    member val logoSprite : Sprite = null with get, set


    member this.NextRandom (min, max) =  rand.Next(min,max) |> float32

    override this.Start () = 
        base.Start ()

        let InitLogoSprite () = 
            let sprite = this.UI.Root.CreateSprite()
            sprite.Texture <- this.ResourceCache.GetTexture2D("Textures/LogoLarge.png")
            Assert.IsNotNull(sprite.Texture)
            let w = sprite.Texture.Width
            let h = sprite.Texture.Height
            sprite.SetScale(float32 (256 / w))
            sprite.SetSize(w, h)
            sprite.SetHotSpot(0, h)
            sprite.SetAlignment(HorizontalAlignment.Left, VerticalAlignment.Bottom)
            sprite.Opacity <- 0.75f
            sprite.Priority <- -100
            sprite

        this.console <- this.Engine.CreateConsole()
        this.debugHud <- this.Engine.CreateDebugHud()

        let xml = this.ResourceCache.GetXmlFile("UI/DefaultStyle.xml")
        this.console.DefaultStyle <- xml
        this.console.Background.Opacity <- 0.8f
        this.debugHud.DefaultStyle <- xml
    
        this.logoSprite <- InitLogoSprite() 
        InitTouchInput () 
        MonoDebugHud.Show()

        //Does this have any effect in iOS? 
        this.Graphics.SetWindowIcon(this.ResourceCache.GetImage("Textures/UrhoIcon.png"))
        this.Graphics.WindowTitle <- "UrhoSharp Sample"

    override this.OnUpdate (timeStep ) = 
        match this.CameraNode with
        | Some node -> 
            let c = node.GetComponent<Camera>()
            match c = null with
            | true -> MoveCameraByTouches timeStep None
            | false -> MoveCameraByTouches timeStep (Some c)
        | None -> ignore()

        base.OnUpdate(timeStep)  