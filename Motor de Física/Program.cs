using System.Numerics;
using Raylib_cs;
using PhysicsEngine;

Raylib.InitWindow(1700, 900, "Physics Engine");
Raylib.SetTargetFPS(60);

float t = 0;
Random rng = new Random();
World Wrld = new World();
Vector2 MousePosition;

while (!Raylib.WindowShouldClose())
{
    float deltatime = Raylib.GetFrameTime();
    t += deltatime;
    MousePosition = Raylib.GetMousePosition();

    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
    {
        Wrld.CheckColliderGrabbed(MousePosition);
    }

    if (Raylib.IsMouseButtonReleased(MouseButton.Left))
    {
        Wrld.SetCollidersNotGrabbed();
    }

    if (Raylib.IsKeyPressed(KeyboardKey.A))
    {
        Wrld.ToggleDrawAcceleration();
    }

    if (Raylib.IsKeyPressed(KeyboardKey.V))
    {
        Wrld.ToggleDrawVelocity();
    }

    if (Raylib.IsKeyPressed(KeyboardKey.F))
    {
        Wrld.ToggleDrawForce();
    }

    if (Raylib.IsKeyPressed(KeyboardKey.P))
    {
        Wrld.ToggleDrawLinearMomentum();
    }


    Wrld.Simulate(deltatime, MousePosition);

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);
    Wrld.Draw();
    Raylib.EndDrawing();
}

Raylib.CloseWindow();