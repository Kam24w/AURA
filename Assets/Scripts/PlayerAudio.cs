using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Pasos")]
    public AudioClip[] pasos;
    [Min(0.1f)] public float intervaloPasos = 0.34f;

    [Header("Movimiento")]
    public AudioClip salto;
    public AudioClip aterrizaje;
    public AudioClip deslizamiento;

    [Header("Combate")]
    public AudioClip espada;
    public AudioClip patada;
    public AudioClip combo;
    public AudioClip impacto;
    public AudioClip bloqueo;
    public AudioClip dano;
    public AudioClip muerte;

    [Range(0f, 1f)] public float volumen = 0.8f;

    private AudioSource fuente;
    private AudioSource fuentePasos;
    private float proximoPaso;

    private void Awake()
    {
        fuente = GetComponent<AudioSource>();
        if (fuente == null)
        {
            fuente = gameObject.AddComponent<AudioSource>();
        }
        ConfigurarFuente(fuente);

        fuentePasos = gameObject.AddComponent<AudioSource>();
        ConfigurarFuente(fuentePasos);
    }

    public void ActualizarPasos(bool moviendo)
    {
        if (!moviendo)
        {
            proximoPaso = 0f;
            return;
        }
        if (Time.time < proximoPaso || pasos == null || pasos.Length == 0) return;

        AudioClip clip = pasos[Random.Range(0, pasos.Length)];
        if (clip == null) return;

        fuentePasos.pitch = Random.Range(0.94f, 1.06f);
        fuentePasos.PlayOneShot(clip, volumen * 0.55f);
        proximoPaso = Time.time + intervaloPasos;
    }

    public void ReproducirSalto() => Reproducir(salto);
    public void ReproducirAterrizaje() => Reproducir(aterrizaje, 0.7f);
    public void ReproducirDeslizamiento() => Reproducir(deslizamiento, 0.75f);
    public void ReproducirEspada() => Reproducir(espada, 0.75f);
    public void ReproducirPatada() => Reproducir(patada, 0.7f);
    public void ReproducirCombo() => Reproducir(combo, 0.85f);
    public void ReproducirImpacto() => Reproducir(impacto);
    public void ReproducirBloqueo() => Reproducir(bloqueo, 0.7f);
    public void ReproducirDano() => Reproducir(dano);

    public void ReproducirMuerte()
    {
        fuente.Stop();
        fuentePasos.Stop();
        Reproducir(muerte);
    }

    private void Reproducir(AudioClip clip, float escalaVolumen = 1f)
    {
        if (clip != null)
        {
            fuente.PlayOneShot(clip, volumen * escalaVolumen);
        }
    }

    private static void ConfigurarFuente(AudioSource audioSource)
    {
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }
}
