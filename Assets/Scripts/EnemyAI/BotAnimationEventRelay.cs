using UnityEngine;

/// <summary>
/// Va en el objeto que tiene el Animator (normalmente el modelo hijo).
/// Reenvía los Animation Events de los clips al EnemyController de la raíz.
/// Configura en el FBX: clip Shoot -> AnimEvent_Shoot (fotograma 4-5),
/// clip Attack_Stomp -> AnimEvent_Stomp (fotograma 20).
/// </summary>
public class BotAnimationEventRelay : MonoBehaviour
{
    private EnemyController enemigo;

    private void Awake()
    {
        enemigo = GetComponentInParent<EnemyController>();
    }

    public void AnimEvent_Shoot()
    {
        if (enemigo != null)
        {
            enemigo.AnimEvent_Shoot();
        }
    }

    public void AnimEvent_Stomp()
    {
        if (enemigo != null)
        {
            enemigo.AnimEvent_Stomp();
        }
    }
}
