using UnityEngine;

public static class CustomGravity
{

	public static Vector3 GetGravity(Vector3 position) //retorna la gravedad que hay en un momento concreto, cuando es llamada la funcion
	{
        return Physics.gravity;
        //return position.normalized * Physics.gravity.y; //esto es para que funcioen la gravedad en esferas
    }

	public static Vector3 GetUpAxis(Vector3 position) //esto retorna el upAxis de ese momento en el qsue se llama la funcion
	{
        return -Physics.gravity.normalized;

  //      Vector3 up = position.normalized;
		//return Physics.gravity.y < 0f ? up : -up;
	}

	public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis) //este out quiere decir que el parametro upAxis vector 3 que se le pasa a esta funcion una vez es pasado
																		   //va a ser posible que se lo modifique dentro y si se hace el original quedará mdoificado
	{
        upAxis = -Physics.gravity.normalized;                               //esta funcion hace las dos cosas de antes, retorna modificado el upAxis y retorna la gravedad de ese preciso momento
        return Physics.gravity;

        //upAxis = position.normalized; //para que funcione en esferas
        //return upAxis * Physics.gravity.y;

        //Vector3 up = position.normalized;
        //upAxis = Physics.gravity.y < 0f ? up : -up;
        //return up * Physics.gravity.y;
    }
}