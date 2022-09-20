using Godot;
using System;

public class scr_Enemy : KinematicBody, IDamageable
{
    [Export] private int health = 100;
    [Export] private int defense = 3;
    [Export] private float debuffResetTime = 2f;
    
    private int currentDefense;
    private bool debuffed;
    private Timer debuffTimer;

    public override void _Ready()
    {
        currentDefense = defense;
    }

    public void TakeDamage(int damage, int defenseLoss = 0){
        if (defenseLoss != 0 && !debuffed){
            currentDefense -= defenseLoss;
            debuffed = true;
            StartResetDebuff();
        } 

        health -= damage / currentDefense;
        GD.Print(health);
        if (health <= 0) Destroy();
    }

    private void Destroy(){
        QueueFree();
    }

    private void StartResetDebuff(){
        GD.Print("Resetting Debuff");
        GetTree().CreateTimer(debuffResetTime).Connect("timeout", this, "StopDebuffTimer");               
    }

    private void StopDebuffTimer(){
        currentDefense = defense;
        debuffed = false;
        GD.Print("Debuff Reset");
    }
}
