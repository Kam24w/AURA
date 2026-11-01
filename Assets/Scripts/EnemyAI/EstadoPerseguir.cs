using UnityEngine;

/// <summary>
/// Estado Perseguir: corre hacia el jugador mientras lo ve. Sin línea de visión
/// va a la última posición conocida (nunca a la posición real a través de
/// paredes) y vuelve a Patrullar tras el tiempo de pérdida configurado.
/// Entra en Disparo o Melee según la distancia.
/// </summary>
public class EstadoPerseguir : IEnemyState
{
    private readonly EnemyController enemigo;
    private Vector3 ultimaPosicionConocida;
    private float tiempoSinVer;

    public EstadoPerseguir(EnemyController enemigo)
    {
        this.enemigo = enemigo;
    }

    public void Enter()
    {
        tiempoSinVer = 0f;
        if (enemigo.objetivo != null)
        {
            ultimaPosicionConocida = enemigo.objetivo.position;
        }
    }

    public void UpdateState()
    {
        if (enemigo.objetivo == null)
        {
            enemigo.CambiarEstado(enemigo.EstadoPatrullar);
            return;
        }

        if (enemigo.TieneLineaVision() && enemigo.ObjetivoEnRango(enemigo.rangoPerdida))
        {
            ultimaPosicionConocida = enemigo.objetivo.position;
            tiempoSinVer = 0f;

            if (enemigo.ObjetivoEnRango(enemigo.rangoMelee))
            {
                enemigo.CambiarEstado(enemigo.EstadoAtacar);
                return;
            }

            if (enemigo.ObjetivoEnRango(enemigo.rangoDisparo))
            {
                enemigo.CambiarEstado(enemigo.EstadoDisparo);
                return;
            }
        }
        else
        {
            tiempoSinVer += Time.deltaTime;
            if (tiempoSinVer >= enemigo.tiempoPerdida)
            {
                enemigo.CambiarEstado(enemigo.EstadoPatrullar);
                return;
            }
        }

        enemigo.MoverHacia(ultimaPosicionConocida, enemigo.velocidadPersecucion);
    }

    public void Exit()
    {
    }
}
