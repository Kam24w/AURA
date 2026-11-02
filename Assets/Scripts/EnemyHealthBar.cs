using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Visibilidad")]
    [Min(0f)] public float distanciaVisible = 12f;
    [Min(0f)] public float distanciaVisibleBoss = 20f;
    [Min(0f)] public float tiempoVisibleTrasDano = 3f;

    [Header("Dimensiones y posición")]
    public Vector2 tamañoBot = new Vector2(1.2f, 0.14f);
    public Vector2 tamañoBoss = new Vector2(2.4f, 0.2f);
    public float alturaBot = 2.4f;
    public float alturaBoss = 3f;

    [Header("Colores")]
    public Color colorBot = new Color(0.2f, 0.9f, 0.25f, 1f);
    public Color colorBoss = new Color(0.9f, 0.08f, 0.08f, 1f);

    private EnemyHealth health;
    private GameObject barObject;
    private Transform cameraTransform;
    private float visibleUntil;
    private bool isBoss;

    private void Start()
    {
        health = GetComponent<EnemyHealth>();
        isBoss = GetComponent<BossController>() != null;

        if (health.healthBarSlider == null)
        {
            health.healthBarSlider = CrearBarraFlotante();
        }

        barObject = health.healthBarSlider.gameObject;
        ActualizarBarra();
        barObject.SetActive(false);

        health.OnDamaged += AlRecibirDano;
        health.OnDied += Ocultar;
    }

    private void LateUpdate()
    {
        if (health == null || barObject == null)
        {
            return;
        }

        if (health.IsDead)
        {
            barObject.SetActive(false);
            return;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        float distancia = isBoss ? distanciaVisibleBoss : distanciaVisible;
        bool estaCerca = cameraTransform != null
            && (cameraTransform.position - transform.position).sqrMagnitude <= distancia * distancia;
        bool visible = estaCerca || Time.time < visibleUntil;

        if (barObject.activeSelf != visible)
        {
            barObject.SetActive(visible);
        }
    }

    private Slider CrearBarraFlotante()
    {
        Vector2 tamaño = isBoss ? tamañoBoss : tamañoBot;
        float altura = isBoss ? alturaBoss : alturaBot;
        Color color = isBoss ? colorBoss : colorBot;

        GameObject canvasObject = new GameObject("EnemyHealthCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = Vector3.up * altura;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        ((RectTransform)canvas.transform).sizeDelta = tamaño;
        canvasObject.AddComponent<Billboard>();

        Slider slider = HealthBarFactory.Create(canvas.transform, "HealthBar", tamaño, color);
        ((RectTransform)slider.transform).anchoredPosition = Vector2.zero;
        return slider;
    }

    private void AlRecibirDano(float amount)
    {
        visibleUntil = Time.time + tiempoVisibleTrasDano;
        ActualizarBarra();

        if (barObject != null)
        {
            barObject.SetActive(true);
        }
    }

    private void ActualizarBarra()
    {
        Slider slider = health.healthBarSlider;
        slider.minValue = 0f;
        slider.maxValue = health.maxHealth;
        slider.value = health.currentHealth;
    }

    private void Ocultar()
    {
        if (barObject != null)
        {
            barObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= AlRecibirDano;
            health.OnDied -= Ocultar;
        }
    }
}
