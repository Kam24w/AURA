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

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }
    }

    // Método para recibir daño
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        OnDamaged?.Invoke(amount);

        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Si la IA está suscrita, ella gestiona la muerte y destruye el cuerpo
        // tras unos segundos; si no, comportamiento clásico (maniquí de prueba)
        if (OnDied != null)
        {
            OnDied.Invoke();
        }
        else
        {
            Debug.Log("¡El maniquí ha sido destruido!");
            Destroy(gameObject);
        }
    }
}