namespace PhysicsEngine;

public static class IMoment_Functions
{
    //Càlcul del moment d'inèrcia d'un cercle.
    public static float IM_Circle(float mass, Collider col)
    {
        if (mass == float.PositiveInfinity)
        {
            return float.PositiveInfinity;
        }
        else
        {
            float I = (1f / 2f) * mass * (col.Radius * col.Radius);
            return I;
        }
    }

    //Càlcul del moment d'inèrcia d'una caixa.
    public static float IM_Box(float mass, Collider col)
    {
        if (mass == float.PositiveInfinity)
        {
            return float.PositiveInfinity;
        }
        else
        {
            float I = (1f / 12f) * mass * ((col.width * col.width) + (col.height * col.height));
            return I;
        }
    }
}