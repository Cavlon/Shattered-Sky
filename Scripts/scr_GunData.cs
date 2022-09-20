using Godot;
using System;

public class scr_GunData : Resource
{
    [Export] public string name = "Velocitat";
    [Export] public int damage = 10;
    [Export] public int altDamage = 10;
    [Export] public float shotDelay = 0.1f;
    [Export] public float altShotDelay = 0f;
    [Export] public int altGforceCost = 0;
}
