using Godot;
using System;

public class scr_MoveCam : Spatial
{
    [Export] private NodePath headPath;

    private Spatial head;

    public override void _Ready()
    {     
        head = GetNode<Spatial>(headPath);
        //OS.WindowMaximized = true;
    }

    public override void _Process(float delta)
    {
        // Set position of the camera to the player's head
        Translation = head.GlobalTransform.origin;
    }
}
