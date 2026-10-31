using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidad = 4.5f;          // Bajamos de 6f a 4.5f para que no sea tan rápido
    public float velocidadRotacion = 6f;    // Bajamos de 10f a 6f para que la vuelta sea suave

    [Header("Ajustes de Ataque")]
    public float duracionAtaque = 0.8f;      // Tiempo en segundos que dura tu animación de ataque

    [Header("Referencias")]
    public Transform camaraTransform;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocidadVertical;
    private bool estaEnSuelo;
    private bool estaAtacando = false;       // Bandera para bloquear movimiento

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (camaraTransform == null && Camera.main != null)
        {
            camaraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // 1. GRAVEDAD Y SUELO (Siempre debe ejecutarse para que no flote)
        estaEnSuelo = controller.isGrounded;
        if (estaEnSuelo && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
        }

        // 2. SISTEMA DE ATAQUE (Prevalece sobre el movimiento)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
        {
            if (!estaAtacando)
            {
                EjecutarAtaque();
            }
        }

        // 3. SI ESTÁ ATACANDO, NO LEEMOS TECLADO NI MOVEMOS EL CUERPO
        if (estaAtacando)
        {
            // Mantenemos la velocidad en el Animator en 0 para que no haga animación de correr
            if (animator != null) animator.SetFloat("Speed", 0f);

            // Aplicar gravedad constante para que no se quede flotando si ataca en el aire
            velocidadVertical.y += -9.81f * Time.deltaTime;
            controller.Move(velocidadVertical * Time.deltaTime);
            return; // Cortamos el Update aquí para no procesar el movimiento de abajo
        }

        // 4. LÓGICA DE MOVIMIENTO RELATIVO A LA CÁMARA
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = camaraTransform.forward;
        Vector3 right = camaraTransform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direccion = (forward * vertical + right * horizontal).normalized;

        if (direccion.magnitude >= 0.1f)
        {
            // Rotación suave hacia la dirección de la cámara
            float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;
            Quaternion rotacionObjetivo = Quaternion.Euler(0f, anguloObjetivo, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacionObjetivo,
                velocidadRotacion * Time.deltaTime
            );

            // Movimiento horizontal
            controller.Move(direccion * velocidad * Time.deltaTime);
        }

        // 5. ANIMACIÓN DE CAMINAR / CORRER
        if (animator != null)
        {
            animator.SetFloat("Speed", direccion.magnitude);
        }

        // 6. APLICAR GRAVEDAD
        velocidadVertical.y += -9.81f * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    void EjecutarAtaque()
    {
        estaAtacando = true;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Desbloquea el movimiento automáticamente después del tiempo del ataque
        CancelInvoke(nameof(ResetearAtaque));
        Invoke(nameof(ResetearAtaque), duracionAtaque);
    }

    public void ResetearAtaque()
    {
        estaAtacando = false;
    }
}