using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Ajustes de Vida")]
    [Min(1f)] public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Slider healthBarSlider;
    public bool crearBarraSiFalta = true;
    public Color colorBarra = new Color(0.2f, 0.9f, 0.25f, 1f);

    public bool IsDead { get; private set; }
    public event Action<float> OnDamaged;
    public event Action OnDied;

    private PlayerController playerController;
    private CharacterController characterController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;

        if (healthBarSlider == null && crearBarraSiFalta)
        {
            healthBarSlider = CrearBarraEnPantalla();
        }

        ActualizarBarra();
        CrearIndicacionesControles();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f ||
            (playerController != null && playerController.EstaBloqueando))
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        ActualizarBarra();
        OnDamaged?.Invoke(amount);

        if (currentHealth <= 0f)
        {
            Die();
        }
        else if (playerController != null)
        {
            playerController.RecibirGolpe();
        }
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (playerController != null)
        {
            playerController.Morir();
            playerController.enabled = false;
        }
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        OnDied?.Invoke();
        Debug.Log("AURA: el jugador ha muerto.", this);
    }

    private Slider CrearBarraEnPantalla()
    {
        GameObject canvasObject = new GameObject("PlayerHealthCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Slider slider = HealthBarFactory.Create(canvas.transform, "PlayerHealthBar", new Vector2(320f, 24f), colorBarra);
        RectTransform rect = (RectTransform)slider.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        return slider;
    }

    private void CrearIndicacionesControles()
    {
        if (healthBarSlider == null) return;

        Canvas canvas = healthBarSlider.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.transform.Find("ControlsHelp") != null) return;

        GameObject textObject = new GameObject("ControlsHelp", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvas.transform, false);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = "WASD: mover\nEspacio: saltar\nClic izquierdo: atacar (combo de 5)\nClic derecho mantenido: bloquear\nShift izquierdo + movimiento: deslizar\nAtaque durante deslizamiento: SlideAttack";

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 24f);
        rect.sizeDelta = new Vector2(650f, 150f);
    }

    private void ActualizarBarra()
    {
        if (healthBarSlider == null)
        {
            return;
        }

        healthBarSlider.minValue = 0f;
        healthBarSlider.maxValue = maxHealth;
        healthBarSlider.value = currentHealth;
    }
}
