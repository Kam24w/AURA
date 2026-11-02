using UnityEngine;

public static class GameSettings
{
    private const string ClaveVolumen = "AURA.VolumenGeneral";
    private const string ClaveSensibilidad = "AURA.SensibilidadMouse";

    public static float VolumenGeneral { get; private set; } = 1f;
    public static float SensibilidadMouse { get; private set; } = 3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Cargar()
    {
        VolumenGeneral = Mathf.Clamp01(PlayerPrefs.GetFloat(ClaveVolumen, 1f));
        SensibilidadMouse = Mathf.Clamp(PlayerPrefs.GetFloat(ClaveSensibilidad, 3f), 0.5f, 8f);
        AudioListener.volume = VolumenGeneral;
    }

    public static void EstablecerVolumen(float volumen)
    {
        VolumenGeneral = Mathf.Clamp01(volumen);
        AudioListener.volume = VolumenGeneral;
        PlayerPrefs.SetFloat(ClaveVolumen, VolumenGeneral);
    }

    public static void EstablecerSensibilidad(float sensibilidad)
    {
        SensibilidadMouse = Mathf.Clamp(sensibilidad, 0.5f, 8f);
        PlayerPrefs.SetFloat(ClaveSensibilidad, SensibilidadMouse);
    }

    public static void Guardar()
    {
        PlayerPrefs.Save();
    }
}
