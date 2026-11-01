/// <summary>
/// Contrato base del Patrón State para la IA de enemigos en AURA.
/// Cada estado concreto (Patrullar, Perseguir, Atacar...) implementa esta interfaz.
/// </summary>
public interface IEnemyState
{
    // Se llama una vez al entrar al estado
    void Enter();

    // Se llama cada frame mientras el estado esté activo
    void UpdateState();

    // Se llama una vez al salir del estado
    void Exit();
}
