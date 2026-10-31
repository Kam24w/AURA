using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform camaraPrincipal;

    void Start()
    {
        if (Camera.main != null)
        {
            camaraPrincipal = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camaraPrincipal != null)
        {
            // Apuntar la interfaz hacia la dirección de la cámara
            transform.LookAt(transform.position + camaraPrincipal.forward);
        }
    }
}