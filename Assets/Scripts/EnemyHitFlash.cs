using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyHitFlash : MonoBehaviour
{
    [Min(0.02f)] public float duracion = 0.09f;
    public Color colorDestello = new Color(1f, 0.55f, 0.3f, 1f);

    private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int IdColor = Shader.PropertyToID("_Color");

    private EnemyHealth health;
    private Renderer[] renderers;
    private Color[] coloresBase;
    private Color[] colores;
    private bool[] tieneBaseColor;
    private bool[] tieneColor;
    private MaterialPropertyBlock propiedades;
    private float tiempoRestante;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        renderers = GetComponentsInChildren<Renderer>(true);
        coloresBase = new Color[renderers.Length];
        colores = new Color[renderers.Length];
        tieneBaseColor = new bool[renderers.Length];
        tieneColor = new bool[renderers.Length];
        propiedades = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            if (material == null) continue;

            tieneBaseColor[i] = material.HasProperty(IdBaseColor);
            tieneColor[i] = material.HasProperty(IdColor);
            if (tieneBaseColor[i]) coloresBase[i] = material.GetColor(IdBaseColor);
            if (tieneColor[i]) colores[i] = material.GetColor(IdColor);
        }
    }

    private void OnEnable()
    {
        if (health != null) health.OnDamaged += Destellar;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDamaged -= Destellar;
        RestaurarColores();
    }

    private void Update()
    {
        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante > 0f) return;

        RestaurarColores();
        enabled = false;
    }

    private void Destellar(float cantidad)
    {
        AplicarColor(colorDestello, false);
        tiempoRestante = duracion;
        enabled = true;
    }

    private void RestaurarColores()
    {
        AplicarColor(Color.clear, true);
    }

    private void AplicarColor(Color destello, bool restaurar)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;

            renderer.GetPropertyBlock(propiedades);
            if (tieneBaseColor[i])
            {
                propiedades.SetColor(IdBaseColor, restaurar ? coloresBase[i] : destello);
            }
            if (tieneColor[i])
            {
                propiedades.SetColor(IdColor, restaurar ? colores[i] : destello);
            }
            renderer.SetPropertyBlock(propiedades);
        }
    }
}
