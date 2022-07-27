using UnityEngine;

public class AccelerationZone : MonoBehaviour
{

    [SerializeField, Min(0f)]
    float acceleration = 10f, speed = 10f;

    void OnTriggerEnter(Collider other) //esto hace algo en el momento en que se entra en contacto
    {
        Rigidbody body = other.attachedRigidbody;

        if(body)
        {
            Accelerate(body);
        }
    }

    void OnTriggerStay (Collider other) //esto hace algo mientras la colision se mantenga y por tanto el tringer se este constantemente dando
    {
        Rigidbody body = other.attachedRigidbody;
        if (body)
        {
            Accelerate(body);
        }
    }

    void Accelerate(Rigidbody body)
    {
        Vector3 velocity = /*body.velocity*/transform.InverseTransformDirection(body.velocity);

        if (velocity.y >= speed)
        {
            return;
        }

        if (acceleration > 0f) 
        {
            velocity.y = Mathf.MoveTowards(velocity.y, speed, acceleration * Time.deltaTime); //esto es para que la geometria actue como algo que le haga levitar
        }
        else
        {
            velocity.y = speed; //para que actue como un saltador
        }


        body.velocity = /*velocity*/transform.TransformDirection(velocity);


        if (body.TryGetComponent(out MovingSphere sphere)) //esto lo que mira es si el rigidbody que ha entrado en la geometria tiene un componente llamada movingshere que es el script q tenemos en la sphere
                                                            //de ser asi, lo que hacemos es llamar a la funcio que nos permitrira que la sphere salga lanzadaa hacia arriba
        {
            sphere.PreventSnapToGround();
        }
    }





}