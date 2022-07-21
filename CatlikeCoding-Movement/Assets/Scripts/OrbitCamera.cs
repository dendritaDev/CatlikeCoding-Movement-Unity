using UnityEngine;

[RequireComponent(typeof(Camera))]

public class OrbitCamera : MonoBehaviour
{
    [SerializeField]
    Transform focus = default;

    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.5f;

    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f; //esto representaria la velocidad de rotacion que es 90º por segundo.

    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -45f, maxVerticalAngle = 45f; //Esto es para que solo podamos rotar verticalmente la camera hasta ciertos puntos, para que no se pueda dar la vuelta verticalmente

    [SerializeField, Min(0f)]
    float alignDelay = 5f; //esto es los segundos que tardara la rotacion horizontal de manera automatica para que se quede en la espalda del personaje por defecto, despues de que la hayamos rotado

    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;

    float lastManualRotationTime;

    Vector3 focusPoint, previousFocusPoint;
    Vector2 orbitAngles = new Vector2(45f, 0f); //esto representara las rotacioens en X e Y a los que mira la camara. En X sera 45, es decir estara mirando en un punto medio entre mirar al horizonte o al suelo
                                                //mientras que en Y (horizontalmente) la rotacion sera 0

    void Awake()
    {
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }
    void LateUpdate()
    {
        UpdateFocusPosition();
        ManualRotation();
        Quaternion lookRotation;
        if(ManualRotation() || AutomaticRotation()) //si se mueve la camara ajustamos el lookrotation segun lo que hayamos movido
        {
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);
        }
        else //si no se mueve simplemente lookrotation sera la rotacion que tenga de defecto la camara
        {
            lookRotation = transform.localRotation;
        }
        
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;



        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void UpdateFocusPosition()
    {
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;

        if (focusRadius > 0f)
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;

            if (distance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime); //en vez de deltaTime utilizamos unscaled, esto es para prevenir que si hay algun momento de slow motion effects no se congele la camara o se relantice mucho
            }
            if(distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
            }

            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t); //antes no era t, sino focusradius/distance. Pero ahora lo que hacemos es asignarle a t ese valor o el valor de t si es menor que este

            //https://docs.unity3d.com/ScriptReference/Vector3.Lerp.html
            //Esto lo que hace es decir que si el resultado de la division es 0, focuspoint = targetPoint
            //Si es 1, focuspoint = focuspoint;
            //si es 0.5 focuspoint = (targetpoint + focuspoint)  /2

            //Con lo que esto nos sirve para decir: Si la distancia de targetpoint a focus point es digamos 3, por tanto es mayor que focusradius(1)
            //ASi que 1/3 = 0.3 y por tanto focuspoint tendria que acercarse a un punto entre targetpoint y focuspoint, pero mas cercano a targetpoint

        }
        else //si la posicion de la bola respecto a la posicion previa~, es menor a 1, no movemos la camara
        {
            focusPoint = targetPoint;
        }

    }

    bool ManualRotation()
    {
        Vector2 input = new Vector2(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera")); //estos input se definen en unity:
            //Edit - project settings - input y ahi añadimos los nombres que queremos y las teclas

        const float e = 0.001f;
        if (input.x < -e || input.x > e || input.y < -e || input.y > e) //los valores input.x y input.y son inicialmente 0, al minimo que pulsemos alguna de las tecla que los mueve y por tanto modifijca su valor, se rota la camara
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }

    bool AutomaticRotation()
    {

        if(Time.unscaledTime - lastManualRotationTime < alignDelay) //si no han pasado 5 segundos desde que se rotó la camara false, si ya han pasado mas, true
        {
            return false;
        }

        Vector2 movement = new Vector2(focusPoint.x - previousFocusPoint.x, focusPoint.z - previousFocusPoint.z);
        float movementDeltaSqr = movement.sqrMagnitude; //con esto calculamos la magnitud de la rotacion que ha habido

        if(movementDeltaSqr < 0.0001f)  //y aqui miramos si es realmente relevante la rotacion, si no lo es simplemente no hacemos automaticrotation
        {
            return false;
        }

        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr)); //aqui le pasamos a getangle la direccion a la que mira el movimiento normalizada.
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle)); //calcula el total de angulo que deberá girar automaticamente
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);

        if(deltaAbs < alignSmoothRange) //mientras sea menor que el angulo que hemos dicho de 45º, hacemos que la rotacion automatica sea mas despacita/smooth, mientras que si lo supera,
                                        //se hace mas rapido ya que en este if lo que hacemos es hacer a rotationchange mas pequeño
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        } 
        else if (180f - deltaAbs < alignSmoothRange) //aqui lo que comprobamos es si la rotacion la estamos haciendo de fuera a dentro, es decir rotamos para acercar la pelota al centro.
                                                    //en caso de que se asi lo que hacemos es
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }
        
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange); //es eje y pero es para la rotacion horizontal. Nose pq la rotacion en eje y es para dercha e izqueirda. Y la del eje x es arriba abajo.
        return true;
    }

    static float GetAngle(Vector2 direction) //esto lo hacemos para calcular el angulo horizontal que coincide con la direccion en la que mira el jugador
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle : angle; //el angulo que podemos obtener puede ser tanto en direccion a las agujas como a la contraria. Asi que con esto lo que hacemos es asegurarnos que lo damos 
                                                        //en el orden de las agujas del reloj
    }

    void OnValidate() //esto es una funcion de MonoBehaviour que controla cosas como que algunos valores que se dan a seralizedfields, se cumplan y sino, los cambia aqui dentro
    {
        if(maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    void ConstrainAngles() //esto es para asegurarnos que la rotacion en el eje horizontal de la camara se queda entre 0 y 360
    {
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if(orbitAngles.y < 0f)
        {
            orbitAngles.y += 360f;
        } 
        else if(orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }

}