using UnityEngine;

/// <summary>
/// Estado Disparo: se detiene en alcance con línea de visión, gira suavemente
/// hacia el jugador y dispara con cooldown. El daño sale en el momento del
/// disparo (Animation Event o tiempo de respaldo), no al iniciar la animación.
/// </summary>
public class EstadoDisparo : IEnemyState
{
    private readonly EnemyController enemigo;

    public EstadoDisparo(EnemyController enemigo)
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

        if (enemigo.ObjetivoEnRango(enemigo.rangoMelee) && enemigo.TieneLineaVision())
        {
            enemigo.CambiarEstado(enemigo.EstadoAtacar);
            return;
        }

        if (!enemigo.ObjetivoEnRango(enemigo.rangoDisparo) || !enemigo.TieneLineaVision())
        {
            enemigo.CambiarEstado(enemigo.EstadoPerseguir);
            return;
        }

        enemigo.DetenerMovimiento();
        enemigo.MirarHacia(enemigo.objetivo.position);

        if (enemigo.PuedeDisparar)
        {
            enemigo.IniciarDisparo();
        }
    }

    public void Exit()
    {
    }
}
