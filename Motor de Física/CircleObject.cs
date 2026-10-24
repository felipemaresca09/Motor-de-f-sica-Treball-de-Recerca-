using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PhysicsEngine;

public class Circle : BaseObject
{
    World world;
    float t;

    public Circle(World objworld, Vector2 vPos, float mass, float rad, float e, float ang) : base()
    {
        rb = new Rigidbody(vPos, mass, e, ang);
        Col = new CircleCollider(rad, vPos, ang);
        world = objworld;
        t = world.GetTime();
    }

    public override void DrawObject(float scale)
    {
        base.DrawObject(scale);
        Raylib.DrawCircleV(rb.pos * scale, Col.Radius * scale, Color.White);
    }

    public override void Update()
    {
        base.Update();
        t = world.GetTime();
    }
}