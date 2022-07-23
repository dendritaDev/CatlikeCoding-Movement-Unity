using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{

	Rigidbody body;
	float floatDelay;

	[SerializeField]
	bool floatToSleep = false; //esto es para dar la opcion de si queremos que ese objeto pueda estar en sleep o no


	void Awake()
	{
		body = GetComponent<Rigidbody>();
		body.useGravity = false;
	}

	void FixedUpdate()
	{
		if (floatToSleep)
		{
			if (body.IsSleeping()) //esto lo que comprueba es si el objeto que tiene este componente esta quieto
								   //Sleeping is an optimisation that is used to temporarily remove an object from physics simulation when it is at rest.
								   //This function tells if the rigidbody is currently sleeping.  
								   //Basicamente desactiva el rigidbody pq eso consume supongo rendimiento o memoria y si no se esta usando pos mejor tenerlo desactivado

			{
				floatDelay = 0f;
				return;
			}

			if (body.velocity.sqrMagnitude < 0.0001f) //para poder ponerlo a rest, tenemos que comprobar que no se mueve y si no se mueve entonces no le damos aceleracion con addforce, pq sino
													  //como constantemente le estamos dando una aceeracion, nunca se podria ejecutar la condicion que miramos arriba de issleeping
			{
				floatDelay += Time.deltaTime; //como al inicio de todo los cuerpos y todo empeiza quieto, la conficion de la magnitud seria cierta desde un inicio y nunca se haria el add force
											  //asi que hacemos un delay para que se pueda llegar a plicar el addforce al inicio y ya despues vamos comprobando si esta quieto
				if (floatDelay >= 1f)
				{
					return;
				}
			}
			else
			{
				floatDelay = 0f;
			}
		}
		

		body.AddForce(CustomGravity.GetGravity(body.position), ForceMode.Acceleration
		);
	}
}