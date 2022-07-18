using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	Rigidbody body;

	[SerializeField, Range(0f, 100f)]
	float maxSpeed = 10f;

	[SerializeField, Range(0f, 100f)]
	float maxAcceleration = 10f;

	[SerializeField, Range(0f, 100f)]
	float jumpHeight = 2f;

	bool desiredJump;
	bool onGround;

	Vector3 velocity, desiredVelocity;

    private void Awake()
    {
		body = GetComponent<Rigidbody>();
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
		velocity = body.velocity;
		float maxSpeedChange = maxAcceleration * Time.deltaTime;

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

	void Jump()
    {
		//velocity.y += 5f;  //En vez de hacer algo asi de simple, vamos a aplicar fisicas. Sabemos que en el tiro vertical la formula de la velocidad final es la raiz cuadrado de -2*gravedad*altura. Para saltar requerira que superemos la gravedad
		if(onGround)
        {

		velocity.y += Mathf.Sqrt(-2f * Physics.gravity.y + jumpHeight);
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
		onGround = true;
    }

    private void OnCollisionStay(Collision collision)  //Esto siempre se llama cuando se entra al fixedupdate, por tanto si se detecta en ese fixedupdate que hay colision onground es true y se puede saltar ya sea por chocar contra el suelo como por chocar con pared, pero si no se
		//esta en colision con nada al siguiente frame ya no se puede saltar porque en fixedupdate onground se pone a false en la ultima linea de codigo
    {
		onGround = true;
    }


}