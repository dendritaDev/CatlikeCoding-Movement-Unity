using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	Rigidbody body;

	[SerializeField, Range(0f, 100f)]
	float maxSpeed = 10f;

	[SerializeField, Range(0f, 100f)]
	float maxAcceleration = 10f, maxAirAcceleration = 0f;

	[SerializeField, Range(0f, 100f)]
	float jumpHeight = 2f;

	[SerializeField, Range(0, 5)]
	int maxAirJumps = 0;

	[SerializeField, Range(0f, 90f)]
	float maxGroundAngle = 25f;

	bool desiredJump;
	bool onGround;

	int jumpPhase;

	float minGroundDotProduct;

	Vector3 velocity, desiredVelocity;
	Vector3 contactNormal;
	
	void OnValidate()
    {
		minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad); //Nosotros queremos hablar de los grados como grados, sin embargo la funcion cos trata radianes, asi que simplemente lo multiplicamos por una funcion para que lo convierta en radianes y ya esta
    }
    private void Awake()
    {
		body = GetComponent<Rigidbody>();
		OnValidate();
    }

    void Update()
	{
		Vector2 playerInput;
		playerInput.x = Input.GetAxis("Horizontal"); //Get axis solo va de -1 a 1, por eso tenemos un limite de hasta donde podemos mover la bola, pq en trasnform pasamos directamente este valor
		playerInput.y = Input.GetAxis("Vertical");
		playerInput = Vector2.ClampMagnitude(playerInput, 1f); //Antes usabamos esto: playerInput.Normalize(); Pero el clamp nos permite solo ajustar la posicion si su posicion excede uno o -1, con lo que
															   //podemos mover la bola por todos los puntos del circulo que llega a hacer
		
		desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

		desiredJump |= Input.GetButtonDown("Jump");
	}

    private void FixedUpdate()
    {
		UpdateState();

		float acceleration = onGround ? maxAcceleration : maxAirAcceleration; //si esta en el aire la aceleracion y maxspeed sera una, si esta en el suelo otra .Esto para hacerlo mas realista, pq un personaje debe ser mas dificil de controlar en cuanto a movimiento, si se encuentra en el aire
		float maxSpeedChange = acceleration * Time.deltaTime;

		velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
		velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

		if (desiredJump)
		{
			desiredJump = false;
			Jump();
		}

		body.velocity = velocity;
		
		onGround = false;
	}

	void UpdateState()
    {
		velocity = body.velocity;

		if(onGround)
        {
			jumpPhase = 0;
		}
		else //linea 158
		{
			contactNormal = Vector3.up;
		}
	}

	void Jump()
    {
		//velocity.y += 5f;  //En vez de hacer algo asi de simple, vamos a aplicar fisicas. Sabemos que en el tiro vertical la formula de la velocidad final es la raiz cuadrado de -2*gravedad*altura. Para saltar requerira que superemos la gravedad
		if(onGround || jumpPhase < maxAirJumps) //si estamos en el suelo o solo hemos hecho un salto, ya que ahora permitiremos hacer dos saltos y el segundo puede ser estemos en el aire o donde sea.
        {
			jumpPhase += 1;
			float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y + jumpHeight);
			float alignedSpeed = Vector3.Dot(velocity, contactNormal);

			if(/*velocity.y*/alignedSpeed > 0f) //como queremos que cuando salte la velocidad sea la misma haga un vote o dos, tenemos que ahcer algunso cambios. Ya que sino como le vamos sumando el jumspeed, la velocidad en y y por tanto la distancia que puede moverse se va acumulando mucho, en vez de ser la misma por cada salto
            {
				//jumpSpeed = jumpSpeed - velocity.y; //entonces, lo que hacemos si la velocidad de y es mayor que 0 es recalcular la velocidad de jumpspeed para que lo que le sumemos a velocit.y sea como maximo el valor que hemos definido 4 lineas más arriba para jumspeed

				//Sin embargo, si lo dejaramos como la linea de arriba, que podria pasar? Que si en algun momento la velocidad de y es mayor que jumspeed, si saltaramos, en vez de aumentar la velocidad de y, nos la restaria.
				//Así que por tal de evitar eso utilizamos la funcion mathf.max que le asigna a jumspeed un valor maximo entre (jumspeed velocity.y) y (0), para asegurarnos de que nunca pueda ser un numero negativo y por tanto disminuyera la velocidad

				/* jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f); */ //Ahora ya no usamos un float, sino que un vector3:

				alignedSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);

			}

			velocity += contactNormal * jumpSpeed; //Antes era velocity.y += jumspeed, pero ahora queremos que el salto pueda ser no solo vertical, sino segun la sueprficie que tocamos, asi que lo hacemos usando vector3. En el caso que este en el aire
                                                  //contactNormal sería un vector.up que es : (0,1,0), así que un salto en el aire, si que solo nos moveria hacia arriba


            /*  "Now that the jumps are aligned with the slopes each sphere in our test scene gets a unique jump trajectory. " 
                "Spheres on steeper slopes no longer jump straight into their slopes but do get slowed down as the jump pushes " 
                "them in the opposite direction that they're moving." */

        }

    }

    private void OnCollisionEnter(Collision collision)
    {
		EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)  //Esto siempre se llama cuando se entra al fixedupdate, por tanto si se detecta en ese fixedupdate que hay colision onground es true y se puede saltar ya sea por chocar contra el suelo como por chocar con pared, pero si no se
		//esta en colision con nada al siguiente frame ya no se puede saltar porque en fixedupdate onground se pone a false en la ultima linea de codigo
    {
		EvaluateCollision(collision);
	}

	void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++) //contactCount: Nos devuelve el numero de puntos de contacto que ha habido en la colision
        {
            Vector3 normal = collision.GetContact(i).normal; //GetContact devuelve el punto exacto de un indice. Y despues .normal calcula su normal (vector perpendicular).
															 //Un punto es un Vector3. En este caso la normal es la direccion a la que la pelota deberia ser empujada, ya que es un vector perpendicular entre la parte que choca y su parte que es chocada

			/*onGround |= normal.y >= 0.9f;*/ //esto es un booleano bitwise. Que quiere decir que onGround sera true cuando onground sea true o cuando la normal.y sea mayor a 0.9f
			//Es como decir:  if(onGround || normal.y=0.9f) {onGround = true} else{onGround = false};

			// 0.9? Si la pelota chocase con un plano horizontal, como la normal apuntaria perpendicularmente el vector seria 1 en la Y, así que la idea de esto es que solo podamos saltar cuando choquemos con el suelo y no con la pared
			//pone 0.9 por si el plano estuviera algo inclinado o algo. Pero la idea es que solo se peuda saltar cuando

			//Sin emabrgo, en los juegos hay cosas mas complejos que planos verticales o horizontales. Y, debido a que la variable onGround, tambien se utiliza para saber que aceleracion se va a utilizar si maxair o max aceleration, esta variable
			//Acaba inf luenciando tambien en la max speed que se puede tener. Así que hay que tener una manera mas especifica y no tan categorica de definir la variable on ground, en vez de que sea true si es mayor o igual a 0.9

			/* onGround |= normal.y >= minGroundDotProduct; */

			//así que lo que vamos a tener en cuenta sera el angulo de las rampas, definiremos un angulo minimo por el cual onground será true, en vez de el 0.9 que usabamos. 
			//Como una rampa no deja de ser un triangulo rectangulo, si contemplamos el lado del suelo y el de la rampa como dos vectores, podemos obtener el escalar del dot product segun ese angulo
			//mediante el dot product -> A · B = |A| · |B| · cos angulo
			//del dot product, lo que obtendremos sera un escalar (numero), que nos da información sobre la magnitud del vector resultante. Si este escalar es menor a la normal.y onground será true

			//De esta manera, si consideramos que a 45º, la pelota no deberia seguir pudiendo avanzar como si estuviera andando normal, sino que deberia considerarse que ya esta volando y q no esta tocando suelo
			//Obtendremos un escalar con el dotproduct, pongamos p.e 0,7, que lo tendremos guardado en la variable minGroundDotProduct. Por tanto cuando la normal.y sea menor a ese 0.7, onGround sera false y por tanto
			//La aceleracion que se tendrá en cuenta será la de aire, lo que modificará la maxspeed a la que pueda ir lo que dificultará que se mueva por tanto, en el caso de las rampas, que pueda superarlas 

			if(normal.y >= minGroundDotProduct)
            {
				onGround = true;
				contactNormal = normal;
            } 

			//Ahora queremos ^Hacer que los saltos varien segun el angulo, asi que lo que hacemos es conservar lo de que si la normal.y es mayor al minground, onground es true y ademas, guardamos en un vector3, 
			//el vector normal que se da en ese punto en contacto entre bola y superficie
			//pero como este if solo tiene en cuenta si estamos en colision, en la funcion update ponemos que si jonground es falso, es decir, estamos en el aire, el contactNormal es igual a (0,1,0).


		}


    }



}