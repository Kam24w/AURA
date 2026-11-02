using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Ajustes de Vida")]
    public float maxHealth = 1000f;
    public float currentHealth;

    [Header("UI (Opcional)")]
    public Slider healthBarSlider;

    // Eventos para que la IA reaccione al daño y a la muerte sin acoplarse
    public System.Action<float> OnDamaged;
    public System.Action OnDied;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }
        if (GetComponent<EnemyHitFlash>() == null)
        {
            gameObject.AddComponent<EnemyHitFlash>();
        }
    }

    // Método para recibir daño
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnDamaged?.Invoke(amount);

        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        OnDied?.Invoke();

        // La IA gestiona su cadáver; el maniquí conserva su destrucción inmediata.
        if (GetComponent<EnemyController>() == null)
        {
            Debug.Log("¡El maniquí ha sido destruido!");
            Destroy(gameObject);
        }
    }
}