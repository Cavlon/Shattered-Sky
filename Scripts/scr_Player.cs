using Qkmaxware.GodotAddons.Inspector;
using Godot;

public class scr_Player : KinematicBody
{
    [Header("Movement")]
    [Export] private float speed;
    [Export] private float hAcceleration;
    [Export] private float gravity;
    [Export] private float jumpForce;

    private Vector3 dir;
    private Vector3 hVelocity;
    private Vector3 movement;
    private Vector3 gravVec;
    private bool groundContact;
    private RayCast groundCheck;

    [Header("WallRun")]
    [Export] float wallRunJumpForce;
    [Export] float wallRunGravity;

    private RayCast leftWallCheck;
    private RayCast rightWallCheck;
    private RayCast minJumpHeightCheck;
    private bool wallRunning;
    private bool wallLeft;
    private bool wallRight;

    [Header("Camera")]
    [Export] private float mouseSensitivity;
    [Export] private NodePath camHolderPath;
    [Export] private float fov;
    [Export] private float wallRunfov;
    [Export] private float wallRunfovTime;
    [Export] private float camTilt;
    [Export] private float camTiltTime;

    private Spatial cameraHolder;   
    private Camera cam;
    private float tilt;
    private Spatial head;
    private float yRot;
    private float xRot;

    public override void _Ready()
    {
        Node raycasts = GetNode("Raycasts");
        Input.SetMouseMode(Input.MouseMode.Captured);
        cameraHolder = GetNode<Spatial>(camHolderPath);
        cam = cameraHolder.GetNode<Camera>("Camera");
        groundCheck = raycasts.GetNode<RayCast>("GroundCheck");
        leftWallCheck = raycasts.GetNode<RayCast>("LWallCheck");
        rightWallCheck = raycasts.GetNode<RayCast>("RWallCheck");
        minJumpHeightCheck = raycasts.GetNode<RayCast>("MinJumpCheck");
        head = GetNode<Spatial>("Head");
    }

    public override void _Process(float delta)
    {
        Look();
        WallRun(delta);
    }

    public override void _PhysicsProcess(float delta)
    {
        ProcessInput(delta);
    }

    private void ProcessInput(float delta){
        // If escape is pressed, cursor between being hidden and locked to visible and free
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (Input.GetMouseMode() == Input.MouseMode.Visible)
                Input.SetMouseMode(Input.MouseMode.Captured);
            else
                Input.SetMouseMode(Input.MouseMode.Visible);
        }       

        Move(delta);
    }

    public override void _Input(InputEvent @event)
    {
        // Checks if the mouse has moved and casts that
        if (@event is InputEventMouseMotion && Input.GetMouseMode() == Input.MouseMode.Captured){
            InputEventMouseMotion mouseEvent = @event as InputEventMouseMotion;

            float delta = GetProcessDeltaTime();

            //Takes the mouse movement and casts it to an angle
            yRot = Mathf.Deg2Rad(-mouseEvent.Relative.y * mouseSensitivity * delta);
            xRot = Mathf.Deg2Rad(-mouseEvent.Relative.x * mouseSensitivity * delta);
        }
    }

    private void WallRun(float delta){
        bool canWallRun = !minJumpHeightCheck.IsColliding();
        wallLeft = leftWallCheck.IsColliding();
        wallRight = rightWallCheck.IsColliding();
        
        //Checks if the player is high enough and close enough to a wall to wallrun
        if (canWallRun){
            if (wallLeft || wallRight){
                wallRunning = true;
                StartWallRun(delta);
            } else {
                wallRunning = false;
                StopWallRun(delta);
            }
        } else {
            wallRunning = false;
            StopWallRun(delta);
        }
    }

    //Smoothly tilts the camera and increases fov
    private void StartWallRun(float delta){
        cam.Fov = Mathf.Lerp(cam.Fov, wallRunfov, wallRunfovTime * delta);        

        if (wallLeft){
            tilt = Mathf.Lerp(tilt, -camTilt, camTiltTime * delta);
        } else {
            tilt = Mathf.Lerp(tilt, camTilt, camTiltTime * delta);
        }
    }

    //Smoothly resets the camera and resets fov
    private void StopWallRun(float delta){
        cam.Fov = Mathf.Lerp(cam.Fov, fov, wallRunfovTime * delta);
        tilt = Mathf.Lerp(tilt, 0, camTiltTime * delta);
    }

    private void Move(float delta){
        dir = Vector3.Zero;

        groundContact = groundCheck.IsColliding();

        float speedMultiplierX = 1f;
        float speedMultiplierZ = 1f;

        if (!IsOnFloor()){
            if (wallRunning){
                gravVec += Vector3.Down * wallRunGravity * delta;
            } else {
                gravVec += Vector3.Down * gravity * delta;
            }           
            speedMultiplierX = 0.65f;
            speedMultiplierZ = 0.5f;
        } else if (IsOnFloor() && groundContact) {
            gravVec = -GetFloorNormal() * gravity;
        } else {
            gravVec = -GetFloorNormal();
        }

        if (Input.IsActionJustPressed("jump") && (IsOnFloor() || groundContact)){
            gravVec = Vector3.Up * jumpForce;
        }

        if (Input.IsActionPressed("move_forward")){
            dir -= Transform.basis.z;
        } else if (Input.IsActionPressed("move_back")){
            dir += Transform.basis.z;
        }

        if (Input.IsActionPressed("move_left")){
            dir -= Transform.basis.x;
        } else if (Input.IsActionPressed("move_right")){
            dir += Transform.basis.x;
        }

        dir = dir.Normalized();
        hVelocity = hVelocity.LinearInterpolate(dir * speed, hAcceleration * delta);
        movement.x = hVelocity.x + gravVec.x;
        movement.z = hVelocity.z + gravVec.z;
        movement.x *= speedMultiplierX;
        movement.z *= speedMultiplierZ;
        movement.y = gravVec.y;

        MoveAndSlide(movement, Vector3.Up);
    }

    private void Look(){
        // float yRot = Mathf.Deg2Rad(-mouseEvent.Relative.y * mouseSensitivity);
        // float xRot = Mathf.Deg2Rad(-mouseEvent.Relative.x * mouseSensitivity);

        // float simulatedY = cameraHolder.Rotation.x + yRot;
               
        // // Clamps the camera's vertical rotation
        // if (simulatedY < Mathf.Pi / 2 && simulatedY > -(Mathf.Pi / 2)){
        //     cameraHolder.RotateObjectLocal(Vector3.Right, yRot);
        // }    

        // cameraHolder.RotateY(xRot);
        // RotateY(xRot);   
            
        // Transform = Transform.Orthonormalized();       

        float simulatedY = cameraHolder.Rotation.x + yRot;
        float simulatedX = cameraHolder.Rotation.y + xRot;

        yRot = xRot = 0;
               
        simulatedY = Mathf.Clamp(simulatedY, Mathf.Deg2Rad(-89.5f), Mathf.Deg2Rad(89.5f));

        Vector3 rot = cameraHolder.Rotation;
        rot.y = simulatedX;
        rot.x = simulatedY;
        rot.z = Mathf.Deg2Rad(tilt);
        cameraHolder.Rotation = rot;

        rot.x = 0;
        rot.z = 0;
        Rotation = rot;
    }
}
