namespace SkeletalAnimation

open System
open Urho
open Urho.iOS
open Urho.Gui


type Mover (MoveSpeed : float32, RotationSpeed : float32, Bounds : BoundingBox) as this = 
    inherit Component () 

    do
        this.ReceiveSceneUpdates <- true

    override this.OnUpdate (timeStep : float32) = 
        // This moves the character position
        this.Node.Translate(Vector3.UnitZ * MoveSpeed * timeStep, TransformSpace.Local)

        // If in risk of going outside the plane, rotate the model right
        let pos = this.Node.Position
        if (pos.X < Bounds.Min.X || pos.X > Bounds.Max.X || pos.Z < Bounds.Min.Z || pos.Z > Bounds.Max.Z) then
            this.Node.Yaw(RotationSpeed * timeStep, TransformSpace.Local)

        // Get the model's first (only) animation
        // state and advance its time. Note the
        // convenience accessor to other components in
        // the same scene node

        let model = this.GetComponent<AnimatedModel>()
        if (model.NumAnimationStates > 0u) then
            let state = model.AnimationStates |> Seq.head
            state.AddTime(timeStep)

type SkeletalAnimation(options) as this = 
    inherit Sample(options)

    let mutable scene : Scene = null
    let mutable camera : Camera = null
    let mutable drawDebug  = false

    let CreateScene () = 
        scene <- new Scene()

        // Create the Octree component to the scene so that drawable objects can be rendered. Use default volume
        // (-1000, -1000, -1000) to (1000, 1000, 1000)
        scene.CreateComponent<Octree> () |> ignore //TODO: Called for side-effect?
        scene.CreateComponent<DebugRenderer>() |> ignore //TODO: Called for side-effect?

        // Create scene node & StaticModel component for showing a static plane
        let planeNode = scene.CreateChild("Plane")
        planeNode.Scale <- new Vector3 (100.f, 1.f, 100.f)
        let planeObject = planeNode.CreateComponent<StaticModel> ()
        planeObject.Model <- this.ResourceCache.GetModel ("Models/Plane.mdl")
        planeObject.SetMaterial (this.ResourceCache.GetMaterial ("Materials/StoneTiled.xml"))

        // Create a Zone component for ambient lighting & fog control
        let zoneNode = scene.CreateChild("Zone")
        let zone = zoneNode.CreateComponent<Zone>()
        
        // Set same volume as the Octree, set a close bluish fog and some ambient light
        zone.SetBoundingBox (new BoundingBox(-1000.0f, 1000.0f))
        zone.AmbientColor <- new Color (0.15f, 0.15f, 0.15f)
        zone.FogColor <- new Color (0.5f, 0.5f, 0.7f)
        zone.FogStart <- 100.f
        zone.FogEnd <- 300.f

        // Create a directional light to the world. Enable cascaded shadows on it
        let lightNode = scene.CreateChild("DirectionalLight")
        lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f))
        let light = lightNode.CreateComponent<Light>()
        light.LightType <- LightType.Directional
        light.CastShadows <- true
        light.ShadowBias <- new BiasParameters(0.00025f, 0.5f)
    
        // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
        light.ShadowCascade <- new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f)

        // Create animated models
        let numModels = 100
        let modelMoveSpeed = 2.0f
        let modelRotateSpeed = 100.0f
        let bounds = new BoundingBox (new Vector3(-47.0f, 0.0f, -47.0f), new Vector3(47.0f, 0.0f, 47.0f))

        for _ in 0 .. numModels do
            let modelNode = scene.CreateChild("Jack")

            modelNode.Position <- new Vector3(this.NextRandom(-45, 45), 0.0f, this.NextRandom(-45, 45))
            modelNode.Rotation <- new Quaternion (0.f, this.NextRandom(0, 360), 0.f)
            let modelObject = new AnimatedModel ()
            modelNode.AddComponent (modelObject)
            modelObject.Model <- this.ResourceCache.GetModel("Models/Jack.mdl")
            modelObject.SetMaterial(this.ResourceCache.GetMaterial("Materials/Jack.xml"))
            modelObject.CastShadows <- true

            // Create an AnimationState for a walk animation. Its time position will need to be manually updated to advance the
            // animation, The alternative would be to use an AnimationController component which updates the animation automatically,
            // but we need to update the model's position manually in any case
            let walkAnimation = this.ResourceCache.GetAnimation("Models/Jack_Walk.ani")
            let state = modelObject.AddAnimationState(walkAnimation)
            // The state would fail to create (return null) if the animation was not found
            if (state <> null) then
                // Enable full blending weight and looping
                state.Weight <- 1.f
                state.SetLooped(true)

        
            // Create our custom Mover component that will move & animate the model during each frame's update
            let mover = new Mover (modelMoveSpeed, modelRotateSpeed, bounds)
            modelNode.AddComponent (mover)
        
        // Create the camera. Limit far clip distance to match the fog
        this.CameraNode <- scene.CreateChild("Camera") |> Some
        camera <- this.CameraNode.Value.CreateComponent<Camera>()
        camera.FarClip <- 300.f
    
        // Set an initial position for the camera scene node above the plane
        this.CameraNode.Value.Position <- new Vector3(0.0f, 5.0f, 0.0f)
        (scene, camera)

    let SetupViewport (ctxt : Context) (scn : Scene) (cam : Camera) = 
        this.Renderer.SetViewport(uint32 0, new Viewport(ctxt, scn, cam, null))

    let SubscribeToEvents () = 
        this.Engine.SubscribeToPostRenderUpdate (fun _ -> 
            if drawDebug then
                this.Renderer.DrawDebugGeometry(false) ) 

    override this.Start () = 
        base.Start()
        let (scn, cam) = CreateScene()
        SetupViewport this.Context scn cam
        SubscribeToEvents() |> ignore //TODO: Subscription returned -- ignore ok?
