using UnityEngine;

[CreateAssetMenu(fileName = "NewBunkerSettings", menuName = "AURA/Bunker Settings")]
public class BunkerSettingsSO : ScriptableObject
{
    [Header("Semilla y cuadrícula")]
    public bool generarAlIniciar = true;
    public int semilla;
    [Range(3, 6)] public int columnas = 4;
    [Range(3, 5)] public int filas = 3;
    [Min(14f)] public float separacionSalas = 20f;
    [Min(8f)] public float tamanoSalaMin = 12f;
    [Min(9f)] public float tamanoSalaMax = 16f;
    [Min(1)] public int conexionesExtra = 5;

    [Header("Circulación")]
    [Min(3f)] public float anchoPasillo = 4f;
    [Min(3f)] public float alturaPiso = 3.5f;
    [Min(4f)] public float alturaEntrePisos = 5f;
    [Min(2.2f)] public float alturaPuerta = 2.7f;
    [Min(0.1f)] public float grosorEstructura = 0.25f;
    [Min(2.5f)] public float anchoRampa = 3.5f;
    [Min(6f)] public float recorridoRampa = 8f;

    [Header("Bloques modulares opcionales")]
    public GameObject prefabSuelo;
    public GameObject prefabPared;
    public GameObject prefabRampa;
    public Material materialSuelo;
    public Material materialParedes;
    public Material materialRampa;

    [Header("Atmósfera y ruinas")]
    public bool atmosferaOscura = true;
    public bool crearTechos = true;
    public bool crearLuces = true;
    public bool generarEscombros = true;
    [Range(0f, 0.1f)] public float densidadNiebla = 0.025f;
    [Range(0f, 2f)] public float intensidadLuces = 0.65f;
    public Color colorAmbiente = new Color(0.055f, 0.075f, 0.1f);
    public Color colorFondo = new Color(0.004f, 0.007f, 0.012f);

    [Header("Jugador, enemigos y bosses")]
    public bool moverJugadorAlSpawn = true;
    public GameObject prefabEnemigo;
    [Min(0)] public int enemigosPorSala;
    public GameObject prefabBoss;
    [Min(0f)] public float distanciaMinimaSpawnJugador = 8f;

    private void OnValidate()
    {
        separacionSalas = Mathf.Max(separacionSalas, tamanoSalaMax + 3f);
        tamanoSalaMin = Mathf.Max(tamanoSalaMin, recorridoRampa + 3f);
        tamanoSalaMax = Mathf.Max(tamanoSalaMax, tamanoSalaMin + 1f);
        alturaEntrePisos = Mathf.Max(alturaEntrePisos, alturaPiso + 0.75f);
        alturaPuerta = Mathf.Clamp(alturaPuerta, 2.2f, alturaPiso - 0.2f);
        recorridoRampa = Mathf.Max(recorridoRampa, alturaEntrePisos * 1.25f);
        anchoRampa = Mathf.Clamp(anchoRampa, 2.5f, anchoPasillo - 0.5f);
        enemigosPorSala = Mathf.Clamp(enemigosPorSala, 0, 2);
        conexionesExtra = Mathf.Max(0, conexionesExtra);
    }
}
