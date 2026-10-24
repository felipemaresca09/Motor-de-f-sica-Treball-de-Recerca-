using System.Numerics;
using Raylib_cs;

namespace PhysicsEngine;

public class Rigidbody
{
    //Es creen totes les variables físiques (massa, acceleració, velocitat, posició, moment lineal, rotació, velocitat angular, etc.)

    //Magnituds lineals

    private float m;
    private float mInv; //mInv és la inversa de la massa, és important ja que permet que els objectes kinematic i static no es moguin en les col·lisions.
    public Vector2 a;
    public Vector2 v;
    public Vector2 pos;
    public Vector2 p;
    private Vector2 acF; //acF és l'acumulador de forces, es reinicia en cada bucle de joc, permetent decidir amb major precisió quan aplicar les forces.

    private Vector2 LastFrameForce;
    private List<Vector2[]> CollisionsForces = new List<Vector2[]>();



    //Magnituds angulars

    public float angle; //Rotació (angle en radians)
    public float w; //Velocirar angular en rad/s
    public float angular_a; //Acceleració angular
    private float acT; //Parell de forces, equivalent a la força però en les magnituds angular
    //El moment d'inèrcia depèn de la forma de l'objecte, per tant, s'assigna un valor depenent del tipus de collider.
    private float I; //Moment d'inèrcia (determina que tan difícil es fer girar l'objecte, similar a la massa).
    private float InverseI; //Inversa del moment d'inèrcia, serveix per fer que els objectes Kinematic y Static no es moguin en les col·lisions.



    //Magnituds variades

    private float e; //Coeficient de restitució (determina quanta energia es perd en una col·lisió).

    //Gravity scale és un factor que permet modificar la força de la gravetat.
    private float GravityScale = 1.0f; //1 = Gravetat de la terra 9,8 m/s^2

    private float DynamicFc; //Coeficient de fregament dinàmic, determina la quantitat màxima de fregament possible en el xoc.
    private float StaticFc; //Coeficient de fricció estàtica, determina la quantitat de força necessària per començar a moure l'objecte.




    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




    //physicsNeedUpdate és la variable encarregada de notificar a BaseObject que s'han d'ajustar algunes propietats del rigidbody
    //utilitzat més que res per ajustar quan s'ha de canviar el moment d'inèrcia
    private bool physicsNeedUpdate;

    //needUpdate se encarrega de donar el valor de physicsNeedUpdate perque el BaseObject pugui comprovar si és necessari actualitzar alguna cosa.
    //needUpdate és necessari per evitar errors y que res fora de Rigidbody pugui modificar physicsNeedUpdate.
    public bool needUpdate
    {
        get => physicsNeedUpdate;
    }



    //L'enum State conté els tres tipus de Rigidbody, Static per a objectes que no es mouen, Kinematic per a objectes que es mouen mitjançant codi i no són afectats per les forces.
    //i Dynamic per a objectes afectats per totes les forces.
    public enum State
    {
        Static,
        Kinematic,
        Dynamic,
    }

    //Type és la variable que guarda el tipus de Rigidbody.
    private State Type;



    //Diccionari encarregat de determinar quina funció executar per al càlcul del moment d'inèrcia. S'ha d'actualitzar en cas d'afegir una forma nova.
    private readonly Dictionary<Collider.ColliderType, Func<float, Collider, float>> ITypes = new Dictionary<Collider.ColliderType, Func<float, Collider, float>>
    {
        {Collider.ColliderType.Circle, IMoment_Functions.IM_Circle},
        {Collider.ColliderType.Box, IMoment_Functions.IM_Box},
    };



    //El constructor de la classe Rigidbody permet triar la posició inicial, la massa de l'objecte i el coeficient de restitució.
    public Rigidbody(Vector2 position, float mass = 1f, float restitution = 1f, float ang = 0f, float Dynfc = 0.1f, float Statfc = 0.3f)
    {
        physicsNeedUpdate = true; //physicsNeedUpdate ha de ser true a l'inici per ajustar el moment d'inèrcia.
        m = mass;
        mInv = (1 / m);
        a = new Vector2(0f, 0f);
        v = new Vector2(0f, 0f);
        pos = position;
        angle = ang;
        p = new Vector2(0f, 0f);
        acF = new Vector2(0f, 0f);
        GravityScale = 1.0f;
        DynamicFc = Dynfc;
        StaticFc = Statfc;

        if (restitution > 1f)
        {
            e = 1f;
        }
        else if (restitution < 0f)
        {
            e = 0f;
        }
        else
        {
            e = restitution;
        }

        Type = State.Dynamic;
    }




    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------




    //SetStatic s'encarrega de deixar tot a punt perquè l'objecte passi a ser del tipus Static; s'utilitza una funció perquè la variable Type és privada.
    //Això permet assegurar-se que funcioni sempre correctament.
    public void SetStatic()
    {
        Type = State.Static;
        SetMass(float.PositiveInfinity);
        GravityScale = 0f;
        a = new Vector2(0f, 0f);
        v = new Vector2(0f, 0f);
        p = new Vector2(0f, 0f);
        acF = new Vector2(0f, 0f);
        acT = 0f;
        w = 0f;
        angular_a = 0f;
    }



    //SetKinematic deixa tot a punt perquè l'objecte es converteixi en un tipus Kinematic.
    public void SetKinematic()
    {
        Type = State.Kinematic;
        SetMass(float.PositiveInfinity);
        GravityScale = 0f;
    }



    //SetDynamic configura l'objecte com a Dynamic i el GravityScale com a 1.
    public void SetDynamic()
    {
        Type = State.Dynamic;
        GravityScale = 1f;
    }



    //GetType s'encarrega de retornar el valor de la variable Type; s'utilitza una funció perquè la variable és privada, evitant que ningú modifiqui el tipus de manera incorrecta.
    public State GetType()
    {
        return Type;
    }




    //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




    //Funcions encarregades d'aplicar les forces, els impulsos i de canviar la velocitat.


    //LINEALS:

    //Aplica força a l'acumulador de forces només si l'objecte és del tipus Dynamic.
    public void ApplyForce(Vector2 F)
    {
        if (Type == State.Dynamic)
        {
            acF += F;
        }
    }

    //Configura la velocitat al valor especificat només si l'objecte no és Static.
    public void SetVelocity(Vector2 vel)
    {
        if (Type != State.Static)
        {
            v = vel;
        }
    }

    //Suma el valor indicat a la velocitat només si l'objecte no és Static.
    public void Impulse(Vector2 vel)
    {
        if (Type != State.Static)
        {
            v += vel;
        }
    }

    public void SetCollisionForce(Vector2 point, Vector2 Force)
    {
        Vector2[] vectors = new Vector2[2];
        vectors[0] = point;
        vectors[1] = Force;
        CollisionsForces.Add(vectors);
    }




    //ANGULARS:

    //Aplica un parell de forces a l'objecte, permet que comenci a girar.
    public void ApplyTorque(float T)
    {
        if (Type == State.Dynamic)
        {
            acT += T;
        }
    }

    //Selecciona el valor especificat de velocitat angular.
    public void SetAngularVelocity(float ang_v)
    {
        if (Type != State.Static)
        {
            w = ang_v;
        }
    }

    //Suma el valor d'velocitat angular indicat a la variable de velocitat angular.
    public void AngularImpulse(float ang_v)
    {
        if (Type != State.Static)
        {
            w += ang_v;
        }
    }




    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




    //Funció que assigna un valor al gravity scale, només si l'objecte és "dynamic"; en cas contrari, no l'assigna.
    public void SetGravityScale(float gravityScale)
    {
        if (Type != State.Static && Type != State.Kinematic)
        {
            GravityScale = gravityScale;
        }
    }




    //Funcions del coeficient de restitució (canviar i obtenir).

    //Funció que s'encarrega de canviar el coeficient de restitució de manera correcta.
    public void SetRestitution(float Res)
    {
        if (Res > 1f)
        {
            e = 1f;
        }
        else if (Res < 0f)
        {
            e = 0f;
        }
        else
        {
            e = Res;
        }
    }

    //Funció que s'encarrega de retornar el valor del coeficient de restitució, ja que aquest és una variable privada.
    public float GetRestitution()
    {
        return e;
    }




    //Funcions del coeficient de fricció.

    //SetDynamicFc s'encarrega de donar un valor al coeficient de fricció dinàmica.
    public void SetDynamicFc(float fc)
    {
        DynamicFc = MathF.Abs(fc);
    }

    //GetDynamicFc s'encarrega d'obtenir el valor del coeficient de fricció dinàmica.
    public float GetDynamicFc()
    {
        return DynamicFc;
    }

    //SetStaticFc s'encarrega de donar un valor al coeficient de fricció estàtica.
    public void SetStaticFc(float fc)
    {
        StaticFc = MathF.Abs(fc);
    }

    //GetStaticFc s'encarrega d'obtenir el valor del coeficient de fricció estàtica.
    public float GetStaticFc()
    {
        return StaticFc;
    }




    //Funcions del moment d'inèrcia (canviar depenent de la massa, obtenir).

    //SetIM s'encarrega d'ajustar el moment d'inèrcia depenent de la forma de l'objecte i la massa, i també ajusta la inversa de l'IM.
    public void SetIM(Collider collider)
    {
        if (ITypes.TryGetValue(collider.Type, out var f))
        {
            I = f(m, collider);
            InverseI = (1f / I);
        }
    }
    public float GetIM()
    {
        return I;
    }
    public float GetInverseIM()
    {
        return InverseI;
    }




    //Funcions de massa (canviar i obtenir).

    //Canvia la massa de l'objecte i ajusta la inversa de la massa.
    public void SetMass(float mass)
    {
        m = mass;
        mInv = (1 / mass);
        physicsNeedUpdate = true;
    }


    //Retorna el valor de la massa, ja que aquesta és una variable privada; això es fa per evitar errors en configurar-la.
    public float GetMass()
    {
        return m;
    }

    //Retorna el valor de la inversa de la massa, ja que aquesta és una variable privada; això es fa per evitar errors en configurar-la.
    public float GetInvMass()
    {
        return mInv;
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    //Funcions per dibuixar els vectors en pantalla.

    public bool DrawVelocity = false;
    public bool DrawAcceleration = false;
    public bool DrawLinearMomentum = false;
    public bool DrawForce = false;


    public void DrawVectors(float scale)
    {
        if (DrawVelocity == true)
        {
            Raylib.DrawLineEx(pos * scale, (pos + v) * scale, 5f, Color.Blue);
        }
        if (DrawAcceleration == true)
        {
            Raylib.DrawLineEx(pos * scale, (pos + a) * scale, 5f, Color.Red);
        }
        if (DrawLinearMomentum == true)
        {
            Raylib.DrawLineEx(pos * scale, (pos + p) * scale, 5f, Color.Green);
        }
        if (DrawForce == true)
        {
            Raylib.DrawLineEx(pos * scale, (pos + LastFrameForce) * scale, 5f, Color.Orange);

            for (int i = 0; i < CollisionsForces.Count; i++)
            {
                Raylib.DrawLineEx(CollisionsForces[i][0] * scale, (CollisionsForces[i][0] + CollisionsForces[i][1]) * scale, 5f, Color.Orange);
            }
            CollisionsForces.Clear();
        }
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


    private bool IsGrabbed = false;

    private Vector2 DistancePosMouse;

    public void SetGrabbedTrue(Vector2 MousePos, float Scale)
    {
        if (IsGrabbed == false)
        {
            IsGrabbed = true;
            DistancePosMouse = pos - (MousePos / Scale);
        }  
    }

    public void SetGrabbedFalse()
    {
        if (IsGrabbed == true)
        {
            IsGrabbed = false;
        }
    }

    private Vector2 LastFramePosition;
    private Vector2 LastFramePosition2;
    private Vector2 LastFramePosition3;
    private Vector2 LastFramePosition4;
    private Vector2 LastFramePosition5;

    private Vector2 LastFrameVelocity;
    private Vector2 LastFrameVelocity2;
    private Vector2 LastFrameVelocity3;
    private Vector2 LastFrameVelocity4;
    private Vector2 LastFrameVelocity5;



    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




    //Funció que integra la velocitat i la posició, a més afegeix la força gravitatòria.
    public void Integrate(float dt, Vector2 MousePosition, float Scale)
    {
        if (IsGrabbed == false)
        {
            ApplyForce(new Vector2(0f, (9.81f * m * GravityScale))); //Aplica la gravetat.

            a = acF * mInv; //S'ajusta l'aceleració lineal i angular.
            angular_a = acT * InverseI;

            v += a * dt; //S'integra la velocitat lineal i angular.
            w += angular_a * dt;

            pos += v * dt; //S'integra la posició lineal i angular.
            angle += w * dt;

            p = v * m; //Moment lineal/Quantitat de moviment.

            LastFrameForce = acF;

            acF = new Vector2(0f, 0f); //S'ajustan a 0 els acumuladors de força y torc.
            acT = 0;
        }
        else
        {
            LastFramePosition5 = LastFramePosition4;
            LastFramePosition4 = LastFramePosition3;
            LastFramePosition3 = LastFramePosition2;
            LastFramePosition2 = LastFramePosition;
            LastFramePosition = pos;

            Vector2 MetricMousePos = MousePosition / Scale;

            pos = MetricMousePos + DistancePosMouse;


            LastFrameVelocity5 = LastFrameVelocity4;
            LastFrameVelocity4 = LastFrameVelocity3;
            LastFrameVelocity3 = LastFrameVelocity2;
            LastFrameVelocity2 = LastFrameVelocity;
            LastFrameVelocity = v;

            v = (pos - LastFramePosition5) / (5f * dt);
            p = v * m;

            a = (v - LastFrameVelocity5) / (5f * dt);
        }
    }
}