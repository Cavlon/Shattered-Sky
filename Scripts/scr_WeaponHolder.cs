using Godot;
using System;

public class scr_WeaponHolder : Spatial
{
    [Export] private float swayThreshold = 0.5f;
    private Spatial weapon;
    private Vector3 weaponOriginalRot;
    private Vector2 relativeInput;
    private Spatial head;
    private float prevVertRot;
    
    public override void _Ready()
    {
        head = GetParent().GetParent<Spatial>();
        prevVertRot = head.RotationDegrees.x;
        GetWeapon();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion){
            InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
            relativeInput = mouseMotion.Relative;
        }
    }

    public override void _Process(float delta)
    {
        float vertRot = head.RotationDegrees.x;

        Vector3 rot = weapon.Rotation;
        rot.x = Mathf.Deg2Rad(weaponOriginalRot.x);
        rot.y = Mathf.LerpAngle(rot.y, Mathf.Deg2Rad(Mathf.Clamp(weaponOriginalRot.y + relativeInput.x * 2.5f, -25, 25)), delta * 7f);

        if (Mathf.Abs(vertRot - prevVertRot) > swayThreshold){
            rot.z = Mathf.LerpAngle(rot.z, Mathf.Deg2Rad(Mathf.Clamp(weaponOriginalRot.z + -relativeInput.y * 2.5f, -20, 40)), delta * 7f);
        } else {
            rot.z = Mathf.LerpAngle(rot.z, Mathf.Deg2Rad(weaponOriginalRot.z), delta * 7f);
        }

        weapon.Rotation = rot;
        relativeInput = Vector2.Zero;
        prevVertRot = vertRot;
    }

    public void GetWeapon(){
        weapon = GetChild<Spatial>(0);
        weaponOriginalRot = weapon.RotationDegrees;
    }
}
