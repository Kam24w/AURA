using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Ajustes de Vida")]
    public float maxHealth = 1000f;
    public float currentHealth;

    [Header("UI (Opcional)")]
    public Slider healthBarSlider;

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
        Debug.Log("¡Golpe al maniquí! Vida restante: " + currentHealth);

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
        Debug.Log("¡El maniquí ha sido destruido!");
        Destroy(gameObject); // Elimina el cubo de la escena
    }
}