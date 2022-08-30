using Godot;
using System.Collections.Generic;

public class scr_Player : KinematicBody
{
    //Movement
    [Export] private float speed = 7f;
    [Export] private float maxSpeed = 80f;
    [Export] private float gravity = 9.8f;
    [Export] private float jumpForce = 5f;
    [Export] private float normalAccel = 7f;
    [Export] private float airAccel = 1f;
    [Export] private float dashForce = 5f;
    private Dictionary<string, float> accel_type = new Dictionary<string, float>();
    private Spatial head;
    private Vector3 snap;
    private Vector3 velocity = new Vector3();
    private Vector3 gravityVec = new Vector3();
    private Vector3 additionalVel = new Vector3();
    private Vector3 movement = new Vector3();
    private RayCast groundCheck;
    private bool airJump;
    private float accel;
    private float threshold = 0.1f;
    private bool grounded;
    private MovementState state;


    //WallRunning
    [Export] private float wallRunGrav;
    private RayCast leftWallCheck;
    private RayCast rightWallCheck;
    private RayCast minJumpHeightCheck;
    private bool wallLeft;
    private bool wallRight;


    //Sliding
    private MeshInstance mesh;
    private CollisionShape collider;


    //Camera
    [Export] private float mouseSense = 0.1f;
    [Export] private float basefov;
    [Export] private float additionalfov;
    [Export] private float fovChangeRate;
    [Export] private float camTilt;
    [Export] private float highSpeedfov;
    private float fov;
    private Camera camera;
    private Camera weaponCam;
    private float camAccel = 40f;
    private float tilt;

    //Galeforce
    [Export] private float gforceLevel1;
    [Export] private float gforceLevel2;
    [Export] private float gforceMax;
    [Export] private float gforceGain;
    private float gforce;


    [Signal]
    public delegate void UpdateBars(float speed, float gforce);


    public override void _Ready()
    {
        fov = basefov;
        Node raycasts = GetNode("Raycasts");

        accel_type.Add("default", normalAccel);
        accel_type.Add("air", airAccel);
        accel = accel_type["default"];

        head = GetNode<Spatial>("Head");
        camera = GetNode<Spatial>("Head").GetChild<Camera>(0);
        weaponCam = camera.GetNode<ViewportContainer>("ViewportContainer").GetNode<Viewport>("Viewport").GetNode<Camera>("WeaponCamera");

        leftWallCheck = raycasts.GetNode<RayCast>("WallLeft");
        rightWallCheck = raycasts.GetNode<RayCast>("WallRight");
        minJumpHeightCheck = raycasts.GetNode<RayCast>("MinJump");
        groundCheck = raycasts.GetNode<RayCast>("GroundCheck");

        mesh = GetNode<MeshInstance>("PlayerMesh");
        collider = GetNode<CollisionShape>("PlayerCollider");

        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion) {

            //Rotate player horizontally
            RotateY(Mathf.Deg2Rad(-mouseMotion.Relative.x * mouseSense));

            //Simulate a vertical head rotation
            Basis headBasis = head.Transform.basis;
            headBasis = headBasis.Rotated(Vector3.Right, Mathf.Deg2Rad(-mouseMotion.Relative.y * mouseSense));
            Vector3 headRot = headBasis.GetEuler();
            float simulatedX = Mathf.Rad2Deg(headRot.x);

            //Clamp the vertical rotation based on the simulation
            if ((simulatedX >= -89f && simulatedX <= 89f) && headBasis.z.z > 0){
                head.RotateX(Mathf.Deg2Rad(-mouseMotion.Relative.y * mouseSense));
            }
        }
    }

    public override void _Process(float delta)
    {
        //Tilt the head around the z axis
        Vector3 headRot = head.Rotation;
        headRot.z = Mathf.Deg2Rad(tilt);
        head.Rotation = headRot;

        //Smoothly move the camera even if the game's physics lag behind
        if (Engine.GetFramesPerSecond() > Engine.IterationsPerSecond) {
            camera.SetAsToplevel(true);

            Vector3 Gtrans = head.GlobalTransform.origin;
	    	
	    
	        var cameraGT = camera.GlobalTransform;
            cameraGT.origin = camera.GlobalTransform.origin.LinearInterpolate(Gtrans, camAccel * delta);
	        camera.GlobalTransform = cameraGT;

            Vector3 camRot = camera.Rotation;
            camRot.y = Rotation.y;
            camRot.x = head.Rotation.x;
            camRot.z = head.Rotation.z;
            camera.Rotation = camRot;
        } else {
            camera.SetAsToplevel(false);
            camera.GlobalTransform = head.GlobalTransform;
        }

        //Check if the player is wallrunning
        if (state != MovementState.sliding) WallRunCheck(delta);

        if (state == MovementState.normal){

            //Reset the fov and camera tilt
            camera.Fov = Mathf.Lerp(camera.Fov, fov, fovChangeRate * delta);
            tilt = Mathf.Lerp(tilt, 0, fovChangeRate * delta);

            CapsuleMesh capsMesh = (CapsuleMesh)mesh.Mesh;
            capsMesh.MidHeight = Mathf.Lerp(capsMesh.MidHeight, 4f, delta * 5f);
            mesh.Mesh = capsMesh;

            CapsuleShape shape = (CapsuleShape)collider.Shape;
            shape.Height = Mathf.Lerp(shape.Height, 4f, delta * 5f);
            collider.Shape = shape;

            mesh.Translation = mesh.Translation.LinearInterpolate(Vector3.Zero, delta * 5f);
            collider.Translation = mesh.Translation;

            head.Translation = head.Translation.LinearInterpolate(new Vector3 (head.Translation.x, 2.1f, head.Translation.z), delta * 5f);
        }

        weaponCam.GlobalTransform = camera.GlobalTransform;
        weaponCam.Fov = camera.Fov;

        //Release/capture the mouse if 'escape' is pressed
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (Input.GetMouseMode() == Input.MouseMode.Visible)
                Input.SetMouseMode(Input.MouseMode.Captured);
            else
                Input.SetMouseMode(Input.MouseMode.Visible);
        }   
    }

    public override void _PhysicsProcess(float delta)
    {  
        grounded = groundCheck.IsColliding();
        Vector3 normal = groundCheck.GetCollisionNormal();

        //Gather the player's inputs
	    var forwardInput = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");
	    var horizontalInput = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");

        Vector2 input = new Vector2(horizontalInput, forwardInput);
        if (state == MovementState.sliding){
            input = Vector2.Zero;
        }

        //Store the player's relative velocity
        Vector2 mag = FindRelativeVel(movement);

        //Reset the speed multipliers
        float speedMultiplierX = 1f;
        float speedMultiplierZ = 1f;

        //Ground check
        if (grounded) {
            airJump = true;
            if (state == MovementState.normal) snap = -normal;         
		    accel = accel_type["default"];
		    gravityVec = Vector3.Zero;
        } else {
            snap = Vector3.Down;
		    accel = accel_type["air"];
            float grav = gravity;
            if (state == MovementState.wallrunning) grav = wallRunGrav;

            if (IsOnCeiling()){
                additionalVel.y = 0;
            }

            //First decrease the additional velocity before adding additional gravity force
            if (additionalVel.y > 0){
                additionalVel += Vector3.Down * grav * delta;
            } else if (gravityVec.y < 100) {
                additionalVel = Vector3.Zero;
                gravityVec += Vector3.Down * grav * delta;
            }		   

            //Set the air speed multipliers 
            if (state != MovementState.sliding){
                speedMultiplierX = 5f;
                speedMultiplierZ = 1.75f;
            }   

            if (Input.IsActionJustPressed("dash") && state == MovementState.normal && gforce >= 30){
                if (input.x * mag.x < 0){
                    mag.x = 0;
                }
                if (input.y * mag.y < 0){
                    mag.y = 0;
                }
                additionalVel.y = 0;
                gravityVec = Vector3.Zero;
                Vector2 dir = FindVelFromMag(input);
                additionalVel += new Vector3(dir.x, 0, dir.y) * dashForce;
                gforce -= 30;
            }        
        }

        Slide(normal, delta);

        //Check if the player jumps on the ground or mid-air
        if (Input.IsActionJustPressed("jump")) {
            if (grounded){
                snap = Vector3.Zero;
                if (state != MovementState.sliding){                   
                    Jump(jumpForce, normal * jumpForce);
                } else {
                    Jump(jumpForce * 0.5f, normal * jumpForce * 0.6f);
                    Vector2 dir = new Vector2(velocity.x, velocity.z).Normalized();
                    if (mag.Length() > 30 && gforce >= 10){
                        additionalVel += new Vector3(dir.x, 0, dir.y) * jumpForce * 0.5f;
                        gforce -= 10;
                    }                                        
                }                
            } else if (state == MovementState.normal && (gforce >= 20 || airJump)){
                Jump(jumpForce * 1.75f, Vector3.Zero);
                if (!airJump){
                    gforce -= 20;
                }
                airJump = false;
            }           
        }

        //The player decelerates slower when going above top speed
        if (mag.Length() > speed + 1){
            accel *= 0.8f;
            fov = highSpeedfov;  
            if (gforce <= gforceLevel2 && mag.Length() > speed + 20f){
                gforce += gforceGain * delta * 1.5f;
            } else {
                gforce += gforceGain * delta;
            }            
        } else {
            if (mag.Length() > 30 && gforce <= gforceLevel1){
                gforce += gforceGain * delta;
            }
            fov = basefov;
        }

        //Add a counter movement if the player has a counter input
        Vector2 newAccel = CounterMovement(input, mag, delta);
        if (state == MovementState.sliding){
            accel *= 0.1f;
            newAccel = new Vector2(accel, accel);
        }     

        float magX = mag.x;
        float magY = mag.y;
        Vector2 inputNorm = input.Normalized();

        //Smoothly interpolate the velocity on each axis
        magX = Mathf.Lerp(magX, inputNorm.x * speed, newAccel.x * delta * speedMultiplierX);
        magY = Mathf.Lerp(magY, inputNorm.y * speed, newAccel.y * delta * speedMultiplierZ);

        //Find the global velocity from the player's new relative velocity
        Vector2 newMag = new Vector2(magX, magY);
        Vector2 vel = FindVelFromMag(newMag);
        velocity = new Vector3(vel.x, velocity.y, vel.y);

        //Check if the player's collided with an object that isn't the floor
        if (GetSlideCount() > 0){
            Vector2 collisionNormal = new Vector2(GetSlideCollision(0).Normal.x, GetSlideCollision(0).Normal.z);

            Vector3 normDif = GetFloorNormal() - GetSlideCollision(0).Normal;

            if (collisionNormal != Vector2.Zero && normDif.Length() > 0.01){
                //Account for collisions
                velocity = IntegrateCollisions(velocity, collisionNormal);
            }  
        }

	    movement = velocity + gravityVec + additionalVel;
        additionalVel = new Vector3(0, additionalVel.y, 0);

        Vector2 horizVel = new Vector2(movement.x, movement.z);
        if (horizVel.Length() > maxSpeed){
            horizVel = horizVel.Normalized() * maxSpeed;
            movement = new Vector3(horizVel.x, movement.y, horizVel.y);
        }
	
	    MoveAndSlideWithSnap(movement, snap, Vector3.Up);

        EmitSignal("UpdateBars", Mathf.Round(mag.Length()), gforce);
    }

    private void Jump(float vertForce, Vector3 normVec){
        gravityVec = Vector3.Zero;

        additionalVel.y = 0;

        additionalVel += Vector3.Up * vertForce;
        additionalVel += normVec;
    }

    private void WallRunCheck(float delta){
        bool canWallRun = !minJumpHeightCheck.IsColliding();
        wallLeft = leftWallCheck.IsColliding();
        wallRight = rightWallCheck.IsColliding();

        if (canWallRun && (wallLeft || wallRight)){
            WallRun(delta);
        } else {
            state = MovementState.normal;
        }
    }

    private void WallRun(float delta){
        state = MovementState.wallrunning;

        //Interpolate the fov and camera tilt
        camera.Fov = Mathf.Lerp(camera.Fov, fov + additionalfov, fovChangeRate * delta);
        if (wallLeft){
            tilt = Mathf.Lerp(tilt, -camTilt, fovChangeRate * delta);
        } else {
            tilt = Mathf.Lerp(tilt, camTilt, fovChangeRate * delta);
        }

        //Wall jump
        if (Input.IsActionJustPressed("jump")){
            Vector3 normalVector = new Vector3();

            if (wallLeft){
                normalVector = leftWallCheck.GetCollisionNormal();
            } else {
                normalVector = rightWallCheck.GetCollisionNormal();
            }

            Jump(jumpForce * 1.25f, normalVector * jumpForce * 2.5f);
        }
    }

    private void Slide(Vector3 normal, float delta){
        if (Input.IsActionPressed("crouch") && state != MovementState.wallrunning){
            state = MovementState.sliding;

            camera.Fov = Mathf.Lerp(camera.Fov, fov + additionalfov, fovChangeRate * delta);

            mesh.Translation = mesh.Translation.LinearInterpolate(new Vector3 (0f, -1.5f, 0f), delta * 5f);

            CapsuleMesh capsMesh = (CapsuleMesh)mesh.Mesh;
            capsMesh.MidHeight = Mathf.Lerp(capsMesh.MidHeight, 1f, delta * 6f);
            mesh.Mesh = capsMesh;

            CapsuleShape shape = (CapsuleShape)collider.Shape;
            shape.Height = Mathf.Lerp(shape.Height, 1f, delta * 6f);
            collider.Shape = shape;

            collider.Translation = mesh.Translation;

            head.Translation = head.Translation.LinearInterpolate(new Vector3 (head.Translation.x, -1.2f, head.Translation.z), delta * 5f);

            additionalVel += new Vector3(normal.x, 0, normal.z);
        } else if (Input.IsActionJustReleased("crouch")){
            state = MovementState.normal;           
        }
    }

    private Vector2 CounterMovement(Vector2 input, Vector2 mag, float delta){
        Vector2 newAccel = new Vector2(accel, accel);
        if (grounded){
            if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(input.x) < 0.05f || (mag.x < -threshold && input.x > 0) || (mag.x > threshold && input.x < 0)){
                newAccel.x = accel * 1.5f;
            }
            if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(input.y) < 0.05f || (mag.y < -threshold && input.y > 0) || (mag.y > threshold && input.y < 0)){
                newAccel.y = accel * 1.5f;
            }
        } else {
            if ((mag.x < -threshold && input.x > 0) || (mag.x > threshold && input.x < 0)){
                newAccel.x = accel * 3f * 0.2f;
            }
            if ((mag.y < -threshold && input.y > 0) || (mag.y > threshold && input.y < 0)){
                newAccel.y = accel * 3f * 0.5f;
            }
            if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(input.x) < 0.05f){
                newAccel.x = accel * 0.2f;
            }
            if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(input.y) < 0.05f){
                newAccel.y = accel * 0.5f;
            }
        }
    
        return newAccel;
    }

    private Vector2 FindRelativeVel(Vector3 linearVelocity){

        float lookAngle = Rotation.y;

        Vector2 mag = new Vector2(linearVelocity.x, linearVelocity.z).Rotated(lookAngle);
        return mag;
    }

    private Vector2 FindVelFromMag(Vector2 mag){

        float lookAngle = -Rotation.y;

        Vector2 vel = new Vector2(mag.x, mag.y).Rotated(lookAngle);
        return vel;
    }

    private Vector3 IntegrateCollisions(Vector3 vel, Vector2 normal){

        float mag = Mathf.Pow(normal.x, 2) + Mathf.Pow(normal.y, 2);

        float a = (vel.x * normal.y) - (vel.z * normal.x);
        float y = (-1 * a * normal.x) / mag;
        float x = (a * normal.y) / mag;

        Vector3 newVel = new Vector3(x, vel.y, y);

        return new Vector3(x, vel.y, y);
    }

    public enum MovementState{
        normal,
        wallrunning,
        sliding,
    }
}