using Godot;
using System.Collections.Generic;
using Qkmaxware.GodotAddons.Inspector;

public class Player : KinematicBody
{
    [Header("Movement")]
    [Export] private float speed = 7f;
    [Export] private float gravity = 9.8f;
    [Export] private float jumpForce = 5f;
    [Export] private float normalAccel = 7f;
    [Export] private float airAccel = 1f;
    private Dictionary<string, float> accel_type = new Dictionary<string, float>();
    private Spatial head;
    private Vector3 snap;
    private Vector3 direction = new Vector3();
    private Vector3 velocity = new Vector3();
    private Vector3 gravityVec = new Vector3();
    private Vector3 jumpVec = new Vector3();
    private Vector3 movement = new Vector3();
    private float accel;
    private float threshold = 0.1f;


    [Header("WallRun")]
    [Export] private float wallRunGrav;
    [Export] private float wallRunJumpForce;
    private RayCast leftWallCheck;
    private RayCast rightWallCheck;
    private RayCast minJumpHeightCheck;
    private bool wallRunning;
    private bool wallLeft;
    private bool wallRight;


    [Header("Camera")]
    [Export] private float mouse_sense = 0.1f;
    [Export] private float fov;
    [Export] private float wallRunfov;
    [Export] private float wallRunfovTime;
    [Export] private float camTilt;
    [Export] private float camTiltTime;
    private Camera camera;
    private float cam_accel = 40f;
    private float tilt;

    public override void _Ready()
    {
        Node raycasts = GetNode("Raycasts");
        accel_type.Add("default", normalAccel);
        accel_type.Add("air", airAccel);
        accel = accel_type["default"];
        head = GetNode<Spatial>("Head");
        camera = GetNode<Spatial>("Head").GetChild<Camera>(0);
        leftWallCheck = raycasts.GetNode<RayCast>("WallLeft");
        rightWallCheck = raycasts.GetNode<RayCast>("WallRight");
        minJumpHeightCheck = raycasts.GetNode<RayCast>("MinJump");

        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion) {
            RotateY(Mathf.Deg2Rad(-mouseMotion.Relative.x * mouse_sense));
            head.RotateX(Mathf.Deg2Rad(-mouseMotion.Relative.y * mouse_sense));
            Vector3 rotDeg = head.RotationDegrees;
            rotDeg.x = Mathf.Clamp(rotDeg.x, -89f, 89f);
            head.RotationDegrees = rotDeg;
        }
    }

    public override void _Process(float delta)
    {
        Vector3 headRot = head.Rotation;
        headRot.z = Mathf.Deg2Rad(tilt);
        head.Rotation = headRot;

        if (Engine.GetFramesPerSecond() > Engine.IterationsPerSecond) {
            camera.SetAsToplevel(true);

            Vector3 Gtrans = head.GlobalTransform.origin;
	    	
	    
	        var cameraGT = camera.GlobalTransform;
            cameraGT.origin = camera.GlobalTransform.origin.LinearInterpolate(Gtrans, cam_accel * delta);
	        camera.GlobalTransform = cameraGT;

            Vector3 camRot = camera.Rotation;
            camRot.y = Rotation.y;
            camRot.x = head.Rotation.x;
            camera.Rotation = camRot;
        } else {
            camera.SetAsToplevel(false);
            camera.GlobalTransform = head.GlobalTransform;
        }

        WallRun(delta);

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
        direction = Vector3.Zero;       
        var h_rot = GlobalTransform.basis.GetEuler().y;
	    var f_input = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");
	    var h_input = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");

        Vector2 input = new Vector2(h_input, f_input);

        Vector2 mag = FindRelativeVel(movement);

        float speedMultiplierX = 1f;
        float speedMultiplierZ = 1f;

        if (IsOnFloor()) {
            snap = -GetFloorNormal();
		    accel = accel_type["default"];
		    gravityVec = Vector3.Zero;
        } else {
            snap = Vector3.Down;
		    accel = accel_type["air"];
            float grav = gravity;
            if (wallRunning) grav = wallRunGrav;
            if (jumpVec.y > 0){
                jumpVec += Vector3.Down * grav * delta;
            } else {
                jumpVec = Vector3.Zero;
                gravityVec += Vector3.Down * grav * delta;
            }		    
            speedMultiplierX = 2.25f;
            speedMultiplierZ = 1.1f;
        }

        direction = new Vector3(h_input, 0, f_input).Rotated(Vector3.Up, h_rot).Normalized();

        if (Input.IsActionJustPressed("jump") && IsOnFloor()) {
            snap = Vector3.Zero;
		    jumpVec = Vector3.Up * jumpForce;
            jumpVec += GetFloorNormal() * jumpForce;
        }

        Vector2 newAccel = CounterMovement(input, mag, delta);     

        float magX = mag.x;
        float magY = mag.y;
        Vector2 inputNorm = input.Normalized();

        magX = Mathf.Lerp(magX, inputNorm.x * speed, newAccel.x * delta * speedMultiplierX);
        magY = Mathf.Lerp(magY, inputNorm.y * speed, newAccel.y * delta * speedMultiplierZ);
        Vector2 newMag = new Vector2(magX, magY);
        Vector2 vel = FindVelFromMag(newMag);
        velocity = new Vector3(vel.x, velocity.y, vel.y);

	    movement = velocity + gravityVec + jumpVec;
        jumpVec = new Vector3(0, jumpVec.y, 0);
	
	    MoveAndSlideWithSnap(movement, snap, Vector3.Up);

    }

    private void WallRun(float delta){
        bool canWallRun = !minJumpHeightCheck.IsColliding();
        wallLeft = leftWallCheck.IsColliding();
        wallRight = rightWallCheck.IsColliding();

        if (canWallRun && (wallLeft || wallRight)){
            wallRunning = true;
            StartWallRun(delta);
        } else {
            wallRunning = false;
            StopWallRun(delta);
        }
    }

    private void StartWallRun(float delta){
        camera.Fov = Mathf.Lerp(camera.Fov, wallRunfov, wallRunfovTime * delta);

        if (wallLeft){
            tilt = Mathf.Lerp(tilt, -camTilt, camTiltTime * delta);
        } else {
            tilt = Mathf.Lerp(tilt, camTilt, camTiltTime * delta);
        }

        if (Input.IsActionJustPressed("jump")){
            Vector3 normalVector = new Vector3();
            gravityVec = Vector3.Zero;

            if (wallLeft){
                normalVector = leftWallCheck.GetCollisionNormal();
            } else {
                normalVector = rightWallCheck.GetCollisionNormal();
            }
            jumpVec = Vector3.Up * jumpForce * 1.25f;
            jumpVec += normalVector * jumpForce * 4f;
        }
    }

    private void StopWallRun(float delta){
        camera.Fov = Mathf.Lerp(camera.Fov, fov, wallRunfovTime * delta);
        tilt = Mathf.Lerp(tilt, 0, camTiltTime * delta);
    }

    private Vector2 CounterMovement(Vector2 input, Vector2 mag, float delta){
        Vector2 newAccel = new Vector2(accel, accel);

        if (IsOnFloor()){
            if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(input.x) < 0.05f || (mag.x < -threshold && input.x > 0) || (mag.x > threshold && input.x < 0)){
                newAccel.x = accel * 1.5f;
            }
            if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(input.y) < 0.05f || (mag.y < -threshold && input.y > 0) || (mag.y > threshold && input.y < 0)){
                newAccel.y = accel * 1.5f;
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
}
