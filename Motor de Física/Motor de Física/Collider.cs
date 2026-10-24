using System.Numerics;

namespace PhysicsEngine;


//La classe Collider és la classe base de tots els tipus de colliders; tots els altres hereten d'aquesta i cadascun s'encarrega d'especificar el tipus.
//La classe Collider també conté propietats amb característiques de tots els tipus de colliders; aquestes s'encarreguen que es pugui accedir a les característiques de cada collider específic.
//Quan es crea un collider d'un tipus nou, s'han d'especificar les característiques pròpies d'aquest collider específicament.
public abstract class Collider
{
    public Vector2 Position; //Variable de posició pròpia de tots els tipus de colliders.
    public float angle;
    public Vector2 AABB_min;
    public Vector2 AABB_max;


    //Funció encarregada de modificar la posició del collider; cada tipus de collider la pot modificar per ajustar la posició de l'AABB.
    //S'executarà en cada bucle del joc.
    public virtual void SetPosition(Vector2 position, float ang)
    {
        Position = position;
        angle = ang;
    }


    public virtual bool PointInsideCollider(Vector2 point, float scale)
    {
        return false;
    }


    //Enum que conté un valor per a cada tipus de collider; cada collider ha d'especificar el tipus de collider (collider type) que utilitza perquè la resta del programa hi pugui accedir.
    public enum ColliderType
    {
        Circle,
        Box,
    }



    //Propietat de tipus de collider; guarda el tipus de collider de cada objecte. Com que és abstracta, cada collider HA d'especificar el valor que retornarà aquesta propietat amb la finalitat de poder saber el tipus de collider d'un objecte.
    //És en poques paraules el lloc on es guarda quin tipus de collider té un objecte.
    public abstract ColliderType Type
    {
        get;
    }


    //Propietat de radi pròpia dels cercles; només l'hauria de modificar el CircleCollider amb la finalitat de determinar el radi de la circumferència.
    public virtual float Radius
    {
        get => 0f;
        set { }
    }



    //Propietats per als BoxColliders.
    public virtual float width //Amplada de la caixa (eix X).
    {
        get => 0f;
        set { }
    }
    public virtual float height //Alçada de la caixa (eix Y).
    {
        get => 0f;
        set { }
    }
    public virtual Vector2 min //Cantó inferior esquerre de la caixa (s'hauria d'anar ajustant segons la posició de l'objecte, l'amplada del collider i l'alçada del collider).
    {
        get => new Vector2(0f, 0f);
        set { }
    }
    public virtual Vector2 max //Cantó superior dret de la caixa (s'hauria d'anar ajustant segons la posició de l'objecte, l'amplada del collider i l'alçada del collider).
    {
        get => new Vector2(0f, 0f);
        set { }
    }
}





//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------





//Collider d'un cercle; conté propietats com ara radi i posició.
public class CircleCollider : Collider
{
    public float R;

    public override ColliderType Type => ColliderType.Circle;


    //Modifica la propietat Radius de la classe pare Collider perquè mostri el valor de la variable R d'aquesta mateixa classe.
    public override float Radius
    {
        get => R;
        set => R = value;
    }


    //Modifica la funció SetPosition perquè ajusti l'AABB a la posició i al radi del cercle.
    public override void SetPosition(Vector2 position, float ang)
    {
        base.SetPosition(position, ang);
        AABB_min.X = position.X - R;
        AABB_min.Y = position.Y - R;
        AABB_max.X = position.X + R;
        AABB_max.Y = position.Y + R;
    }


    public override bool PointInsideCollider(Vector2 point, float scale)
    {
        Vector2 metricPoint = point / scale;
        if (metricPoint.X >= Position.X - R && metricPoint.X <= Position.X + R)
        {
            if (metricPoint.Y >= Position.Y - R && metricPoint.Y <= Position.Y + R)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }


    //El constructor de la classe CircleCollider demana obligatòriament una posició inicial i un radi.
    public CircleCollider(float r, Vector2 pos, float ang) : base()
    {
        SetPosition(pos, ang);
        Radius = r;
    }


    //Funció que permet modificar el radi del collider mentre s'executa el codi.
    public void SetSize(float r)
    {
        Radius = r;
    }
}




//Collider d'una caixa (quadrat o rectangle); conté amplada, alçada i posició (la posició és la posició del centre de la caixa).
public class BoxCollider : Collider
{
    //Selecciona el tipus de collider Box.
    public override ColliderType Type => ColliderType.Box;


    //Width i height són propietats que permeten ajustar la mida de la caixa.
    public override float width
    {
        get => widthVar;
        set => widthVar = value;
    }
    public override float height
    {
        get => heightVar;
        set => heightVar = value;
    }


    //Min i max són les propietats que ajusten els dos punts de la caixa (inferior esquerre i superior dret).
    public override Vector2 min
    {
        get => minVar;
    }
    public override Vector2 max
    {
        get => maxVar;
    }



    //A part de modificar la posició, ajusta el min i el max tenint en compte la nova posició, i canvia el de l'AABB.
    public override void SetPosition(Vector2 position, float ang)
    {
        base.SetPosition(position, ang);
        minVar = new Vector2(-(width / 2f), -(height / 2f)); //Es calcula on es troben el cantó inferior esquerre i el cantó superior dret de la caixa en coordenades locals.
        maxVar = new Vector2(width / 2f, height / 2f);

        float sin = MathF.Sin(ang); //Sinus i cosinus de l'angle.
        float cos = MathF.Cos(ang);

        float half_width = width / 2f; //Es calcula la meitat de l'amplada i de l'alçada per al càlcul següent.
        float half_height = height / 2f;

        float dist_x = MathF.Abs(cos) * half_width + MathF.Abs(sin) * half_height; //Es calcula quina distància en l'eix x i y recorren la meitat de l'amplada i de l'alçada.
        float dist_y = MathF.Abs(sin) * half_width + MathF.Abs(cos) * half_height; //(quant s'estenen en x i y l'amplada i l'alçada partint des del centre).

        AABB_min = new Vector2(position.X - dist_x, position.Y - dist_y); //Si des del centre de la caixa es pren aquesta distància, en sumar s'arriba al màxim i en restar s'arriba al mínim.
        AABB_max = new Vector2(position.X + dist_x, position.Y + dist_y);
    }


    //Les variables min, max, width i height del box collider serveixen perquè les propietats en tornin el valor.
    private Vector2 minVar;
    private Vector2 maxVar;
    private float widthVar;
    private float heightVar;


    //El constructor del BoxCollider deixa tot a punt per utilitzar-lo.
    public BoxCollider(Vector2 pos, float w, float h, float ang) : base()
    {
        width = w; //S'ajusten l'amplada i l'alçada.
        height = h;
        SetPosition(pos, ang); //S'executa SetPosition amb l'amplada i l'alçada ajustades.
    }
}