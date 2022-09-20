using Godot;
using System;

public class scr_WeaponHolder : Spatial
{
    [Export] private float swayThreshold = 0.5f;
    [Export] private Vector3 slideRot;
    [Export] private string[] equippedWeaponPaths = new string[4]; 

    private PackedScene[] equippedWeapons = new PackedScene[4];
    private scr_Gun weaponScript;
    private Spatial weapon;
    private Vector3 originalRot = Vector3.Zero;
    private Vector2 relativeInput;
    private Spatial head;
    private RayCast aimcast;
    private Control HUD;
    private int weaponVal;
    private float prevVertRot;
    private bool sliding;
    private bool canScroll = true;
    private bool canSway = true;
    private bool shot;

    [Signal]
    public delegate void UpdateWeaponDisplay(string name);
    
    public override void _Ready()
    {
        head = GetParent<Spatial>();
        HUD = Owner.GetNode<Control>("HUD");
        prevVertRot = head.RotationDegrees.x;
        aimcast = head.GetNode<Camera>("Camera").GetNode<RayCast>("AimCast");
        for (int i = 0; i < equippedWeaponPaths.Length; i++){
            if (equippedWeaponPaths[i] != ""){
                equippedWeapons[i] = (PackedScene)ResourceLoader.Load(equippedWeaponPaths[i]);
            }          
        }

        Connect("UpdateWeaponDisplay", HUD, "UpdateCurrentWeapon");

        GetWeapon();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion){
            relativeInput = mouseMotion.Relative;
        }

        if (@event is InputEventMouseButton mouseButton){
            if (mouseButton.ButtonIndex == (int)ButtonList.WheelUp && canScroll){
                SwitchWeaponUp();
                canScroll = false;
                GetTree().CreateTimer(0.1f).Connect("timeout", this, "ResetScroll");
            }
            if (mouseButton.ButtonIndex == (int)ButtonList.WheelDown && canScroll){
                SwitchWeaponDown();
                canScroll = false;
                GetTree().CreateTimer(0.1f).Connect("timeout", this, "ResetScroll");
            }
        }
    }

    public override void _Process(float delta)
    {
        if (canSway) Sway(delta);
        
    }

    public void GetWeapon(){
        Spatial weaponInstance = (Spatial)equippedWeapons[weaponVal].Instance();
        AddChild(weaponInstance);
        weapon = weaponInstance;
        weapon.RotationDegrees = new Vector3(-90, 0, 0);
        weaponScript = (scr_Gun)weaponInstance;
        EmitSignal("UpdateWeaponDisplay", weapon.Name);
        weaponScript.aimcast = aimcast;
    }

    private void Shot(){
        shot = true;
    }

    private void AltShot(){
        
    }

    private void SwitchWeaponDown(){
        weapon.QueueFree();
        weaponVal -= 1;
        if (weaponVal == -1) weaponVal = 1;        
        GetWeapon();
    }

    private void SwitchWeaponUp(){
        weapon.QueueFree();
        weaponVal += 1;
        if (weaponVal == 2) weaponVal = 0;
        GetWeapon();
    }

    private void ResetScroll(){
        canScroll = true;
    }

    private void Sway(float delta){
        float vertRot = head.RotationDegrees.x;

        Vector3 rot = Rotation;
        rot.z = 0;
        rot.y = Mathf.LerpAngle(rot.y, Mathf.Deg2Rad(Mathf.Clamp(relativeInput.x * 2.5f, -30, 30)), delta * 5f);

        if (Mathf.Abs(vertRot - prevVertRot) > swayThreshold && !shot){
            rot.x = Mathf.LerpAngle(rot.x, Mathf.Deg2Rad(Mathf.Clamp(relativeInput.y * 2.5f, -30, 30)), delta * 5f);
        } else {
            rot.x = Mathf.LerpAngle(rot.x, 0, delta * 7f);
        }

        if (weapon != null){
            weaponScript.sliding = sliding;
            Vector3 weaponPos = Translation;
            if (sliding){         
                weaponPos = weaponPos.LinearInterpolate(new Vector3(weaponPos.x, weaponPos.y, -0.4f), delta * 7f);
            } else {
                weaponPos = weaponPos.LinearInterpolate(new Vector3(weaponPos.x, weaponPos.y, -0.5f), delta * 7f);
            }
            Translation = weaponPos;
        }
        

        Rotation = rot;
        relativeInput = Vector2.Zero;
        prevVertRot = vertRot;
    }

    public void Sliding(bool isSliding){
        sliding = isSliding;
    }
}
