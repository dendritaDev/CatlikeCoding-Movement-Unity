using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class DetectionZone : MonoBehaviour
{
[SerializeField]
UnityEvent onEnter = default, onLastExit = default;

List <Collider> colliders = new List<Collider> ();

    void Awake()
    {
        enabled = false; //par que no se este comprobando cada fixedupdate de este objeto si no tenemos nada dentro lo que haemos es dque en awake hacemos que este unabled y lo volvemos a hacer enabled cuando entra algo.
                           //de la misma manera cuando se queda vacio y no hay nada dentro lo hacemos enabled =false (unabled, vaya).
    }

    void FixedUpdate() 
    {
        for (int i = 0; i < colliders.Count; i++)//tmb tenemos que tener en cuenta que los elementos que hayan en zonas detectables pueden ser destruidos o desactivados sus colliders o lo que sea. Así que en cada step fisico tenemos que comprobar
                                                 //si los colliders son validos. Si son false queire decir que han sido destruidos, de ser asi tenemos que comprobar si el objeto ha sido desactivado que eso lo podemos ahcer con activeInHierarchy, de ser asi, lo quitamos de la lista
                                                 //y si la lista llega a estar vacia porque todos los que habian dentro han sido eliminados&/desactivados pos invocamos a onlastexit pa que cambie el color
        {
            Collider collider = colliders[i];
            if (!collider || !collider.gameObject.activeInHierarchy)
            {
                colliders.RemoveAt(i--);
                if (colliders.Count == 0)
                {
                    onLastExit.Invoke();
                    enabled = false;
                }
            }
        }
    }

    void OnTriggerEnter (Collider other)
    {
        if(colliders.Count == 0) //esto comprueba si la lista esta vacia. Y solo hacemos que cambie color una vez, cuando entra un item
        {
        onEnter.Invoke();
            enabled = true;
        }

        colliders.Add (other);//aqui vamos añadiendo los colliders a la lista

    }

    void OnTriggerExit (Collider other)
    {
        if (colliders.Remove(other) && colliders.Count == 0) //aqui vamos elimiando los items que salen y cuando eliminamos el ultimo ya llamamos a que cambie de color de nuevo pq no hay nada con lo que hace collision
        {
            onLastExit.Invoke();
            enabled = false;
        }
        
    }

    void OnDisable() //tmb tenemos que tener en cuenta qdue si el game objedt de la detection zone es eliminada, tenemos que limpiar la lista y invocad onlastexit
    {
    #if UNITY_EDITOR //esto es pq en el modo editor de unity, esto siempre se llama, cuando la compilacion del unity, asi que para que no nos limpie o invoque last exit sin que el gameobject se haya destruido
                    //ponemos ese if y endif que se hara cuando estemos en unityeditor y que hara que si el gamobject esta enblaed(no dsuitriod) hacemos que salga de la funcion ondisable con el return
        if (enabled && gameObject.activeInHierarchy)
        {
            return;
        }
    #endif
        if (colliders.Count > 0)
        {
            colliders.Clear();
            onLastExit.Invoke();
        }
    }
}
