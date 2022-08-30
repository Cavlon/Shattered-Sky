using Godot;
using System;

public class scr_SignalConnector : Node
{
    
    private Node player;
    private Node HUDBars;

    public override void _Ready()
    {
        player = GetParent().GetNode("Player");
        HUDBars = GetParent().GetNode("Bars");

        player.Connect("UpdateBars", HUDBars, "SetValues");
    }
}
