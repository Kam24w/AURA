/// <summary>
/// Estado de ataque del Boss: delega el cuerpo a cuerpo en EstadoAtacar y,
/// cuando el especial está listo y el jugador está a la vista, ejecuta el
/// patrón de ataque especial correspondiente al piso actual.
/// </summary>
public class EstadoAtaqueBoss : IEnemyState
{
    private readonly BossController boss;
    private readonly EstadoAtacar ataqueBasico;

    public EstadoAtaqueBoss(BossController boss)
    {
        this.boss = boss;
        ataqueBasico = new EstadoAtacar(boss);
    }

    public void Enter()
    {
        ataqueBasico.Enter();
    }

    public void UpdateState()
    {
        // Mientras el especial está en curso, la corrutina del Boss lleva el control
        if (boss.EjecutandoEspecial)
        {
            return;
        }

        if (boss.EspecialListo
            && boss.ObjetivoEnRango(boss.rangoEspecial)
            && boss.TieneLineaVision())
        {
            boss.DetenerMovimiento();
            boss.MirarHacia(boss.objetivo.position);
            boss.EjecutarEspecial();
            return;
        }

        ataqueBasico.UpdateState();
    }

    public void Exit()
    {
        ataqueBasico.Exit();
    }
}
