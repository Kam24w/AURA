using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    public Transform objetivo;

    [Header("Ajustes de Cámara")]
    public Vector3 offset = new Vector3(0f, 2.5f, -4f); 
    public float suavizado = 10f;

    [Header("Colisión con Muros")]
    public LayerMask capaColision; 
    public float radioEsfera = 0.2f;

    void LateUpdate()
    {
        if (objetivo == null) return;

        
        Vector3 posicionDeseada = objetivo.position + objetivo.TransformDirection(offset);
        Vector3 puntoDeMira = objetivo.position + Vector3.up * 1.5f;

        Vector3 posicionFinal = posicionDeseada;
        RaycastHit hit;

        
        if (Physics.SphereCast(puntoDeMira, radioEsfera, (posicionDeseada - puntoDeMira).normalized, out hit, Mathf.Abs(offset.z), capaColision))
        {
            posicionFinal = hit.point + hit.normal * radioEsfera;
        }

       
        transform.position = Vector3.Lerp(transform.position, posicionFinal, suavizado * Time.deltaTime);

      
        transform.LookAt(puntoDeMira);
    }
}