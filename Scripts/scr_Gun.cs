using Godot;
using System;

public abstract class scr_Gun : Spatial
{
    [Export] private scr_GunData gunData;   

    protected Position3D firePoint;
    protected scr_Player player;
    public RayCast aimcast;

    public bool sliding;

    private Vector3 slideRot = new Vector3(0, 0, 90);
    protected int damage;
    protected int altDamage;
    protected float shotDelay;
    protected float altShotDelay;  
    protected float timeSinceShot;
    protected float timeSinceAltShot;
    protected bool hasShot;
    protected bool hasAltShot;
    protected int altGforceCost;
    protected float shotRecoil;
    protected float altShotRecoil;  
    protected SceneTreeTween TW;

    [Signal]
    public delegate void Shot();    

    [Signal]
    public delegate void AltShot();    

    public override void _Ready()
    {
        damage = gunData.damage;
        altDamage = gunData.altDamage;
        shotDelay = gunData.shotDelay;
        altShotDelay = gunData.altShotDelay;
        altGforceCost = gunData.altGforceCost;

        timeSinceAltShot = altShotDelay;
        timeSinceShot = shotDelay;
        
        player = (scr_Player)GetTree().GetNodesInGroup("Player")[0];
        firePoint = GetNode<Position3D>("FirePoint");        
    }

    protected bool CanShoot(){
        return (timeSinceShot > shotDelay && !hasShot) ? true : false;
    }

    protected bool CanAltShoot(){
        return (timeSinceAltShot > altShotDelay && !hasAltShot && player.gforce >= altGforceCost) ? true : false;
    }

    public override void _Process(float delta)
    {
        timeSinceShot += delta;
        timeSinceAltShot += delta;

        Sliding(delta);

        if (Input.IsActionJustPressed("fire") && CanShoot()){
            Shoot();
            EmitSignal("Shot");
            hasShot = true;
            timeSinceShot = 0f;
        }
        if (Input.IsActionJustReleased("fire")){
            hasShot = false;
        }

        if (Input.IsActionJustPressed("alt_fire") && CanAltShoot()){
            AltShoot();
            EmitSignal("AltShot");
            hasAltShot = true;
            timeSinceAltShot = 0f;
            player.gforce -= altGforceCost;
        }
        if (Input.IsActionJustReleased("alt_fire")){
            hasAltShot = false;
        }
    }

    public virtual void ShotAnim(){
        if (TW != null) TW.Stop();   
        TW = CreateTween().SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        TW.TweenProperty(this, "translation:z", 0.07f, 0.05f);
        TW.TweenProperty(this, "translation:z", 0.2f, 0.05f);
        TW.Parallel().TweenProperty(this, "translation:y", 0.1f, 0.05f);
        TW.Parallel().TweenProperty(this, "rotation:x", Mathf.Deg2Rad(70f), 0.05f);
        TW.TweenProperty(this, "translation:z", 0f, 0.35f);
        TW.Parallel().TweenProperty(this, "translation:y", 0f, 0.35f);
        TW.Parallel().TweenProperty(this, "rotation:x", 0f, 0.35f);
    }

    private void Sliding(float delta){
        Vector3 weaponRot = Rotation;
        if (sliding){         
            weaponRot = weaponRot.LinearInterpolate(new Vector3(Mathf.Deg2Rad(slideRot.x), Mathf.Deg2Rad(slideRot.y), Mathf.Deg2Rad(slideRot.z)), delta * 7f);
        } else {
            weaponRot = weaponRot.LinearInterpolate(new Vector3(Mathf.Deg2Rad(slideRot.x), Mathf.Deg2Rad(slideRot.y), 0), delta * 7f);
        }
        Rotation = weaponRot;
    }

    public abstract void Shoot();

    public abstract void AltShoot();
}
