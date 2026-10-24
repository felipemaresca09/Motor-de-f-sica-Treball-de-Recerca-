namespace PhysicsEngine;

public abstract class BaseObject
{
    public Rigidbody? rb;
    public Collider? Col;

    //Funció DrawObject: cada objecte l'ha de modificar a la seva manera, determina com es renderitzarà l'objecte (la forma, el color, etc.).
    //Se li ha de passar el valor de l'escala (guardat a la classe World) per passar de metres a píxels.
    public virtual void DrawObject(float scale)
    { }


    //La funció Dev_Update s'executa abans de qualsevol cosa en el bucle del joc, serveix per ajustar algunes magnituds físiques importants de manera automàtica.
    public void Dev_Update()
    {
        if (rb != null && Col != null)
        {
            if (rb.needUpdate == true)
            {
                rb.SetIM(Col);
            }
        }
    }

    //Funció Update, s'executa a l'inici de cada bucle de joc.
    public virtual void Update()
    { }
}