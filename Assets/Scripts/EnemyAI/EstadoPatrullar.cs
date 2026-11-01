using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Estado Patrullar: espera un tiempo variable en Idle o camina entre puntos.
/// Si no hay puntos de patrulla asignados, elige destinos cercanos validados
/// con NavMesh.SamplePosition. Transiciona a Perseguir al detectar al jugador.
/// </summary>
public class EstadoPatrullar : IEnemyState
{
    private readonly EnemyController enemigo;
    private Vector3 origen;
    private int indicePunto;
    private float finEspera;
    private bool esperando;

    public EstadoPatrullar(EnemyController enemigo)
    {
        this.enemigo = enemigo;
    }

    public void Enter()
    {
        origen = enemigo.transform.position;
        indicePunto = 0;
        EmpezarEspera();
    }

    public void UpdateState()
    {
        if (enemigo.DetectaJugador())
        {
            enemigo.CambiarEstado(enemigo.EstadoPerseguir);
            return;
        }

        if (esperando)
        {
            enemigo.DetenerMovimiento();
            if (Time.time >= finEspera)
            {
                esperando = IrAlSiguientePunto();
            }
            return;
        }

        if (!enemigo.Agente.pathPending
            && enemigo.Agente.remainingDistance <= enemigo.stoppingDistance)
        {
            EmpezarEspera();
        }
    }

    public void Exit()
    {
    }

    private void EmpezarEspera()
    {
        esperando = true;
        finEspera = Time.time + Random.Range(enemigo.esperaMinPatrulla, enemigo.esperaMaxPatrulla);
        enemigo.DetenerMovimiento();
    }

    // Devuelve true si encontró un destino válido; si no, sigue esperando
    private bool IrAlSiguientePunto()
    {
        if (enemigo.puntosPatrulla != null && enemigo.puntosPatrulla.Length > 0)
        {
            Transform punto = enemigo.puntosPatrulla[indicePunto];
            indicePunto = (indicePunto + 1) % enemigo.puntosPatrulla.Length;

            if (punto != null)
            {
                enemigo.MoverHacia(punto.position, enemigo.velocidadPatrulla);
                return true;
            }

            return false;
        }

        Vector2 aleatorio = Random.insideUnitCircle * enemigo.radioPatrulla;
        Vector3 candidato = origen + new Vector3(aleatorio.x, 0f, aleatorio.y);

        if (NavMesh.SamplePosition(candidato, out NavMeshHit impacto, 3f, NavMesh.AllAreas))
        {
            enemigo.MoverHacia(impacto.position, enemigo.velocidadPatrulla);
            return true;
        }

        return false;
    }
}
