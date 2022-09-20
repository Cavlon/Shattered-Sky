using Godot;
using System;

public class scr_Aethris : scr_Velocitat
{

    [Export] private Vector3 altTrailColourRGB;
    [Export] private float altInitialTrailAlpha = 0.25f;
    [Export] private int defenseLoss = 1;

    public override void AltShoot()
    {
        if (aimcast.IsColliding()){
            ShotAnim();
            var ray = GetWorld().DirectSpaceState;

            var immersionCheck = ray.IntersectRay(aimcast.GlobalTransform.origin, firePoint.GlobalTransform.origin);
            if (immersionCheck.Count > 0){
                if (immersionCheck["collider"] is IDamageable damageable){
                    damageable.TakeDamage(altDamage, defenseLoss);
                }
                return;
            }

            var bullet = ray.IntersectRay(firePoint.GlobalTransform.origin, aimcast.GetCollisionPoint() + (aimcast.GetCollisionPoint() - firePoint.GlobalTransform.origin).Normalized() * 2f);
            BulletTrail((Vector3)bullet["position"], altTrailColourRGB, altInitialTrailAlpha);
            
            if (bullet.Count > 0){
                if (bullet["collider"] is IDamageable damageable){
                    damageable.TakeDamage(altDamage, defenseLoss);
                }              
            }          
        }   
    }

    public override void ShotAnim(){
        if (TW != null) TW.Stop();        
        TW = CreateTween().SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        TW.TweenProperty(this, "translation:z", 0.1f, 0.05f);
        TW.TweenProperty(this, "translation:z", 0.25f, 0.07f);
        TW.Parallel().TweenProperty(this, "translation:y", 0.12f, 0.05f);
        TW.Parallel().TweenProperty(this, "rotation:x", Mathf.Deg2Rad(80f), 0.05f);
        TW.TweenProperty(this, "translation:z", 0f, 0.5f);
        TW.Parallel().TweenProperty(this, "translation:y", 0f, 0.5f);
        TW.Parallel().TweenProperty(this, "rotation:x", 0f, 0.5f);
    }
}
