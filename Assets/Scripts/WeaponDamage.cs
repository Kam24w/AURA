using System.Collections.Generic;
using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    public float damageAmount = 20f;
    public float attackCooldown = 0.5f;

    private bool ataqueActivo;
    private readonly HashSet<EnemyHealth> enemigosGolpeados = new HashSet<EnemyHealth>();
    private readonly Dictionary<EnemyHealth, float> proximoGolpePorEnemigo = new Dictionary<EnemyHealth, float>();

    private void Awake()
    {
        Rigidbody cuerpo = GetComponent<Rigidbody>();
        if (cuerpo == null)
        {
            cuerpo = gameObject.AddComponent<Rigidbody>();
        }

        cuerpo.isKinematic = true;
        cuerpo.useGravity = false;
    }

    public void ComenzarAtaque()
    {
        enemigosGolpeados.Clear();
        ataqueActivo = true;
    }

    public void TerminarAtaque()
    {
        ataqueActivo = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        IntentarDanar(other);
    }

    private void OnTriggerStay(Collider other)
    {
        IntentarDanar(other);
    }

    private void IntentarDanar(Collider other)
    {
        if (!ataqueActivo) return;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || enemy.IsDead || enemigosGolpeados.Contains(enemy)) return;
        if (proximoGolpePorEnemigo.TryGetValue(enemy, out float proximoGolpe) && Time.time < proximoGolpe) return;

        enemy.TakeDamage(damageAmount);
        enemigosGolpeados.Add(enemy);
        proximoGolpePorEnemigo[enemy] = Time.time + attackCooldown;
    }

    private void OnDisable()
    {
        TerminarAtaque();
    }
}