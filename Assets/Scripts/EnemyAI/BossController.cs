using System.Collections;
using UnityEngine;

/// <summary>
/// Controlador del Boss del búnker. Hereda toda la máquina de estados del
/// enemigo común (NavMeshAgent incluido) y sustituye el estado melee por
/// EstadoAtaqueBoss, que añade un ataque especial según el piso:
///   Piso 1 -> Embestida (carga en línea recta)
///   Piso 2 -> Golpe de área (onda de daño alrededor del Boss)
///   Piso 3 -> Ráfaga de proyectiles en abanico
/// </summary>
public class BossController : EnemyController
{
    [Header("Boss - Piso")]
    [Range(1, 3)] public int pisoActual = 1;

    [Header("Boss - Especial")]
    public float rangoEspecial = 9f;
    public float tiempoEntreEspeciales = 5f;
    public float tiempoPreparacion = 0.5f;

    [Header("Boss - Embestida (Piso 1)")]
    public float velocidadEmbestida = 10f;
    public float duracionEmbestida = 0.6f;
    public float danoEmbestida = 30f;

    [Header("Boss - Golpe de Área (Piso 2)")]
    public float radioArea = 4f;
    public float danoArea = 25f;

    [Header("Boss - Proyectiles (Piso 3)")]
    public GameObject prefabProyectilBoss;
    public Transform puntoDisparo;
    public int cantidadProyectiles = 5;
    public float dispersionProyectiles = 30f;
    public float velocidadProyectilBoss = 12f;

    public bool EjecutandoEspecial { get; private set; }
    public bool EspecialListo => Time.time >= proximoEspecial;

    private float proximoEspecial;

    protected override IEnemyState CrearEstadoAtacar()
    {
        return new EstadoAtaqueBoss(this);
    }

    // Punto de entrada llamado por EstadoAtaqueBoss; elige el patrón según el piso
    public void EjecutarEspecial()
    {
        proximoEspecial = Time.time + tiempoEntreEspeciales;

        switch (pisoActual)
        {
            case 1:
                StartCoroutine(RutinaEmbestida());
                break;
            case 2:
                StartCoroutine(RutinaGolpeArea());
                break;
            default:
                StartCoroutine(RutinaRafagaProyectiles());
                break;
        }
    }

    private IEnumerator RutinaEmbestida()
    {
        EjecutandoEspecial = true;
        DetenerMovimiento();
        ReproducirAtaqueEspecial();

        yield return new WaitForSeconds(tiempoPreparacion);

        Vector3 direccion = objetivo != null ? objetivo.position - transform.position : transform.forward;
        direccion.y = 0f;
        direccion = direccion.sqrMagnitude > 0.001f ? direccion.normalized : transform.forward;
        transform.rotation = Quaternion.LookRotation(direccion);

        float fin = Time.time + duracionEmbestida;
        bool golpeo = false;

        while (Time.time < fin && !EstaMuerto)
        {
            // Agente.Move desplaza respetando el NavMesh: no atraviesa paredes ni bordes
            Agente.Move(direccion * velocidadEmbestida * Time.deltaTime);

            if (!golpeo && ObjetivoEnRango(rangoMelee + 0.75f))
            {
                objetivo.SendMessage("TakeDamage", danoEmbestida, SendMessageOptions.DontRequireReceiver);
                golpeo = true;
            }

            yield return null;
        }

        ReanudarMovimiento();
        EjecutandoEspecial = false;
    }

    private IEnumerator RutinaGolpeArea()
    {
        EjecutandoEspecial = true;
        DetenerMovimiento();
        ReproducirAtaqueEspecial();

        yield return new WaitForSeconds(tiempoPreparacion);

        if (ObjetivoEnRango(radioArea))
        {
            objetivo.SendMessage("TakeDamage", danoArea, SendMessageOptions.DontRequireReceiver);
        }

        ReanudarMovimiento();
        EjecutandoEspecial = false;
    }

    private IEnumerator RutinaRafagaProyectiles()
    {
        EjecutandoEspecial = true;
        DetenerMovimiento();
        ReproducirAtaqueEspecial();

        yield return new WaitForSeconds(tiempoPreparacion);

        if (prefabProyectilBoss == null)
        {
            Debug.LogWarning("AURA: el Boss del piso 3 no tiene prefabProyectilBoss asignado.", this);
        }
        else if (objetivo != null)
        {
            Vector3 origen = puntoDisparo != null
                ? puntoDisparo.position
                : transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;

            Vector3 direccionBase = objetivo.position + Vector3.up - origen;
            direccionBase.Normalize();

            int cantidad = Mathf.Max(1, cantidadProyectiles);
            for (int i = 0; i < cantidad; i++)
            {
                float t = cantidad == 1 ? 0.5f : (float)i / (cantidad - 1);
                float angulo = Mathf.Lerp(-dispersionProyectiles * 0.5f, dispersionProyectiles * 0.5f, t);
                Vector3 direccion = Quaternion.AngleAxis(angulo, Vector3.up) * direccionBase;

                GameObject proyectil = Instantiate(prefabProyectilBoss, origen, Quaternion.LookRotation(direccion));

                if (proyectil.TryGetComponent(out Rigidbody cuerpo))
                {
                    cuerpo.linearVelocity = direccion * velocidadProyectilBoss;
                }
            }
        }

        ReanudarMovimiento();
        EjecutandoEspecial = false;
    }

    private void ReproducirAtaqueEspecial()
    {
        ReproducirAtaqueMelee();
    }
}
