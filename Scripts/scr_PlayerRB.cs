using Qkmaxware.GodotAddons.Inspector;
using Godot;

public class scr_PlayerRB : RigidBody
{
    [Header("Movement")]
    [Export] private float speed;
    [Export] private float maxSpeed;
    [Export] private float jumpForce;
    [Export] private float counterMovement;
    [Export] private float gravity;

    private float threshold = 0.01f;
    private bool groundContact;
    private RayCast groundCheck;
    private Node ground;

    private PhysicsDirectBodyState bodyState;

    [Header("WallRun")]
    [Export] float wallRunJumpForce;
    [Export] float wallRunGravity;

    private RayCast leftWallCheck;
    private RayCast rightWallCheck;
    private RayCast minJumpHeightCheck;
    private bool wallRunning;
    private bool wallLeft;
    private bool wallRight;
    private Vector3 normVec;

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
        Connect("body_entered", this, "OnBodyEntered");
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

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        if (Mathf.Sqrt(Mathf.Pow(LinearVelocity.x, 2) + Mathf.Pow(LinearVelocity.z, 2)) > maxSpeed && groundContact){
            float yVel = LinearVelocity.y;
            Vector3 velNormalised = LinearVelocity.Normalized() * maxSpeed;
            LinearVelocity = new Vector3(velNormalised.x, yVel, velNormalised.z);
        }

        bodyState = state;
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
        
        AddCentralForce(Vector3.Down * delta * gravity); 

        if (groundCheck.GetCollider() != null){
            ground = (Node)groundCheck.GetCollider(); 
        } 

        if (groundCheck.IsColliding() == false){
            groundContact = false;
        }

        GD.Print(groundContact);

        Vector2 mag = FindVelRelativeToLook();

        Vector2 moveInput = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

        if (Input.IsActionJustPressed("jump") && groundContact){
            ApplyCentralImpulse(Vector3.Up * delta * jumpForce);
        }
        
        CounterMovement(moveInput, mag, delta);

        if (moveInput.x > 0 && mag.x > maxSpeed) moveInput.x = 0;
        if (moveInput.x < 0 && mag.x < -maxSpeed) moveInput.x = 0;
        if (moveInput.y > 0 && mag.y > maxSpeed) moveInput.y = 0;
        if (moveInput.y < 0 && mag.y < -maxSpeed) moveInput.y = 0;

        float speedMultiplierX = 1f;
        float speedMultiplierZ = 1f;

        if (!groundContact){
            speedMultiplierX = 0.65f;
            speedMultiplierZ = 0.5f;
        }
        
        AddCentralForce(head.GlobalTransform.basis.z * moveInput.y * speed * delta * speedMultiplierZ);
        AddCentralForce(head.GlobalTransform.basis.x * moveInput.x * speed * delta * speedMultiplierX);

    }

    private void Jump(){

    }

    private void CounterMovement(Vector2 input, Vector2 mag, float delta){
        if (!groundContact) return;

        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(input.x) < 0.05f || (mag.x < -threshold && input.x > 0) || (mag.x > threshold && input.x < 0)){
            AddCentralForce(head.GlobalTransform.basis.x * speed * -mag.x * delta * counterMovement);
        }
        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(input.y) < 0.05f || (mag.y < -threshold && input.y < 0) || (mag.y > threshold && input.y > 0)){
            AddCentralForce(head.GlobalTransform.basis.z * speed * mag.y * delta * counterMovement);
        }
    }

    private void OnBodyEntered(Node body){
        normVec = bodyState.GetContactLocalNormal(0);
        if (body == ground){
            groundContact = true;
        }
    }

    private void Look(){
        float simulatedY = cameraHolder.Rotation.x + yRot;
        float simulatedX = cameraHolder.Rotation.y + xRot;

        yRot = xRot = 0;
               
        simulatedY = Mathf.Clamp(simulatedY, Mathf.Deg2Rad(-89.5f), Mathf.Deg2Rad(89.5f));

        float simulatedXDeg = Mathf.Rad2Deg(simulatedX);

        if (simulatedXDeg > 180){
            simulatedXDeg = -180 + (simulatedXDeg - 180);
            simulatedX = Mathf.Deg2Rad(simulatedXDeg);
        }
        if (simulatedXDeg < -180){
            simulatedXDeg = 180 + (simulatedXDeg + 180);
            simulatedX = Mathf.Deg2Rad(simulatedXDeg);
        }


        Vector3 rot = cameraHolder.Rotation;
        rot.y = simulatedX;
        rot.x = simulatedY;
        rot.z = Mathf.Deg2Rad(tilt);
        cameraHolder.Rotation = rot;

        rot.x = 0;
        rot.z = 0;
        head.Rotation = rot;
    }

    private Vector2 FindVelRelativeToLook(){
        float lookAngle = -(Mathf.Rad2Deg(head.Rotation.y));
        float moveAngle = Mathf.Rad2Deg(Mathf.Atan2(LinearVelocity.x, -LinearVelocity.z));

        float u = DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitude = LinearVelocity.Length();
        float yMag = magnitude * Mathf.Cos(Mathf.Deg2Rad(u));
        float xMag = magnitude * Mathf.Cos(Mathf.Deg2Rad(v));

        return new Vector2(xMag, yMag);
    }

    private float DeltaAngle(float angle1, float angle2){
        float angle = (angle2 - angle1) % 360;

        if (angle > 180){
            angle = angle - 360;
        }
        if (angle < -180){
            angle = 360 + angle;
        }

        return angle;
    }
}
