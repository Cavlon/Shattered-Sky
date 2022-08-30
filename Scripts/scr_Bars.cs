using Godot;
using System;

public class scr_Bars : Control
{
    private Label speedLabel;
    private TextureProgress gforceBar;

    public override void _Ready()
    {
        speedLabel = GetNode<Label>("SpeedLabel");
        gforceBar = GetNode<TextureProgress>("GForceBar");
    }

    void SetValues(float speed, float gforce){
        speedLabel.Text = speed + "km/h";
        gforceBar.Value = gforce;
    } 
}
