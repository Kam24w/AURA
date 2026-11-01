using UnityEngine;

/// <summary>
/// Cámara orbital libre alrededor del jugador.
/// - Mantener clic derecho y arrastrar: gira la cámara (horizontal y vertical).
/// - Rueda del ratón: zoom acercar/alejar, con límites configurables.
/// - Clic izquierdo queda libre para el ataque del jugador.
/// La orientación ya NO depende de la rotación del personaje, y la colisión
/// con muros evita que la cámara atraviese la geometría del búnker.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    public Transform objetivo;

    [Header("Órbita con el ratón")]
    public float sensibilidadMouse = 3f;
    public bool invertirEjeY = false;
    public float alturaObjetivo = 1.5f;
    public float inclinacionInicial = 25f;
    public float inclinacionMinima = -10f;
    public float inclinacionMaxima = 70f;

    [Header("Zoom con la rueda")]
    public float distancia = 5f;
    public float distanciaMinima = 2f;
    public float distanciaMaxima = 8f;
    public float sensibilidadZoom = 2f;

    [Header("Suavizado")]
    public float suavizado = 12f;

    [Header("Colisión con Muros")]
    public LayerMask capaColision;
    public float radioEsfera = 0.2f;

    private float yaw;
    private float pitch;

    void Start()
    {
        pitch = inclinacionInicial;
        yaw = transform.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        LeerEntradaRaton();

        Vector3 puntoDeMira = objetivo.position + Vector3.up * alturaObjetivo;
        Quaternion rotacion = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 posicionDeseada = puntoDeMira + rotacion * new Vector3(0f, 0f, -distancia);

        Vector3 posicionFinal = posicionDeseada;
        if (Physics.SphereCast(puntoDeMira, radioEsfera, (posicionDeseada - puntoDeMira).normalized,
            out RaycastHit hit, distancia, capaColision, QueryTriggerInteraction.Ignore))
        {
            posicionFinal = hit.point + hit.normal * radioEsfera;
        }

        transform.position = Vector3.Lerp(transform.position, posicionFinal, suavizado * Time.deltaTime);
        transform.LookAt(puntoDeMira);
    }

    private void LeerEntradaRaton()
    {
        // Clic derecho mantenido para orbitar; el izquierdo queda libre para atacar
        if (Input.GetMouseButton(1))
        {
            float deltaX = Input.GetAxis("Mouse X");
            float deltaY = Input.GetAxis("Mouse Y");

            yaw += deltaX * sensibilidadMouse;
            pitch += (invertirEjeY ? deltaY : -deltaY) * sensibilidadMouse;
            pitch = Mathf.Clamp(pitch, inclinacionMinima, inclinacionMaxima);
        }

        float rueda = Input.mouseScrollDelta.y;
        if (Mathf.Abs(rueda) > 0.01f)
        {
            distancia = Mathf.Clamp(distancia - rueda * sensibilidadZoom, distanciaMinima, distanciaMaxima);
        }
    }
}
