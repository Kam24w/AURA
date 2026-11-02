using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [Header("Pasos")]
    public AudioClip[] pasos;
    [Min(0.1f)] public float intervaloPasos = 0.42f;

    [Header("Combate")]
    public AudioClip disparo;
    public AudioClip melee;
    public AudioClip dano;
    public AudioClip muerte;

    [Range(0f, 1f)] public float volumen = 0.75f;
    public float distanciaMaxima = 24f;

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

    public void ActualizarPasos(float velocidad)
    {
        if (velocidad < 0.15f)
        {
            proximoPaso = 0f;
            return;
        }
        if (Time.time < proximoPaso || pasos == null || pasos.Length == 0) return;

        AudioClip clip = pasos[Random.Range(0, pasos.Length)];
        if (clip == null) return;

        fuentePasos.pitch = Random.Range(0.8f, 0.92f);
        fuentePasos.PlayOneShot(clip, volumen * 0.4f);
        proximoPaso = Time.time + intervaloPasos;
    }

    public void ReproducirDisparo() => Reproducir(disparo);
    public void ReproducirMelee() => Reproducir(melee, 0.85f);
    public void ReproducirDano() => Reproducir(dano, 0.75f);

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

    private void ConfigurarFuente(AudioSource audioSource)
    {
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = distanciaMaxima;
    }
}
