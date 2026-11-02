using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidad = 4.5f;
    public float velocidadRotacion = 6f;

    [Header("Salto")]
    public float fuerzaSalto = 3.5f;

    [Header("Ajustes de Combate")]
    public float duracionAtaque = 0.8f;
    [Min(0.1f)] public float intervaloCombo = 1f;
    [Range(0.5f, 0.95f)] public float momentoSiguienteGolpe = 0.7f;

    [Header("Deslizamiento")]
    public float velocidadDeslizamiento = 5f;
    [Min(0.1f)] public float duracionDeslizamiento = 0.55f;
    [Range(0.5f, 0.95f)] public float momentoSlideAttack = 0.7f;

    [Header("Referencias")]
    public Transform camaraTransform;
    public WeaponDamage arma;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocidadVertical;
    private Vector3 direccionMovimiento;
    private Vector3 direccionDeslizamiento;
    private bool estaEnSuelo;
    private bool saltoEnCurso;
    private bool accionEnCurso;
    private bool estaBloqueando;
    private bool estaMuerto;
    private string accionActual = "";
    private float tiempoAccion;
    private float duracionAccionActual;
    private bool duracionSincronizada;
    private int pasoAtaque;
    private int ataquesPendientes;
    private float ultimoAtaque = float.NegativeInfinity;
    private bool ataqueDuranteDeslizamiento;

    public bool EstaBloqueando => estaBloqueando && !estaMuerto;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (camaraTransform == null && Camera.main != null)
        {
            camaraTransform = Camera.main.transform;
        }
        if (arma == null)
        {
            arma = GetComponentInChildren<WeaponDamage>(true);
        }
    }

    private void Update()
    {
        bool tocaSuelo = controller.isGrounded;
        if (saltoEnCurso && tocaSuelo && velocidadVertical.y <= 0f)
        {
            saltoEnCurso = false;
        }

        estaEnSuelo = tocaSuelo && !saltoEnCurso;
        if (estaEnSuelo && velocidadVertical.y < 0f)
        {
            velocidadVertical.y = -2f;
        }

        if (animator != null)
        {
            animator.SetBool("Grounded", estaEnSuelo);
        }

        if (estaMuerto) return;

        LeerDireccionMovimiento();

        if (pasoAtaque > 0 && Time.time - ultimoAtaque > intervaloCombo)
        {
            ReiniciarCadenaAtaque();
        }

        if (accionEnCurso)
        {
            if (Input.GetMouseButtonDown(0))
            {
                SolicitarAtaque();
            }

            if (animator != null) animator.SetFloat("Speed", 0f);
            ActualizarAccion();
            AplicarGravedad();
            return;
        }

        if (estaBloqueando && (!Input.GetMouseButton(1) || !estaEnSuelo))
        {
            DetenerBloqueo();
        }

        if (!estaBloqueando && Input.GetMouseButton(1) && estaEnSuelo)
        {
            IniciarBloqueo();
        }

        if (!estaBloqueando)
        {
            if (Input.GetMouseButtonDown(0))
            {
                SolicitarAtaque();
            }
            else if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                IniciarDeslizamiento();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                Saltar();
            }
        }

        if (accionEnCurso)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            AplicarGravedad();
            return;
        }

        if (estaBloqueando)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            AplicarGravedad();
            return;
        }

        MoverPersonaje();
        AplicarGravedad();
    }

    private void LeerDireccionMovimiento()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 forward = camaraTransform != null ? camaraTransform.forward : Vector3.forward;
        Vector3 right = camaraTransform != null ? camaraTransform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        direccionMovimiento = (forward * vertical + right * horizontal).normalized;
    }

    private void MoverPersonaje()
    {
        if (direccionMovimiento.magnitude >= 0.1f)
        {
            float anguloObjetivo = Mathf.Atan2(direccionMovimiento.x, direccionMovimiento.z) * Mathf.Rad2Deg;
            Quaternion rotacionObjetivo = Quaternion.Euler(0f, anguloObjetivo, 0f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacionObjetivo,
                velocidadRotacion * Time.deltaTime
            );
            controller.Move(direccionMovimiento * velocidad * Time.deltaTime);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", direccionMovimiento.magnitude);
        }
    }

    private void Saltar()
    {
        if (!estaEnSuelo || estaBloqueando || accionEnCurso || estaMuerto) return;

        bool corriendo = direccionMovimiento.magnitude >= 0.1f;
        saltoEnCurso = true;
        estaEnSuelo = false;
        velocidadVertical.y = fuerzaSalto;
        ReiniciarCadenaAtaque();

        if (animator != null)
        {
            animator.SetBool("Grounded", false);
            animator.Play(corriendo ? "JumpRun" : "JumpUp", 0, 0f);
        }
    }

    private void IniciarBloqueo()
    {
        if (!estaEnSuelo || accionEnCurso || estaMuerto) return;

        estaBloqueando = true;
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("Block", true);
            animator.Play("Block", 0, 0f);
        }
    }

    private void DetenerBloqueo()
    {
        estaBloqueando = false;
        if (animator != null)
        {
            animator.SetBool("Block", false);
        }
    }

    private void IniciarDeslizamiento()
    {
        if (!estaEnSuelo || estaBloqueando || accionEnCurso || estaMuerto ||
            direccionMovimiento.magnitude < 0.1f) return;

        ReiniciarCadenaAtaque();
        ataqueDuranteDeslizamiento = false;
        direccionDeslizamiento = transform.forward;

        if (animator != null)
        {
            animator.SetTrigger("Slide");
        }
        IniciarAccion("Slide");
    }

    private void SolicitarAtaque()
    {
        if (estaMuerto || estaBloqueando) return;

        if (accionEnCurso)
        {
            if (accionActual == "Slide")
            {
                ataqueDuranteDeslizamiento = true;
            }
            else if (EsGolpeDeCadena(accionActual))
            {
                ataquesPendientes++;
                ultimoAtaque = Time.time;
            }
            return;
        }

        if (!estaEnSuelo) return;

        if (Time.time - ultimoAtaque > intervaloCombo || pasoAtaque >= 5)
        {
            ReiniciarCadenaAtaque();
        }

        pasoAtaque++;
        ultimoAtaque = Time.time;

        if (animator != null)
        {
            animator.SetInteger("AttackStep", pasoAtaque);
            animator.SetTrigger(pasoAtaque == 5 ? "Combo" : "Attack");
        }

        IniciarAccion(EstadoDelPaso(pasoAtaque));
        if (pasoAtaque == 5)
        {
            ataquesPendientes = 0;
        }
    }

    private static string EstadoDelPaso(int paso)
    {
        switch (paso)
        {
            case 1: return "Sword1";
            case 2: return "Kick1";
            case 3: return "Sword2";
            case 4: return "Kick2";
            default: return "Combo";
        }
    }

    private static bool EsGolpeDeCadena(string estado)
    {
        return estado == "Sword1" || estado == "Kick1" || estado == "Sword2" || estado == "Kick2";
    }

    private static bool EsAtaque(string estado)
    {
        return EsGolpeDeCadena(estado) || estado == "Combo" || estado == "SlideAttack";
    }

    private void IniciarAccion(string nombreEstado)
    {
        if (estaMuerto) return;

        accionEnCurso = true;
        accionActual = nombreEstado;
        tiempoAccion = 0f;
        duracionAccionActual = nombreEstado == "Slide"
            ? Mathf.Max(0.05f, duracionDeslizamiento)
            : Mathf.Max(0.05f, duracionAtaque);
        duracionSincronizada = nombreEstado == "Slide";

        LimpiarTriggersAcciones();
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.Play(nombreEstado, 0, 0f);
        }
        if (arma != null)
        {
            arma.TerminarAtaque();
            if (EsAtaque(nombreEstado))
            {
                arma.ComenzarAtaque();
            }
        }
    }

    private void LimpiarTriggersAcciones()
    {
        if (animator == null) return;

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Combo");
        animator.ResetTrigger("Slide");
        animator.ResetTrigger("SlideAttack");
        animator.ResetTrigger("Hit");
    }

    private void ActualizarAccion()
    {
        tiempoAccion += Time.deltaTime;

        if (animator != null && !duracionSincronizada)
        {
            AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(0);
            if (estado.IsName(accionActual))
            {
                duracionAccionActual = Mathf.Max(0.05f, estado.length / Mathf.Max(0.01f, Mathf.Abs(estado.speed)));
                duracionSincronizada = true;
            }
        }

        if (accionActual == "Slide")
        {
            controller.Move(direccionDeslizamiento * velocidadDeslizamiento * Time.deltaTime);
            if (ataqueDuranteDeslizamiento && tiempoAccion >= duracionAccionActual * momentoSlideAttack)
            {
                ataqueDuranteDeslizamiento = false;
                if (animator != null) animator.SetTrigger("SlideAttack");
                IniciarAccion("SlideAttack");
                return;
            }
        }

        if (EsGolpeDeCadena(accionActual) && ataquesPendientes > 0 &&
            Time.time - ultimoAtaque <= intervaloCombo &&
            tiempoAccion >= duracionAccionActual * momentoSiguienteGolpe)
        {
            ataquesPendientes--;
            accionEnCurso = false;
            accionActual = "";
            SolicitarAtaque();
            return;
        }

        if (tiempoAccion < duracionAccionActual) return;

        string accionTerminada = accionActual;
        accionEnCurso = false;
        accionActual = "";
        ataquesPendientes = 0;
        if (arma != null) arma.TerminarAtaque();

        if (accionTerminada == "Combo")
        {
            ReiniciarCadenaAtaque();
        }
        else if (accionTerminada == "Slide")
        {
            ReproducirLocomocion();
        }
    }

    private void ReproducirLocomocion()
    {
        if (animator == null) return;

        string estado = direccionMovimiento.magnitude >= 0.1f ? "Running" : "Idle";
        animator.CrossFade(estado, 0.1f, 0, 0f);
    }

    private void ReiniciarCadenaAtaque()
    {
        pasoAtaque = 0;
        ataquesPendientes = 0;
        if (animator != null)
        {
            animator.SetInteger("AttackStep", 0);
        }
    }

    public void RecibirGolpe()
    {
        if (estaMuerto || estaBloqueando) return;

        DetenerBloqueo();
        ReiniciarCadenaAtaque();
        ataqueDuranteDeslizamiento = false;

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
        IniciarAccion("HIT");
    }

    public void Morir()
    {
        if (estaMuerto) return;

        estaMuerto = true;
        accionEnCurso = false;
        accionActual = "";
        ataqueDuranteDeslizamiento = false;
        ReiniciarCadenaAtaque();
        DetenerBloqueo();
        LimpiarTriggersAcciones();
        if (arma != null) arma.TerminarAtaque();

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("Dead", true);
            animator.Play("DEAD", 0, 0f);
        }
    }

    private void AplicarGravedad()
    {
        velocidadVertical.y += -9.81f * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }
}
