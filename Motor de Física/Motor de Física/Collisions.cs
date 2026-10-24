using System.Numerics;

namespace PhysicsEngine;


//CollisionDetails conté la informació bàsica de qualsevol xoc; una instància d'aquesta classe es passa a la funció SolveCollision de la classe World per decidir de quina manera moure els objectes.
//Conté informació sobre l'overlap (solapament entre dos objectes), la normal (vector unitari amb la direcció del xoc), el punt de col·lisió i els objectes involucrats.
public class CollisionDetails
{
    public float overlap;
    public Vector2 normal;
    public Vector2 CollisionPoint;
    public BaseObject A;
    public BaseObject B;

    public CollisionDetails(float ov, Vector2 n, Vector2 p, BaseObject a, BaseObject b)
    {
        overlap = ov;
        normal = n;
        CollisionPoint = p;
        A = a;
        B = b;
    }
}



//La classe CollisionsMath s'encarrega d'aportar la lògica de cada cas de col·lisió i quina funció executar en cada cas, motiu pel qual és static i no cal crear cap instància per utilitzar les seves funcions.
public static class CollisionsMath
{

    //Diccionari que s'encarrega de desar les possibilitats de diferents tipus de col·lisions.
    private static readonly Dictionary<(Collider.ColliderType, Collider.ColliderType), Func<BaseObject, BaseObject, CollisionDetails>> TypesOfCollision = new Dictionary<(Collider.ColliderType, Collider.ColliderType), Func<BaseObject, BaseObject, CollisionDetails>>
    {
        {(Collider.ColliderType.Circle, Collider.ColliderType.Circle), Calculate_CircleCircle},
        {(Collider.ColliderType.Circle, Collider.ColliderType.Box), Calculate_CircleBox},
    };


    public static float Cross(Vector2 a, Vector2 b)
    {
        float result = (a.X * b.Y) - (a.Y * b.X);
        return result;
    }


    //Funció GetCollisionData encarregada de rebre els dos objectes i comprovar la funció a executar en cada cas; utilitza el diccionari per saber quina funció utilitzar en cada cas.
    public static CollisionDetails GetCollisionData(BaseObject A, BaseObject B)
    {
        (Collider.ColliderType, Collider.ColliderType) key = (A.Col.Type, B.Col.Type);

        if (TypesOfCollision.TryGetValue(key, out var collisionfunc))
        {
            return collisionfunc(A, B);
        }

        (Collider.ColliderType, Collider.ColliderType) inverseKey = (B.Col.Type, A.Col.Type);

        if (TypesOfCollision.TryGetValue(inverseKey, out var invColFunc))
        {
            return invColFunc(B, A);
        }

        return null;
    }



    //Funció Calculate_CircleCircle; calcula els detalls de la col·lisió entre dos cercles. Retorna una instància de CollisionDetails amb la informació necessària.
    public static CollisionDetails Calculate_CircleCircle(BaseObject A, BaseObject B)
    {
        float radA = A.Col.Radius; //Radi d'ambdós cercles.
        float radB = B.Col.Radius;

        Vector2 posA = A.Col.Position; //Posició d'ambdós cercles.
        Vector2 posB = B.Col.Position;


        // Vector relatiu de A cap a B

        Vector2 diff = posB - posA; //Vector que uneix el centre d'ambdós cercles (apunta del cercle A al cercle B).
        float distSq = diff.LengthSquared(); //Distància entre ambdós cercles elevada al quadrat. Utilitza la funció LengthSquared, que eleva al quadrat el mòdul del vector.
        float totalRadius = radA + radB; //Suma dels radis dels cercles.


        //Comprova si la distància entre els centres elevada al quadrat és major que la suma dels radis elevada al quadrat.
        //En cas de ser major, significa que no s'estan tocant i, per tant, la funció acaba en aquest punt per estalviar recursos.
        if (distSq > totalRadius * totalRadius)
        {
            return null;
        }

        float distance = MathF.Sqrt(distSq); //Calcula l'arrel quadrada de distSq (distància elevada al quadrat) per obtenir la distància entre ambdós cercles.



        //Defineix les variables que retornarà la funció.

        Vector2 normal; //El vector normal indica la direcció en la qual rebotaran els objectes.
        Vector2 contactPoint; //contactPoint és el punt en el qual ocorre la col·lisió.
        float overlap; //L'overlap indica quant s'estan solapant ambdós cercles.



        // Gestió de la superposició total (ambdós centres en la mateixa posició).
        if (distance <= 0.0001f)
        {
            overlap = totalRadius;
            normal = new Vector2(1, 0); // Normal arbitrària cap a la dreta.
            contactPoint = posA;
        }
        else
        {
            overlap = totalRadius - distance; //L'overlap és igual a la suma dels radis menys la distància que separa els centres
                                              //si la distància entre els centres és igual a la suma dels radis, els cercles no s'estan solapant; si la distància és menor, els cercles es solapen.

            normal = diff / distance; //El vector normal ha de ser un vector unitari (mòdul 1); per tant, s'utilitza el vector que uneix els centres dels cercles, ja que...
                                      //té la mateixa direcció i sentit que el vector normal, i es divideix entre el seu mòdul per tornar-lo unitari.

            // El punt de contacte es calcula sobre la superfície del Cercle A en direcció a B.
            contactPoint = posA + normal * radA; //Per calcular el contactPoint, es parteix de la posició del cercle A i se li suma el vector normal multiplicat pel radi...
                                                 //del cercle A per moure el punt en direcció al centre del cercle B i posicionar-lo just a la vora del cercle A.
        }

        return new CollisionDetails(overlap, normal, contactPoint, A, B); //Retorna la instància de CollisionDetails amb els detalls de la col·lisió.
    }



    //La funció Calculate_CircleBox calcula els detalls de la col·lisió entre un cercle i una caixa. Retorna una instància de CollisionDetails amb la informació necessària.
    public static CollisionDetails Calculate_CircleBox(BaseObject A, BaseObject B) // A: Cercle, B: Caixa
    {
        // Límits de la caixa en coordenades locals (centre a 0,0).
        float minRelX = -(B.Col.width / 2f);
        float maxRelX = B.Col.width / 2f;
        float minRelY = -(B.Col.height / 2f); // Vora superior (-Y)
        float maxRelY = B.Col.height / 2f;  // Vora Inferior (+Y)

        // Vector relatiu del centre de B al centre de A.
        Vector2 relativeVector = A.Col.Position - B.Col.Position;

        // Rotació inversa per portar el cercle a l'espai local de la caixa OBB.
        float cos = MathF.Cos(-B.Col.angle);
        float sin = MathF.Sin(-B.Col.angle);
        float cosInv = MathF.Cos(B.Col.angle);
        float sinInv = MathF.Sin(B.Col.angle);

        Vector2 circlePosRelative = new Vector2(
            (relativeVector.X * cos) - (relativeVector.Y * sin),
            (relativeVector.X * sin) + (relativeVector.Y * cos)
        );

        // Punt més proper a l'interior o a la superfície de la caixa respecte al centre del cercle.
        float px = Math.Clamp(circlePosRelative.X, minRelX, maxRelX);
        float py = Math.Clamp(circlePosRelative.Y, minRelY, maxRelY);
        Vector2 P = new Vector2(px, py);

        // Vector de P cap al centre del cercle (espai local).
        Vector2 distanceVector = circlePosRelative - P;
        float distSq = distanceVector.LengthSquared();
        float radius = A.Col.Radius;

        if (distSq > radius * radius)
        {
            return null; // No hi ha col·lisió
        }

        Vector2 normalRel;
        Vector2 contactPointRel;
        float overlap;

        // CAS 1: El centre del cercle està DINS de la caixa.
        if (P == circlePosRelative)
        {
            float left = circlePosRelative.X - minRelX;
            float right = maxRelX - circlePosRelative.X;
            float top = circlePosRelative.Y - minRelY;       // Distància cap amunt (-Y)
            float bottom = maxRelY - circlePosRelative.Y;    // Distància cap avall (+Y)

            float min = MathF.Min(MathF.Min(left, right), MathF.Min(top, bottom));

            // Normal A -> B (Ha d'apuntar CAP A L'INTERIOR de la caixa.)
            if (min == left)
            {
                normalRel = new Vector2(1, 0); // Apunta cap a la dreta (+X)
                contactPointRel = new Vector2(minRelX, circlePosRelative.Y);
            }
            else if (min == right)
            {
                normalRel = new Vector2(-1, 0); // Apunta cap a l'esquerra (-X)
                contactPointRel = new Vector2(maxRelX, circlePosRelative.Y);
            }
            else if (min == top)
            {
                normalRel = new Vector2(0, 1); // Apunta cap avall (+Y)
                contactPointRel = new Vector2(circlePosRelative.X, minRelY);
            }
            else
            {
                normalRel = new Vector2(0, -1); // Apunta cap amunt (-Y)
                contactPointRel = new Vector2(circlePosRelative.X, maxRelY);
            }

            overlap = min + radius;
        }
        // CAS 2: El centre del cercle està FORA de la caixa.
        else
        {
            float dist = MathF.Sqrt(distSq);

            // Vector A -> B En espai local (del cercle a la caixa).
            normalRel = dist > 0.0001f ? -distanceVector / dist : new Vector2(-1, 0);

            contactPointRel = P;
            overlap = radius - dist;
        }

        // Transformar la normal i el punt de contacte de tornada a l'espai del món.
        Vector2 normal = new Vector2(
            (normalRel.X * cosInv) - (normalRel.Y * sinInv),
            (normalRel.X * sinInv) + (normalRel.Y * cosInv)
        );

        Vector2 contactPoint = new Vector2(
            (contactPointRel.X * cosInv) - (contactPointRel.Y * sinInv),
            (contactPointRel.X * sinInv) + (contactPointRel.Y * cosInv)
        ) + B.Col.Position;

        return new CollisionDetails(overlap, normal, contactPoint, A, B);
    }
}