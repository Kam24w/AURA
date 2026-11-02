using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador de la máquina de estados del enemigo (Patrón State).
/// Movimiento con NavMeshAgent; las animaciones son en el sitio y el parámetro
/// Speed se alimenta de la velocidad real del agente. El Boss hereda de aquí.
/// Daño de disparo/melee sincronizado con Animation Events (AnimEvent_Shoot /
/// AnimEvent_Stomp) o, si el clip no los tiene, con un tiempo de respaldo.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform objetivo;
    public Animator animator;

    [Header("Detección")]
    public float rangoDeteccion = 14f;
    public float rangoPerdida = 20f;
    public float campoVision = 120f;
    public float tiempoPerdida = 3f;
    public LayerMask capasObstaculos = 1;

    [Header("Movimiento")]
    public float velocidadPatrulla = 2f;
    public float velocidadPersecucion = 4.5f;
    public float velocidadRotacion = 8f;
    public float stoppingDistance = 1.2f;
    public Transform[] puntosPatrulla;

    [Header("Patrulla")]
    public float esperaMinPatrulla = 1f;
    public float esperaMaxPatrulla = 3f;
    public float radioPatrulla = 6f;

    [Header("Disparo")]
    public float rangoDisparo = 10f;
    public float danoDisparo = 10f;
    public float cooldownDisparo = 1.5f;
    public float tiempoImpactoDisparo = 0.15f;
    public GameObject prefabProyectil;
    public float velocidadProyectil = 14f;
    public Transform muzzleL;
    public Transform muzzleR;
    public Vector3 offsetMuzzleL = new Vector3(-0.35f, 1.2f, 0.6f);
    public Vector3 offsetMuzzleR = new Vector3(0.35f, 1.2f, 0.6f);

    [Header("Melee")]
    public float rangoMelee = 2.2f;
    public float radioMelee = 2.5f;
    public float danoMelee = 20f;
    public float cooldownMelee = 1.5f;
    public float tiempoImpactoMelee = 0.66f;

    [Header("Muerte")]
    public float tiempoCadaver = 4f;

    public IEnemyState EstadoActual { get; private set; }
    public IEnemyState EstadoPatrullar { get; private set; }
    public IEnemyState EstadoPerseguir { get; private set; }
    public IEnemyState EstadoDisparo { get; private set; }
    public IEnemyState EstadoAtacar { get; private set; }
    public NavMeshAgent Agente { get; private set; }
    public bool EstaMuerto { get; private set; }
    public bool PuedeDisparar => Time.time >= proximoDisparo;
    public bool PuedeAtacar => Time.time >= proximoAtaque;

    private EnemyHealth salud;
    private float proximoDisparo;
    private float proximoAtaque;
    private bool disparoAplicado;
    private bool meleeAplicado;
    private bool usarMuzzleL = true;
    private float proximaComprobacionNavMesh;
    private bool avisoNavMeshMostrado;

    protected virtual void Awake()
    {
        Agente = GetComponent<NavMeshAgent>();
        Agente.stoppingDistance = stoppingDistance;
        Agente.angularSpeed = velocidadRotacion * 45f;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        salud = GetComponent<EnemyHealth>();
        if (salud != null)
        {
            salud.OnDamaged += AlRecibirDano;
            salud.OnDied += Morir;
        }

        CrearMuzzlesSiFaltan();

        EstadoPatrullar = CrearEstadoPatrullar();
        EstadoPerseguir = CrearEstadoPerseguir();
        EstadoDisparo = CrearEstadoDisparo();
        EstadoAtacar = CrearEstadoAtacar();
    }

    protected virtual void Start()
    {
        if (objetivo == null)
        {
            PlayerController jugador = FindFirstObjectByType<PlayerController>();
            if (jugador != null)
            {
                objetivo = jugador.transform;
            }
        }

        CambiarEstado(EstadoPatrullar);
    }

    protected virtual void Update()
    {
        if (EstaMuerto)
        {
            return;
        }

        // Si el agente se activó antes de que existiera el NavMesh (p. ej. bots
        // colocados a mano), lo reconecta en cuanto el NavMesh esté disponible
        if (Agente.enabled && !Agente.isOnNavMesh)
        {
            IntentarConectarNavMesh();
        }

        EstadoActual?.UpdateState();

        if (animator != null)
        {
            float velocidad = Agente.enabled ? Agente.velocity.magnitude : 0f;
            animator.SetFloat("Speed", velocidad, 0.15f, Time.deltaTime);
        }
    }

    private void IntentarConectarNavMesh()
    {
        if (Time.time < proximaComprobacionNavMesh)
        {
            return;
        }
        proximaComprobacionNavMesh = Time.time + 0.5f;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit impacto, 5f, NavMesh.AllAreas))
        {
            Agente.Warp(impacto.position);
        }
        else if (!avisoNavMeshMostrado)
        {
            avisoNavMeshMostrado = true;
            Debug.LogWarning($"AURA: {name} no tiene NavMesh cerca y no podrá moverse. " +
                "El NavMesh se hornea al generar el búnker: coloca al enemigo dentro del búnker " +
                "o usa el spawn automático (prefab en BunkerSettings).", this);
        }
    }

    private void OnDestroy()
    {
        if (salud != null)
        {
            salud.OnDamaged -= AlRecibirDano;
            salud.OnDied -= Morir;
        }
    }

    public void CambiarEstado(IEnemyState nuevoEstado)
    {
        if (nuevoEstado == null || nuevoEstado == EstadoActual || EstaMuerto)
        {
            return;
        }

        EstadoActual?.Exit();
        EstadoActual = nuevoEstado;
        EstadoActual.Enter();
    }

    protected virtual IEnemyState CrearEstadoPatrullar() => new EstadoPatrullar(this);
    protected virtual IEnemyState CrearEstadoPerseguir() => new EstadoPerseguir(this);
    protected virtual IEnemyState CrearEstadoDisparo() => new EstadoDisparo(this);
    protected virtual IEnemyState CrearEstadoAtacar() => new EstadoAtacar(this);

    public float DistanciaAlObjetivo()
    {
        if (objetivo == null)
        {
            return float.MaxValue;
        }

        Vector3 delta = objetivo.position - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    public bool ObjetivoEnRango(float rango)
    {
        return DistanciaAlObjetivo() <= rango;
    }

    // Detección completa: distancia + campo de visión + línea de visión
    public bool DetectaJugador()
    {
        if (objetivo == null || !ObjetivoEnRango(rangoDeteccion))
        {
            return false;
        }

        Vector3 direccion = objetivo.position - transform.position;
        direccion.y = 0f;
        if (Vector3.Angle(transform.forward, direccion) > campoVision * 0.5f)
        {
            return false;
        }

        return TieneLineaVision();
    }

    // Línea de visión hasta el jugador; sin capas de obstáculos asignadas se asume visible
    public bool TieneLineaVision()
    {
        if (objetivo == null)
        {
            return false;
        }

        if (capasObstaculos.value == 0)
        {
            return true;
        }

        Vector3 origen = transform.position + Vector3.up * 1.5f;
        Vector3 destino = objetivo.position + Vector3.up * 1f;
        Vector3 direccion = destino - origen;

        if (!Physics.Raycast(origen, direccion.normalized, out RaycastHit impacto,
            direccion.magnitude, capasObstaculos, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return impacto.transform == objetivo || impacto.transform.IsChildOf(objetivo);
    }

    public void MoverHacia(Vector3 destino, float velocidad)
    {
        if (!Agente.enabled || !Agente.isOnNavMesh)
        {
            return;
        }

        Agente.updateRotation = true;
        Agente.isStopped = false;
        Agente.speed = velocidad;
        Agente.SetDestination(destino);
    }

    public void DetenerMovimiento()
    {
        if (!Agente.enabled)
        {
            return;
        }

        Agente.isStopped = true;
        Agente.updateRotation = false;
    }

    public void ReanudarMovimiento()
    {
        if (EstaMuerto || !Agente.enabled)
        {
            return;
        }

        Agente.isStopped = false;
        Agente.updateRotation = true;
    }

    public void MirarHacia(Vector3 destino)
    {
        Vector3 direccion = destino - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo,
            velocidadRotacion * Time.deltaTime);
    }

    public void IniciarDisparo()
    {
        proximoDisparo = Time.time + cooldownDisparo;
        disparoAplicado = false;

        if (animator != null)
        {
            animator.Play("BotRig|Shoot", 0, 0f);
        }

        // Respaldo por si el clip no tiene el Animation Event configurado
        StartCoroutine(RespaldoImpacto(tiempoImpactoDisparo, true));
    }

    public void IniciarMelee()
    {
        proximoAtaque = Time.time + cooldownMelee;
        meleeAplicado = false;
        ReproducirAtaqueMelee();

        StartCoroutine(RespaldoImpacto(tiempoImpactoMelee, false));
    }

    public void ReproducirAtaqueMelee()
    {
        if (animator != null)
        {
            animator.Play("BotRig|Attack_Stomp", 0, 0f);
        }
    }

    // Llamados desde los Animation Events del clip (directos o vía BotAnimationEventRelay)
    public void AnimEvent_Shoot() => EjecutarDisparo();
    public void AnimEvent_Stomp() => EjecutarMelee();

    private IEnumerator RespaldoImpacto(float espera, bool esDisparo)
    {
        yield return new WaitForSeconds(espera);

        if (esDisparo)
        {
            EjecutarDisparo();
        }
        else
        {
            EjecutarMelee();
        }
    }

    private void EjecutarDisparo()
    {
        // La bandera evita daño doble si coinciden el Animation Event y el respaldo
        if (disparoAplicado || EstaMuerto || objetivo == null)
        {
            return;
        }
        disparoAplicado = true;

        Transform muzzle = usarMuzzleL ? muzzleL : muzzleR;
        usarMuzzleL = !usarMuzzleL;
        Vector3 origen = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.2f;
        Vector3 direccion = (objetivo.position + Vector3.up * 0.9f - origen).normalized;

        if (prefabProyectil != null)
        {
            GameObject proyectil = Instantiate(prefabProyectil, origen, Quaternion.LookRotation(direccion));
            if (proyectil.TryGetComponent(out Rigidbody cuerpo))
            {
                cuerpo.linearVelocity = direccion * velocidadProyectil;
            }
        }
        else if (TieneLineaVision())
        {
            objetivo.SendMessage("TakeDamage", danoDisparo, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void EjecutarMelee()
    {
        if (meleeAplicado || EstaMuerto)
        {
            return;
        }
        meleeAplicado = true;

        if (objetivo != null && ObjetivoEnRango(radioMelee))
        {
            objetivo.SendMessage("TakeDamage", danoMelee, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void AlRecibirDano(float cantidad)
    {
        if (!EstaMuerto && animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void Morir()
    {
        if (EstaMuerto)
        {
            return;
        }
        EstaMuerto = true;

        StopAllCoroutines();

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Shoot");
            animator.SetBool("Dead", true);
        }

        if (Agente.enabled)
        {
            Agente.isStopped = true;
            Agente.enabled = false;
        }

        foreach (Collider colision in GetComponents<Collider>())
        {
            colision.enabled = false;
        }

        enabled = false;
        Destroy(gameObject, tiempoCadaver);
    }

    private void CrearMuzzlesSiFaltan()
    {
        if (muzzleL == null)
        {
            muzzleL = CrearMuzzle("Muzzle_L", offsetMuzzleL);
        }
        if (muzzleR == null)
        {
            muzzleR = CrearMuzzle("Muzzle_R", offsetMuzzleR);
        }
    }

    private Transform CrearMuzzle(string nombre, Vector3 offset)
    {
        Transform padre = animator != null ? animator.transform : transform;
        GameObject muzzle = new GameObject(nombre);
        muzzle.transform.SetParent(padre, false);
        muzzle.transform.localPosition = offset;
        return muzzle.transform;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangoDisparo);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoMelee);
    }
}
