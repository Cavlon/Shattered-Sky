using Godot;
using System;

public class scr_BulletTrail : Spatial
{

    [Export] private float lifeTime;
    public ShaderMaterial mat;
    private float initialAlpha;
    private float alphaVal;

    public void Initialise(Vector3 colour = new Vector3(), float initialAlpha = 0.25f){
        StartDestroyTimer();
        this.initialAlpha = initialAlpha;
        alphaVal = initialAlpha; 
        mat.SetShaderParam("colour", colour);
    }

    public override void _Process(float delta)
    {
        alphaVal -= delta / (lifeTime * (1 / initialAlpha));
        mat.SetShaderParam("alphaVal", alphaVal);
    }

    private void StartDestroyTimer(){
        GetTree().CreateTimer(lifeTime).Connect("timeout", this, "Destroy");      
    }

    private void Destroy(){
        QueueFree();
    }
}
