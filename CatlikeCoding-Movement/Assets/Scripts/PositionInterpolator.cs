using UnityEngine;

public class PositionInterpolator : MonoBehaviour
{

    [SerializeField]
    Rigidbody body = default;

    [SerializeField]
    Vector3 from = default, to = default;

    public void Interpolate (float t) //float t seria el la variable "value" de automatic slider que empezará en 0 y irá hasta 1
    {
        body.MovePosition(Vector3.LerpUnclamped(from, to, t));  //lo que ahce esta funcion es mover la posicion en funcion del valor t, cuando sea 0 sera from y cuando sea 1 sera to, pero de manera progresiva
    }

}
