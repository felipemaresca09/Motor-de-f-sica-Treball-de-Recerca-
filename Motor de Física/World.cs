using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PhysicsEngine;

public class World
{
    private List<BaseObject> objects = new List<BaseObject>();
    private float scale = 50f; //50 píxels = 1 metre; tota la física està calculada en metres i, en renderitzar-se, es canvia a píxels, per la qual cosa es multiplica per 50.
    private float dt = 0.01f;
    private float t = 0;
    private float tAcc;
    private int CollisionSubsteps = 10;


    //AddObject afegeix a la llista d'objectes de la classe World l'objecte indicat.
    public void AddObject(BaseObject obj)
    {
        objects.Add(obj);
    }


    //Funció Draw; s'encarrega de recórrer tots els objectes executant la seva funció DrawObject, la qual canvia de forma diferent per a cada objecte.
    public void Draw()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            objects[i].DrawObject(scale);
            if (objects[i].rb != null)
            {
                objects[i].rb.DrawVectors(scale);
            }
        }
    }

    public float GetTime()
    {
        return t;
    }


    //S'encarrega de comprovar si la posició del ratolí està just damunt d'un col·lisionador per activar la propietat d'agafada del rigidbody.
    public void CheckColliderGrabbed(Vector2 MousePos)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].Col != null && objects[i].rb != null)
            {
                if (objects[i].Col.PointInsideCollider(MousePos, scale))
                {
                    objects[i].rb.SetGrabbedTrue(MousePos, scale);
                }
            }
        }
    }

    //S'executa en deixar anar el clic esquerre del ratolí; posa en fals la propietat d'agafada de cada rigidbody.
    public void SetCollidersNotGrabbed()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].Col != null && objects[i].rb != null)
            {
                objects[i].rb.SetGrabbedFalse();
            }
        }
    }


    public void ToggleDrawAcceleration()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].rb.DrawAcceleration == false)
            {
                objects[i].rb.DrawAcceleration = true;
            }
            else
            {
                objects[i].rb.DrawAcceleration = false;
            }
        }
    }

    public void ToggleDrawVelocity()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].rb.DrawVelocity == false)
            {
                objects[i].rb.DrawVelocity = true;
            }
            else
            {
                objects[i].rb.DrawVelocity = false;
            }
        }
    }

    public void ToggleDrawForce()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].rb.DrawForce == false)
            {
                objects[i].rb.DrawForce = true;
            }
            else
            {
                objects[i].rb.DrawForce = false;
            }
        }
    }

    public void ToggleDrawLinearMomentum()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].rb.DrawLinearMomentum == false)
            {
                objects[i].rb.DrawLinearMomentum = true;
            }
            else
            {
                objects[i].rb.DrawLinearMomentum = false;
            }
        }
    }





    //Lògica de resolució de col·lisions un cop obtinguts els detalls de la colisió (instància de CollisionDetails).
    public void SolveCollision(CollisionDetails details)
    {
        if (details == null) return;

        float mAInv = details.A.rb.GetInvMass();
        float mBInv = details.B.rb.GetInvMass();
        float totalInvMass = mAInv + mBInv;

        // Si ambdós objectes tenen massa infinita (estàtics), no hi ha resposta.
        if (totalInvMass <= 0f) return;

        // CORRECCIÓ DE POSICIÓ (Separació per evitar el solapament)
        float percent = 0.8f;
        Vector2 correction = details.normal * (details.overlap / totalInvMass) * percent;

        details.A.rb.pos -= correction * mAInv;
        details.B.rb.pos += correction * mBInv;

        // Actualitzar colliders immediatament.
        details.A.Col.SetPosition(details.A.rb.pos, details.A.rb.angle);
        details.B.Col.SetPosition(details.B.rb.pos, details.B.rb.angle);

        // RESOLUCIÓ D'IMPULS
        Vector2 rA = details.CollisionPoint - details.A.Col.Position;
        Vector2 rB = details.CollisionPoint - details.B.Col.Position;

        // Velocitat puntual
        Vector2 vpA = details.A.rb.v + new Vector2(-details.A.rb.w * rA.Y, details.A.rb.w * rA.X);
        Vector2 vpB = details.B.rb.v + new Vector2(-details.B.rb.w * rB.Y, details.B.rb.w * rB.X);

        // Velocitat relativa
        Vector2 relativeVelocity = vpB - vpA;

        // Velocitat al llarg de la normal
        float velocityInNormal = Vector2.Dot(relativeVelocity, details.normal);

        // Si s'estan allunyant, no s'aplica cap impuls.
        if (velocityInNormal >= 0f) return;

        float imInvA = details.A.rb.GetInverseIM();
        float imInvB = details.B.rb.GetInverseIM();

        // Producte creuat en 2D
        float rAcrossN = CollisionsMath.Cross(rA, details.normal);
        float rBcrossN = CollisionsMath.Cross(rB, details.normal);

        // Coeficient de restitució (s'utilitza el mínim d'ambdós objectes).
        float e = MathF.Min(details.A.rb.GetRestitution(), details.B.rb.GetRestitution());

        // Massa efectiva de l'impacte
        float denominatorN = totalInvMass + (rAcrossN * rAcrossN * imInvA) + (rBcrossN * rBcrossN * imInvB);
        float jN = -(1f + e) * velocityInNormal / denominatorN;

        Vector2 impulseN = jN * details.normal;

        // Aplicar impulsos lineals
        details.A.rb.v -= impulseN * mAInv;
        details.B.rb.v += impulseN * mBInv;

        Vector2 CollisionForceA = -impulseN / dt;
        Vector2 CollisionForceB = impulseN / dt;

        details.A.rb.SetCollisionForce(details.CollisionPoint, CollisionForceA);
        details.B.rb.SetCollisionForce(details.CollisionPoint, CollisionForceB);

        // Aplicar impulsos angulars
        details.A.rb.w -= CollisionsMath.Cross(rA, impulseN) * imInvA;
        details.B.rb.w += CollisionsMath.Cross(rB, impulseN) * imInvB;
    }




    //Funció Simulate; conté la lògica d'un fotogrames de física. Executa les funcions Dev_Update i Update, i després integra les magnituds físiques de cada objecte.
    //Després detecta les col·lisions i aplica la lògica de resolució de col·lisions, i finalment dibuixa cada objecte en pantalla.
    public void Simulate(float deltaTime, Vector2 MousePosition)
    {
        tAcc += deltaTime;
        t += deltaTime;

        while (tAcc >= dt)
        {
            //Funció Update i Dev_Update.
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].Dev_Update();
                objects[i].Update();
            }


            //Integrar acceleració, velocitat y posició.
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].rb != null)
                {
                    objects[i].rb.Integrate(dt, MousePosition, scale); //S'ajusten totes les magnituds físiques.
                }
                if (objects[i].Col != null)
                {
                    objects[i].Col.SetPosition(objects[i].rb.pos, objects[i].rb.angle); //S'ajusta la posició del collider a la posició del rigidbody.
                }
            }


            //Comprovar col·lisions
            for (int s = 0; s < CollisionSubsteps; s++)
            {
                for (int i = 0; i < objects.Count; i++) //Es recorren tots els objectes comparant cadascun amb tots els altres objectes.
                {
                    for (int j = i + 1; j < objects.Count; j++)
                    {
                        //Comprovar que tots dos objectes tinguin un collider y un rigidbody, en cas contrari no s'executa res.
                        if (objects[i].Col != null && objects[j].Col != null && objects[i].rb != null && objects[j].rb != null)
                        {
                            if (objects[i].rb.GetType() == Rigidbody.State.Dynamic || objects[j].rb.GetType() == Rigidbody.State.Dynamic) //Comprovació que un dels objectes sigui dynamic.
                            {
                                //Comprovar que els AABB s'estiguin solapant, en cas contrari no s'ejecutara res y es passara al següent objecte.
                                if (objects[i].Col.AABB_max.X < objects[j].Col.AABB_min.X || objects[i].Col.AABB_min.X > objects[j].Col.AABB_max.X || objects[i].Col.AABB_max.Y < objects[j].Col.AABB_min.Y || objects[i].Col.AABB_min.Y > objects[j].Col.AABB_max.Y)
                                {
                                    continue;
                                }
                                else
                                {
                                    SolveCollision(CollisionsMath.GetCollisionData(objects[i], objects[j]));
                                }
                            }
                        }
                    }
                }
            }

            tAcc -= dt;
        }
    }
}