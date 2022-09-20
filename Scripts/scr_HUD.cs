using Godot;
using System;

public class scr_HUD : Control
{

    public Control bars;
    private TextureRect crosshair;
    private Label currentWeaponLabel;
    private string weaponName = "Weapon";

    public override void _Ready()
    {
        bars = GetNode<Control>("Bars");
        crosshair = GetNode<TextureRect>("Crosshair");
        currentWeaponLabel = GetNode<Label>("CurrentWeaponLabel");
        

        Vector2 resolution = new Vector2(1920, 1080);
        Vector2 size = crosshair.RectScale * crosshair.RectSize;
        crosshair.RectPosition = (resolution - size) / 2f;

        currentWeaponLabel.Text = weaponName;
    }

    private void UpdateCurrentWeapon(string weaponName){     
        this.weaponName = weaponName;
        if (currentWeaponLabel != null){
            currentWeaponLabel.Text = weaponName;
        }        
    }
}
