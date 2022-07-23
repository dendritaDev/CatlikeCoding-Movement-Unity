using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	Rigidbody body, connectedBody, previousConnectedBody;

	[SerializeField, Range(0f, 100f)]
	float maxSpeed = 10f;

	[SerializeField, Range(0f, 100f)]
	float maxAcceleration = 10f, maxAirAcceleration = 1f;

	[SerializeField, Range(0f, 100f)]
	float jumpHeight = 2f;

	[SerializeField, Range(0, 5)]
	int maxAirJumps = 0;

	[SerializeField, Range(0f, 90f)]
	float maxGroundAngle = 25f, maxStairsAngle = 50f;

	[SerializeField, Range(0f, 100f)]
	float maxSnapSpeed = 100f; //la velocidad maxima hassta la que se hace lo de snap, si es mas alta que esto directamente no se hace snap

	[SerializeField, Min(0f)]
	float probeDistance = 1f;

	[SerializeField]
	LayerMask probeMask = -1, stairsMask = -1;

	[SerializeField]
	Transform playerInputSpace = default;

	bool desiredJump;
	int groundContactCount, steepContactCount;
	bool OnGround => groundContactCount > 0; //En vez de tener en cuenta si contacta con algo o no, lo que haremos es contar si contacta con mas de una cosa con un entero y en caso de ser así, será true
	bool OnSteep => steepContactCount > 0;

	int jumpPhase;
	int stepsSinceLastGrounded; //esto es para saber cuantos frames de fisicas o algo así que el llama "physics steps", se han dado antes de que onground sea true.
	int stepsSinceLastJump; //esto lo usaremos para que no se haga snap cuando saltemos porque sino quitaria el upward momentum del salto

	float minGroundDotProduct, minStairsDotProduct;

	Vector3 velocity, desiredVelocity, connectionVelocity;
	Vector3 contactNormal, steepNormal; //contactNormal es para el suelo y steepNormal para los suelos que estan como muy inclinados y son muy verticales o algo asi

	Vector3 upAxis;
	Vector3 rightAxis, forwardAxis; //como hasta ahora el movimiento en Y solo se controlaba con si habia colision y con la gravedad/saltos, si cambiamos de plano, como el plano de esa Y ahroa es laX y la Z sobre la que ueremos movernos
									//tenemso que definir de nuevo esos ejes para que se actualicen segun cada plano

	Vector3 connectionWorldPosition, connectionLocalPosition; //el primero es apra el desplazamiento y els egundo para la rotación.
	
	float GetMinDot (int layer)
    {
		return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct; //Temario: Surface contact 2.3
	}
	void OnValidate()
    {
		minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad); //Nosotros queremos hablar de los grados como grados, sin embargo la funcion cos trata radianes, asi que simplemente lo multiplicamos por una funcion para que lo convierta en radianes y ya esta
		minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
	}
    private void Awake()
    {
		body = GetComponent<Rigidbody>();
		body.useGravity = false;
		OnValidate();
    }

    void Update()
	{
		Vector2 playerInput;
		playerInput.x = Input.GetAxis("Horizontal"); //Get axis solo va de -1 a 1, por eso tenemos un limite de hasta donde podemos mover la bola, pq en trasnform pasamos directamente este valor
		playerInput.y = Input.GetAxis("Vertical");
		playerInput = Vector2.ClampMagnitude(playerInput, 1f); //Antes usabamos esto: playerInput.Normalize(); Pero el clamp nos permite solo ajustar la posicion si su posicion excede uno o -1, con lo que
															   //podemos mover la bola por todos los puntos del circulo que llega a hacer
		
		if(playerInputSpace) //comprobamos si hay alguna camara asignada
        {
			//desiredVelocity = playerInputSpace.TransformDirection(playerInput.x, 0f, playerInput.y) * maxSpeed;
			////Transforms direction from local space to world space.

			//Vector3 forward = playerInputSpace.forward;
			//forward.y = 0f;
			//forward.Normalize();
			//Vector3 right = playerInputSpace.right;
			//right.y = 0f;
			//right.Normalize();
			//desiredVelocity = (forward * playerInput.y + right * playerInput.x) * maxSpeed;

			rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis); //proyectamos los ejes right y forzar segun el upAxis de cada plano para que la camara se ponga tambien en la espalda del jugador esté en el plano que este
			forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);

		}
		else
        {
			//desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

			rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis); 
			forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);

		}
		desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed; //la velocidad ahora es definida segun los ejes del plano donde nos encontremos
		desiredJump |= Input.GetButtonDown("Jump");

		//GetComponent<Renderer>().material.SetColor("_Color", onGround ? Color.black : Color.white);
	}

    private void FixedUpdate()
    {
		/*upAxis = -Physics.gravity.normalized;*/ //nuestro eje vertical será el contrario a donde tira la feurza de la gravedad
		Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
		UpdateState();
		AdjustVelocity();

		if (desiredJump)
		{
			desiredJump = false;
			Jump(gravity);
		}

		velocity += gravity * Time.deltaTime; //esta linea seria ele quivalente a la gravedad que ejerce unity si le damos al tick de la gravedad en la esfera. Pero ahroa la gravedad la aplicaremos nosotros
											  //y eso lo hacemos añadiendole a la velocidad cada frame la gravedad que hay en ese isntante segun el plano en el que esté
		body.velocity = velocity;
		ClearState();
	}

	void ClearState()
    {
		groundContactCount = steepContactCount = 0;
		contactNormal = steepNormal = connectionVelocity = Vector3.zero; //a todo esto le damos de valor 0.
		previousConnectedBody = connectedBody;
		connectedBody = null;
    }

	void UpdateState()
    {
		stepsSinceLastGrounded += 1;
		stepsSinceLastJump += 1;
		velocity = body.velocity;

		if(OnGround || SnapToGround() || CheckSteepContacts())
        {
			stepsSinceLastGrounded = 0;
			if(stepsSinceLastJump > 1)
            {
			jumpPhase = 0;

            }
			if(groundContactCount > 1)
            {
			contactNormal.Normalize();
            }
		}
		else //linea 158
		{
			contactNormal = /*Vector3.up;*/upAxis;
		}

		if(connectedBody)
        {
			if(connectedBody.isKinematic || connectedBody.mass >= body.mass) //esto es para comprobar que con loq ue estamos en contacto no son objetos pequeños sin importancia, solo queremos objetos que se muevan y mas grandes que el player/esfera
            {
			UpdateConnectionState();
            }
        }

	}


	bool SnapToGround() //este metodo lo que hará es que la bola se quede stucked en el suelo 
	{
		if(stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2) //cuando se de mas de un frame fisico en el que no estamos en contacto con el suelo retornamos false y por tanto no hacemos que se quede stucked, mientras que cuando solo haya uno si, asi evitamos que se de lo de ghost colliders
									  // y asi la bola no va haciendo pequeños rebotes si p.e hay dos suelos diferntes juntos y uno solo esta un milimetro por encima del otro
									  //tampoco dejaremos que se haga snap cuando saltemos y el salto dure mas de dos frames de fisica
        {
			return false;
        }

		float speed = velocity.magnitude;
		if (speed > maxSnapSpeed)
        {
			return false;
        }

		if(!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask)) //esto comprueba si hay suelo debajo, ya que si la condicion de arriba no se da, y entre dos superficies digamos que hay un vacio y esta pensado para que el jugador lo tenga que saltar, si solo es un frame
														//la condicion de arriba nos lo tiraria para abajo, a pesar de que hay vacio. Entonces lo que comprueba eta funcion es si debajo de la bola hay algo con un rayo que devuelve si hay algo rollo rayosX en embarazo
        {
			return false;
        }
		float upDot = Vector3.Dot(upAxis, hit.normal);
		if (/*hit.normal.y*/upDot < GetMinDot(hit.collider.gameObject.layer)) //con lo que choca el raycast es retornado a traves de outRaycast, de esto podemos hcer el vector normal y calcular si entra dentro del rango que tenemos considerado que la bola esta en el aire o no.
											   //En caso de que sea menor a ese angulo que consideramos, será que la pelota no esta en el suelo y se supone que tiene que estar en el aire, por tanto retornamos un false
        {
			return false;
        }

		//si no ha pasado nada de lo que hay arriba entonces hacemos snap de la pelota al suelo
		groundContactCount = 1; //y como hacemos el snap al suelo le damos de valor 1 al groundContactcount, para que se reinicie en 1.
		contactNormal = hit.normal;

		//con estas 2 lineas lo que hacemos es ajsutar la velocidad al ground al que enganchamos la bola, funciona igual que lo que haciamos de velocidad deseada, pero aqui tenemos que mantener la velocidad que ya teniamos
		float dot = Vector3.Dot(velocity, hit.normal); //asi que lo calculamos explicitamente en vez de usando la funcion ProjectOnContactPlane:
		if(dot > 0f) //pero lo que está explicando encima solo haremos cuando dot sea positivo, es decir, cuando se de la situacion de que solo hay un frame fisico y se tiene que pegar la bola al suelo, que de normal la gravedad lo haría, pero habra en ocasioens que no
			//y es cuando entonces lo forzamos nosotros
        {
		velocity = (velocity - hit.normal * dot).normalized * speed;
        }

		connectedBody = hit.rigidbody;
		return true; 
	}

	void Jump(Vector3 gravity)
    {
		//velocity.y += 5f;  //En vez de hacer algo asi de simple, vamos a aplicar fisicas. Sabemos que en el tiro vertical la formula de la velocidad final es la raiz cuadrado de -2*gravedad*altura. Para saltar requerira que superemos la gravedad
		//si estamos en el suelo o solo hemos hecho un salto, ya que ahora permitiremos hacer dos saltos y el segundo puede ser estemos en el aire o donde sea.

		Vector3 jumpDirection;
		if(OnGround)
        {
			jumpDirection = contactNormal;
        }
		else if (OnSteep)
        {
			jumpDirection = steepNormal;
			jumpPhase = 0;
        }
		else if(maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
			if(jumpPhase == 0)
            {
				jumpPhase = 1;
            }
			jumpDirection = contactNormal;
        }
        else
        {
			return;
        }
			stepsSinceLastJump = 0;
			jumpPhase += 1;
			float jumpSpeed = Mathf.Sqrt(2f * /*Physics.gravity.magnitude*/gravity.magnitude * jumpHeight);
			jumpDirection = (jumpDirection + upAxis).normalized; //esto es lo que nos permite escalar muros
			float alignedSpeed = Vector3.Dot(velocity, /*contactNormal*/jumpDirection);

			if(/*velocity.y*/alignedSpeed > 0f) //como queremos que cuando salte la velocidad sea la misma haga un vote o dos, tenemos que ahcer algunso cambios. Ya que sino como le vamos sumando el jumspeed, la velocidad en y y por tanto la distancia que puede moverse se va acumulando mucho, en vez de ser la misma por cada salto
            {
			//jumpSpeed = jumpSpeed - velocity.y; //entonces, lo que hacemos si la velocidad de y es mayor que 0 es recalcular la velocidad de jumpspeed para que lo que le sumemos a velocit.y sea como maximo el valor que hemos definido 4 lineas más arriba para jumspeed

			//Sin embargo, si lo dejaramos como la linea de arriba, que podria pasar? Que si en algun momento la velocidad de y es mayor que jumspeed, si saltaramos, en vez de aumentar la velocidad de y, nos la restaria.
			//Así que por tal de evitar eso utilizamos la funcion mathf.max que le asigna a jumspeed un valor maximo entre (jumspeed velocity.y) y (0), para asegurarnos de que nunca pueda ser un numero negativo y por tanto disminuyera la velocidad

			/* jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f); */ //Ahora ya no usamos un float, sino que un vector3:

				jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);

			}

			velocity += /*contactNormal*/jumpDirection * jumpSpeed; //Antes era velocity.y += jumspeed, pero ahora queremos que el salto pueda ser no solo vertical, sino segun la sueprficie que tocamos, asi que lo hacemos usando vector3. En el caso que este en el aire
                                                  //contactNormal sería un vector.up que es : (0,1,0), así que un salto en el aire, si que solo nos moveria hacia arriba


            /*  "Now that the jumps are aligned with the slopes each sphere in our test scene gets a unique jump trajectory. " 
                "Spheres on steeper slopes no longer jump straight into their slopes but do get slowed down as the jump pushes " 
                "them in the opposite direction that they're moving." */

        

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
		float minDot = GetMinDot(collision.gameObject.layer);
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

			float upDot = Vector3.Dot(upAxis, normal);
			if (/*normal.y*/upDot >= minDot) //plane
            {
				groundContactCount += 1;
				contactNormal += normal; //Antes era solo = normal, pero por si acaba la bola en un sitio con varias pendeintes y por tanto varios sitios donde colisiona,
										 //lo que hacemos es acumular todos los vectores normales en contact normal y despues lo normalizamos en updatescene para que sea como si estuviera en un plano normal y no pete todo lo que hemos hecho
										 //y la bola no se comporte raro
				connectedBody = collision.rigidbody;
            } 
			else if (/*normal.y*/upDot > -0.01f) //slope/rampa
            {
				steepContactCount += 1;
				steepNormal += normal;
				if(groundContactCount == 0)
                {
					connectedBody = collision.rigidbody; //dice que preferimos un plano a una rampe n terminos de connectedbody que nos pueda mover, asi que solo le asignaremos a connectedbody una rampa si no estamos tocando ningun plano
                }
            }

			//Ahora queremos ^Hacer que los saltos varien segun el angulo, asi que lo que hacemos es conservar lo de que si la normal.y es mayor al minground, onground es true y ademas, guardamos en un vector3, 
			//el vector normal que se da en ese punto en contacto entre bola y superficie
			//pero como este if solo tiene en cuenta si estamos en colision, en la funcion update ponemos que si jonground es falso, es decir, estamos en el aire, el contactNormal es igual a (0,1,0).


		}


    }

	//Vector3 ProjectOnContactPlane (Vector3 vector)
	Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
	{
		/*return vector - contactNormal * Vector3.Dot(vector, contactNormal);*/ //3.5Moving Along Slope: Aqui lo que hacemos es projectar la velocidad a la que iremos en un determinado plano, ya que aunque cuando va hacia arriba la bola, funciona bien, es porque
				//el motor de fisics detecta que la bola es empujada y que hay colision y tira hacia arriba. Pero cuando la bola baja una rampa, como lo que se esta moviendo es la velocidad horizontal, hace que vaya hacia el aire y caiga, con lo que
				//cuando la bola baja, lo hace botando y no como se deslizase:


				//Para arreglar esto, tenemos que proyectar el vector de la bola horizontal para el plano que esta bajando.
				//Es decir loq ue ahcemos es alinear la velocidad de la bola con el plano.


				//que es lo que hacemos? Obtenemos el vector normal de la pelota en la rampa y lo multiplicamos por el producto escalar entre este y el vector de la bola en el eje que nos interese
				//despues al vector de la bola le restamos el de la normal multiplicado por el dot product.
				//No acabo de entenderlo mucho como funciona, pq no lo explica. 

				//LO QUE CREO por pasos:
				//
				//-LQC.1: En esta funcion cogemos el vector normal y lo proyectamos sobre el eje que nos interese (x o z)
		return (direction - normal * Vector3.Dot(direction, normal)).normalized;

    }

	void AdjustVelocity ()
    {
		//Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized; //-LQC.2:Estas dos lineas lo que hacen es normalizar la proyeccion que tenemos para que sea vector unitario
		//Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

		Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
		Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);

		Vector3 relativeVelocity = velocity - connectionVelocity; //esto lo hacemos servir para si estamos encima de algo que se mueve, pero tambien nos sirve en general para la velocidad, ya que si no estamos encima de nada le restariamos a nuestra velocidad 0, asi que seria lo mismo.
		float currentX = Vector3.Dot(relativeVelocity, xAxis);//-LQC.3: Aqui lo que el dice que hacemos es proyectar la velocidad que tenemos en cada uno de los ejes
		float currentZ = Vector3.Dot(relativeVelocity, zAxis);

		float acceleration = OnGround ? maxAcceleration : maxAirAcceleration; //si esta en el aire la aceleracion y maxspeed sera una, si esta en el suelo otra .Esto para hacerlo mas realista, pq un personaje debe ser mas dificil de controlar en cuanto a movimiento, si se encuentra en el aire
		float maxSpeedChange = acceleration * Time.deltaTime;

		float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange); //aqui calculamos la velocidad nueva pero respecto al ground
		float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

		velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ); //En vez de asignarle la velocidad nueva tal cual para que no parezca que se teletransporta hacemos la diferencia entre los dos vectores y lo multiplicamos por los normales
	}

	bool CheckSteepContacts()
	{
		if (steepContactCount > 1)
		{
			steepNormal.Normalize();
			float upDot = Vector3.Dot(upAxis, steepNormal);
			if (/*steepNormal.y*/upDot >= minGroundDotProduct)
			{
				steepContactCount = 0;
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
			}
		}
		return false;
	}


	//returns whether it succeeded in converting the steep contacts into virtual ground.If there are multiple steep contacts then normalize them and check whether the result counts as ground.If so, return success, 
	//otherwise failure.In this case we don't have to check for stairs

	void UpdateConnectionState()
    {
		if(connectedBody == previousConnectedBody)
        {
			Vector3 connectionMovement = /*connectedBody.position - connectionWorldPosition;*/ connectedBody.transform.TransformPoint(connectionLocalPosition) - connectionWorldPosition; //aqui lo que hacemos es restar a la posicion actual del cuerpo que se eta moviendo la del frame anterior (connectionworldposition)
			connectionVelocity = connectionMovement / Time.deltaTime; //el resultado lo dividimos entre el tiempo y ya tenemos la velocidad a la que se mueve el objeto
		}
		
		connectionWorldPosition = /*connectedBody*/body.position; //como es una animacioo, el bloque no tiene velocidad, asi que de alguna manera la determinamos scando su posicion en cada frame y actualizando asi a nuestra esfera/player
		connectionLocalPosition = connectedBody.transform.InverseTransformPoint(connectionWorldPosition);
    }

}