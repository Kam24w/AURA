using UnityEngine;

/// <summary>
/// Estado Melee: pisotón (Attack_Stomp) con cooldown cuando el jugador está
/// muy cerca. El enemigo permanece detenido durante el ataque y el daño se
/// aplica en el impacto (Animation Event o tiempo de respaldo).
/// </summary>
public class EstadoAtacar : IEnemyState
{
    private const float MargenSalida = 1.25f;

    private readonly EnemyController enemigo;

    public EstadoAtacar(EnemyController enemigo)
    {
        this.enemigo = enemigo;
    }

    public void Enter()
    {
        enemigo.DetenerMovimiento();
    }

    public void UpdateState()
    {
        if (enemigo.objetivo == null)
        {
            enemigo.CambiarEstado(enemigo.EstadoPatrullar);
            return;
        }

        if (!enemigo.ObjetivoEnRango(enemigo.rangoMelee * MargenSalida))
        {
            enemigo.CambiarEstado(enemigo.EstadoPerseguir);
            return;
        }

        enemigo.DetenerMovimiento();
        enemigo.MirarHacia(enemigo.objetivo.position);

        if (enemigo.PuedeAtacar)
        {
            enemigo.IniciarMelee();
        }
    }

    public void Exit()
    {
    }
}
