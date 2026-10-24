using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PhysicsEngine;

public class Box : BaseObject
{
    World world;
    float t;

    public Box(World objworld, Vector2 vPos, float mass, float width, float height, float e, float ang) : base()
    {
        rb = new Rigidbody(vPos, mass, e, ang);
        Col = new BoxCollider(vPos, width, height, ang);
        world = objworld;
        t = world.GetTime();
    }

    public override void DrawObject(float scale)
    {
        base.DrawObject(scale);
        Vector2 size = new Vector2(Col.width, Col.height) * scale;
        Rectangle rect = new Rectangle(Col.Position.X * scale, Col.Position.Y * scale, size.X, size.Y);
        Vector2 or = new Vector2(size.X / 2f, size.Y / 2f);
        Raylib.DrawRectanglePro(rect, or, Col.angle * (180f / MathF.PI), Color.White);
    }
}