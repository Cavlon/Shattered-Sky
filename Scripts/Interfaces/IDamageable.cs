using Godot;
using System;

public interface IDamageable
{
    void TakeDamage(int damage, int defenseLoss = 0);
}
