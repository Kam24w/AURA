using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    public float damageAmount = 20f;
    public float attackCooldown = 0.5f; // Tiempo mínimo entre golpes
    private float nextAttackTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica que haya pasado el tiempo de espera
        if (Time.time >= nextAttackTime)
        {
            EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                enemy.TakeDamage(damageAmount);
                nextAttackTime = Time.time + attackCooldown; // Reinicia el temporizador
            }
        }
    }
}