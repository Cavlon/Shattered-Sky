using Godot;
using System;

public class scr_Velocitat : scr_Gun
{
    [Export] private string bulletTrailScenePath = "res://Scenes/BulletTrail.tscn";
    [Export] private string bullletTrailShaderPath = "res://Materials/Shaders/BulletTrailFade.tres";
    [Export] private string bulletTrailMeshPath = "res://Models/BulletTrail.tres";
    [Export] protected Vector3 trailColourRGB;
    [Export] private float trailRadius = 0.025f;
    [Export] protected float initialTrailAlpha = 0.25f;
    [Export] private float dashSpeed = 30f;
    private PackedScene bulletTrailScene;
    private ShaderMaterial bulletTrailShader;
    private CylinderMesh bulletTrailMesh;
    private Spatial head;

    public override void _Ready()
    {
        base._Ready();
        bulletTrailScene = (PackedScene)ResourceLoader.Load(bulletTrailScenePath);
        bulletTrailShader = (ShaderMaterial)ResourceLoader.Load(bullletTrailShaderPath);
        bulletTrailMesh = (CylinderMesh)ResourceLoader.Load(bulletTrailMeshPath);
        head = player.GetNode<Spatial>("Head");
    }
    public override void Shoot()
    {
        if (aimcast.IsColliding()){
            var ray = GetWorld().DirectSpaceState;
            ShotAnim();

            var immersionCheck = ray.IntersectRay(aimcast.GlobalTransform.origin, firePoint.GlobalTransform.origin);
            if (immersionCheck.Count > 0){
                if (immersionCheck["collider"] is IDamageable damageable){
                    damageable.TakeDamage(damage);
                }
                return;
            }

            var bullet = ray.IntersectRay(firePoint.GlobalTransform.origin, aimcast.GetCollisionPoint() + (aimcast.GetCollisionPoint() - firePoint.GlobalTransform.origin).Normalized() * 2f);
            BulletTrail((Vector3)bullet["position"], trailColourRGB, initialTrailAlpha);
            
            if (bullet.Count > 0){
                if (bullet["collider"] is IDamageable damageable){
                    damageable.TakeDamage(damage);
                }              
            }          
        }      
    }

    public override void AltShoot()
    {
        ShotAnim();
        player.snap = Vector3.Zero;

        Vector3 dashDir = -head.GlobalTransform.basis.z.Normalized() * dashSpeed;
        Vector2 relDashDir = player.FindRelativeVel(dashDir);


        if (player.mag.x * relDashDir.x < 0){
            relDashDir.x -= player.mag.x;
        }

        if (player.mag.y * relDashDir.y < 0){
            relDashDir.y -= player.mag.y;
        }

        if (player.gravityVec.y < 0 && dashDir.y > 0){
            player.gravityVec.y = 0;
        }

        if (player.additionalVel.y > 0 && dashDir.y < 0){
            player.additionalVel.y = 0;
        }

        Vector2 newDashDir = player.FindVelFromMag(relDashDir);
        player.additionalVel += new Vector3(newDashDir.x, dashDir.y, newDashDir.y);
    }

    protected void BulletTrail(Vector3 target, Vector3 trailColourRGB, float initialTrailAlpha){
        Spatial bulletTrail = (Spatial)bulletTrailScene.Instance();
        GetTree().Root.AddChild(bulletTrail);

        MeshInstance bulletTrailMeshInstance = bulletTrail.GetNode<MeshInstance>("Mesh");
        CylinderMesh newBulletTrailMesh = (CylinderMesh)bulletTrailMesh.Duplicate();

        newBulletTrailMesh.TopRadius = trailRadius;
        newBulletTrailMesh.BottomRadius = trailRadius;
        newBulletTrailMesh.Height = firePoint.GlobalTransform.origin.DistanceTo(target);
        bulletTrailMeshInstance.Mesh = newBulletTrailMesh;
        bulletTrailMeshInstance.MaterialOverride = (Material)bulletTrailShader.Duplicate();

        scr_BulletTrail trailScript = (scr_BulletTrail)bulletTrail;
        trailScript.mat = (ShaderMaterial)bulletTrailMeshInstance.MaterialOverride;
        trailScript.Initialise(trailColourRGB / 255f, initialTrailAlpha);

        bulletTrail.LookAtFromPosition((firePoint.GlobalTransform.origin + target) * 0.5f, target, Vector3.Up);
    }
}
