using UnityEngine;
using UnityEngine.Events;

public class AutomaticSlider : MonoBehaviour
{
    [SerializeField, Min(0.01f)]
    float duration = 1f;

    float value;

    //[SerializeField] 
    //UnityEvent<float> onValueChanged = default; //No se pueden serializar(que aparezcan en la UI de unity lo unity event), para conseguir que se pueda hacer tenemos que hacerlo de la siguiente manera:

    [System.Serializable]
    public class OnValueChangedEvent : UnityEvent<float> { }

    [SerializeField]
    OnValueChangedEvent onValueChanged = default;

    [SerializeField]
    bool autoReverse = false;

    bool reversed;

    void FixedUpdate() //esto nos servira para hacer que algo vaya de 0 a 1 y que una vez haya llegado a 1 se acabe
    {
        float delta = Time.deltaTime / duration;

        if (reversed)
        {
            value -= delta;
            if (value <= 0f)
            {
                if (autoReverse)
                {
                    value = Mathf.Min(1f, -value);
                    reversed = false;
                }
                else
                {
                    value = 0f;
                    enabled = false;
                }
            }
        }
        else
        {
            value += delta;
            if (value >= 1f) //el value que le demos en la interfaz no puede ser por defecto 1 porque sino esto se activaria al momento
            {
                if(autoReverse)
                {
                    value = Mathf.Max(0f, 2f - value);
                    reversed = true;
                }
                else
                {
                    value = 1f;
                    enabled = false;
                }

            }

        }
        
        onValueChanged.Invoke(value);
    }
}
