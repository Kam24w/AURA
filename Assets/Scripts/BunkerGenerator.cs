using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BunkerGenerator : MonoBehaviour
{
    private const int TotalPisos = 3;

    [Header("Configuración")]
    [SerializeField] private BunkerSettingsSO config;

    [Header("Escena")]
    public PlayerController jugador;

    public int SemillaUsada { get; private set; }
    public Transform SpawnJugador { get; private set; }
    public List<Transform> PuntosSpawnEnemigos { get; } = new List<Transform>();
    public List<Transform> PuntosSpawnBoss { get; } = new List<Transform>();

    [SerializeField] private Transform raizGenerada;

    private struct RutaRampa
    {
        public Vector3 inicio;
        public Vector3 fin;
    }

    private readonly List<RutaRampa> rutasRampas = new List<RutaRampa>();
    private NavMeshDataInstance navMeshInstancia;

    private static readonly Vector2Int[] Direcciones =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0)
    };

    private sealed class Sala
    {
        public int indice;
        public int columna;
        public int fila;
        public float ancho;
        public float profundidad;
        public Vector3 centro;
        public readonly bool[] conexiones = new bool[4];
    }

    private sealed class Conexion
    {
        public int a;
        public int b;
        public int direccion;

        public Conexion(int a, int b, int direccion)
        {
            this.a = a;
            this.b = b;
            this.direccion = direccion;
        }
    }

    private void Start()
    {
        if (jugador == null)
        {
            jugador = FindFirstObjectByType<PlayerController>();
        }

        if (config != null && config.generarAlIniciar)
        {
            GenerarBunker();
        }
    }

    [ContextMenu("Generar búnker procedural")]
    public void GenerarBunker()
    {
        if (config == null)
        {
            Debug.LogError("AURA: asigna un BunkerSettingsSO en el campo Config antes de generar.", this);
            return;
        }

        LimpiarBunker();

        SemillaUsada = config.semilla != 0 ? config.semilla : Random.Range(1, int.MaxValue);
        System.Random aleatorio = new System.Random(SemillaUsada);

        if (config.atmosferaOscura)
        {
            ConfigurarAtmosfera();
        }

        GameObject raiz = new GameObject($"Bunker_Procedural_{SemillaUsada}");
        raizGenerada = raiz.transform;
        raizGenerada.SetParent(transform, false);

        int rampa01 = -1;
        int rampa12 = -1;
        int orientacionRampa01 = 0;
        int orientacionRampa12 = 0;
        Vector3 posicionJugador = Vector3.zero;

        for (int piso = 0; piso < TotalPisos; piso++)
        {
            float alturaY = piso * config.alturaEntrePisos;
            List<Sala> salas = CrearSalas(aleatorio);
            List<Conexion> conexiones = CrearGrafoConectado(salas, aleatorio);

            int entrada = piso == 0 ? 0 : piso == 1 ? rampa01 : rampa12;
            int salida = -1;
            int orientacionEntrada = piso == 1 ? orientacionRampa01 : orientacionRampa12;
            int orientacionSalida = 0;

            if (piso == 0)
            {
                rampa01 = ElegirSalaRampa(salas, entrada, aleatorio);
                salida = rampa01;
                orientacionRampa01 = aleatorio.Next(0, 4) * 90;
                orientacionSalida = orientacionRampa01;
                posicionJugador = salas[0].centro + Vector3.up * 0.1f;
            }
            else if (piso == 1)
            {
                rampa12 = ElegirSalaRampa(salas, entrada, aleatorio);
                salida = rampa12;
                orientacionRampa12 = aleatorio.Next(0, 4) * 90;
                orientacionSalida = orientacionRampa12;
            }

            HashSet<int> excluidosBoss = new HashSet<int> { entrada };
            if (salida >= 0)
            {
                excluidosBoss.Add(salida);
            }

            int salaBoss = ElegirSalaLejana(salas, entrada, excluidosBoss, aleatorio);
            Transform raizPiso = CrearGrupo($"Piso_{piso + 1}", raizGenerada);

            ConstruirPiso(raizPiso, salas, conexiones, piso, alturaY, entrada, salida,
                orientacionEntrada, orientacionSalida, salaBoss, aleatorio);
        }

        SpawnJugador = CrearPunto("Spawn_Jugador", raizGenerada, posicionJugador);

        Physics.SyncTransforms();
        MoverJugador();

        // El NavMesh debe existir antes de activar los enemigos y bosses.
        HornearNavMesh();
        SpawnBosses();

        Debug.Log($"AURA: semilla {SemillaUsada}. {TotalPisos} pisos, {config.columnas * config.filas * TotalPisos} salas, {PuntosSpawnEnemigos.Count} spawns enemigos y {PuntosSpawnBoss.Count} bosses. Rampas en salas {rampa01} y {rampa12}.", this);
    }

    [ContextMenu("Limpiar búnker")]
    public void LimpiarBunker()
    {
        PuntosSpawnEnemigos.Clear();
        PuntosSpawnBoss.Clear();
        rutasRampas.Clear();
        SpawnJugador = null;

        if (navMeshInstancia.valid)
        {
            navMeshInstancia.Remove();
            navMeshInstancia = default;
        }

        if (raizGenerada == null)
        {
            return;
        }

        GameObject raiz = raizGenerada.gameObject;
        raizGenerada = null;
        raiz.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(raiz);
        }
        else
        {
            DestroyImmediate(raiz);
        }
    }

    private List<Sala> CrearSalas(System.Random aleatorio)
    {
        List<Sala> salas = new List<Sala>(config.columnas * config.filas);
        float offsetX = (config.columnas - 1) * config.separacionSalas * 0.5f;
        float offsetZ = (config.filas - 1) * config.separacionSalas * 0.5f;

        for (int fila = 0; fila < config.filas; fila++)
        {
            for (int columna = 0; columna < config.columnas; columna++)
            {
                float ancho = Mathf.Lerp(config.tamanoSalaMin, config.tamanoSalaMax, (float)aleatorio.NextDouble());
                float profundidad = Mathf.Lerp(config.tamanoSalaMin, config.tamanoSalaMax, (float)aleatorio.NextDouble());
                ancho = Mathf.Round(ancho * 2f) * 0.5f;
                profundidad = Mathf.Round(profundidad * 2f) * 0.5f;

                salas.Add(new Sala
                {
                    indice = salas.Count,
                    columna = columna,
                    fila = fila,
                    ancho = ancho,
                    profundidad = profundidad,
                    centro = new Vector3(columna * config.separacionSalas - offsetX, 0f, fila * config.separacionSalas - offsetZ)
                });
            }
        }

        return salas;
    }

    private List<Conexion> CrearGrafoConectado(List<Sala> salas, System.Random aleatorio)
    {
        List<Conexion> conexiones = new List<Conexion>();
        HashSet<int> visitados = new HashSet<int> { 0 };
        Stack<int> pendientes = new Stack<int>();
        pendientes.Push(0);

        while (pendientes.Count > 0)
        {
            int actual = pendientes.Peek();
            List<int> vecinos = ObtenerVecinos(salas, actual);
            List<int> sinVisitar = new List<int>();

            foreach (int vecino in vecinos)
            {
                if (!visitados.Contains(vecino))
                {
                    sinVisitar.Add(vecino);
                }
            }

            if (sinVisitar.Count == 0)
            {
                pendientes.Pop();
                continue;
            }

            int siguiente = sinVisitar[aleatorio.Next(sinVisitar.Count)];
            int direccion = DireccionEntre(salas, actual, siguiente);
            Conectar(salas, conexiones, actual, siguiente, direccion);
            visitados.Add(siguiente);
            pendientes.Push(siguiente);
        }

        List<Conexion> candidatos = new List<Conexion>();
        foreach (Sala sala in salas)
        {
            for (int direccion = 0; direccion < 2; direccion++)
            {
                int vecino = VecinoEnDireccion(salas, sala.indice, direccion);
                if (vecino >= 0 && !sala.conexiones[direccion])
                {
                    candidatos.Add(new Conexion(sala.indice, vecino, direccion));
                }
            }
        }

        Mezclar(candidatos, aleatorio);
        int cantidad = Mathf.Min(config.conexionesExtra, candidatos.Count);
        for (int i = 0; i < cantidad; i++)
        {
            Conexion conexion = candidatos[i];
            Conectar(salas, conexiones, conexion.a, conexion.b, conexion.direccion);
        }

        return conexiones;
    }

    private void Conectar(List<Sala> salas, List<Conexion> conexiones, int a, int b, int direccion)
    {
        salas[a].conexiones[direccion] = true;
        salas[b].conexiones[(direccion + 2) % 4] = true;
        conexiones.Add(new Conexion(a, b, direccion));
    }

    private List<int> ObtenerVecinos(List<Sala> salas, int indice)
    {
        List<int> vecinos = new List<int>(4);
        for (int direccion = 0; direccion < 4; direccion++)
        {
            int vecino = VecinoEnDireccion(salas, indice, direccion);
            if (vecino >= 0)
            {
                vecinos.Add(vecino);
            }
        }
        return vecinos;
    }

    private int VecinoEnDireccion(List<Sala> salas, int indice, int direccion)
    {
        int columna = salas[indice].columna + Direcciones[direccion].x;
        int fila = salas[indice].fila + Direcciones[direccion].y;

        if (columna < 0 || columna >= config.columnas || fila < 0 || fila >= config.filas)
        {
            return -1;
        }

        return fila * config.columnas + columna;
    }

    private int DireccionEntre(List<Sala> salas, int a, int b)
    {
        int deltaX = salas[b].columna - salas[a].columna;
        int deltaZ = salas[b].fila - salas[a].fila;

        if (deltaX == 1) return 1;
        if (deltaX == -1) return 3;
        if (deltaZ == 1) return 0;
        return 2;
    }

    private void Mezclar<T>(List<T> elementos, System.Random aleatorio)
    {
        for (int i = elementos.Count - 1; i > 0; i--)
        {
            int j = aleatorio.Next(i + 1);
            (elementos[i], elementos[j]) = (elementos[j], elementos[i]);
        }
    }

    private int ElegirSalaLejana(List<Sala> salas, int entrada, HashSet<int> excluidas, System.Random aleatorio)
    {
        int[] distancias = CalcularDistancias(salas, entrada);
        int mayorDistancia = -1;
        List<int> candidatas = new List<int>();

        foreach (Sala sala in salas)
        {
            if (excluidas.Contains(sala.indice) || distancias[sala.indice] < 0)
            {
                continue;
            }

            if (distancias[sala.indice] > mayorDistancia)
            {
                mayorDistancia = distancias[sala.indice];
                candidatas.Clear();
            }

            if (distancias[sala.indice] == mayorDistancia)
            {
                candidatas.Add(sala.indice);
            }
        }

        return candidatas.Count > 0 ? candidatas[aleatorio.Next(candidatas.Count)] : entrada;
    }

    private int ElegirSalaRampa(List<Sala> salas, int entrada, System.Random aleatorio)
    {
        int[] distancias = CalcularDistancias(salas, entrada);
        int mayorDistancia = 0;

        foreach (int distancia in distancias)
        {
            mayorDistancia = Mathf.Max(mayorDistancia, distancia);
        }

        int distanciaMinima = Mathf.Max(2, mayorDistancia - 1);
        List<int> candidatas = new List<int>();

        foreach (Sala sala in salas)
        {
            if (sala.indice != entrada && distancias[sala.indice] >= distanciaMinima)
            {
                candidatas.Add(sala.indice);
            }
        }

        return candidatas.Count > 0 ? candidatas[aleatorio.Next(candidatas.Count)] : entrada;
    }

    private int[] CalcularDistancias(List<Sala> salas, int entrada)
    {
        int[] distancias = new int[salas.Count];
        for (int i = 0; i < distancias.Length; i++)
        {
            distancias[i] = -1;
        }

        Queue<int> pendientes = new Queue<int>();
        distancias[entrada] = 0;
        pendientes.Enqueue(entrada);

        while (pendientes.Count > 0)
        {
            int actual = pendientes.Dequeue();
            for (int direccion = 0; direccion < 4; direccion++)
            {
                if (!salas[actual].conexiones[direccion])
                {
                    continue;
                }

                int vecino = VecinoEnDireccion(salas, actual, direccion);
                if (vecino >= 0 && distancias[vecino] < 0)
                {
                    distancias[vecino] = distancias[actual] + 1;
                    pendientes.Enqueue(vecino);
                }
            }
        }

        return distancias;
    }

    private void ConstruirPiso(Transform raizPiso, List<Sala> salas, List<Conexion> conexiones,
        int piso, float alturaY, int entrada, int salida, int orientacionEntrada,
        int orientacionSalida, int salaBoss, System.Random aleatorio)
    {
        foreach (Sala sala in salas)
        {
            Transform raizSala = CrearGrupo($"Sala_{sala.fila}_{sala.columna}", raizPiso);
            bool llegaRampa = sala.indice == entrada && piso > 0;
            bool saleRampa = sala.indice == salida;
            int orientacionLlegada = llegaRampa ? orientacionEntrada : 0;

            ConstruirSala(raizSala, sala, alturaY, llegaRampa, saleRampa,
                orientacionLlegada, orientacionSalida);

            if (saleRampa)
            {
                CrearRampa(raizSala, sala, alturaY, orientacionSalida);
            }

            if (llegaRampa)
            {
                CrearProteccionesHueco(raizSala, sala, alturaY, orientacionLlegada);
            }

            List<GameObject> enemigosDeSala = new List<GameObject>();

            if (sala.indice != entrada && !saleRampa && !llegaRampa)
            {
                enemigosDeSala = CrearSpawnsEnemigosConLista(raizSala, sala, piso, alturaY);
            }

            // Configurar el RoomEncounter en la sala si no es la de entrada
            if (sala.indice != entrada && !llegaRampa)
            {
                RoomEncounter encuentro = raizSala.gameObject.AddComponent<RoomEncounter>();

                // Configurar el Collider tipo Trigger para detectar al jugador al entrar
                BoxCollider triggerSala = raizSala.gameObject.AddComponent<BoxCollider>();
                triggerSala.isTrigger = true;
                triggerSala.size = new Vector3(sala.ancho * 0.8f, 3f, sala.profundidad * 0.8f);
                triggerSala.center = new Vector3(sala.centro.x, alturaY + 1.5f, sala.centro.z);

                // Recolectar las posiciones de las puertas conectadas para colocar las barreras azules
                List<Vector3> puertasPosiciones = ObtenerPosicionesPuertas(sala, alturaY);

                encuentro.InicializarSala(enemigosDeSala, puertasPosiciones, sala.centro,
                    config.anchoPasillo, config.alturaPuerta);
            }

            if (config.crearLuces)
            {
                Color color = saleRampa || llegaRampa ? new Color(1f, 0.06f, 0.025f) : new Color(0.28f, 0.55f, 0.68f);
                CrearLuz(raizSala, saleRampa || llegaRampa ? "Luz_Alerta_AURA" : "Luz_Fria",
                    sala.centro + Vector3.up * (alturaY + config.alturaPiso - 0.35f), color);
            }

            if (config.generarEscombros)
            {
                CrearEscombros(raizSala, sala, alturaY, aleatorio);
            }
        }

        foreach (Conexion conexion in conexiones)
        {
            CrearPasillo(raizPiso, salas[conexion.a], salas[conexion.b], conexion.direccion, alturaY);
        }

        Vector3 posicionBoss = salas[salaBoss].centro + Vector3.up * (alturaY + 0.1f);
        Transform puntoBoss = CrearPunto($"Spawn_Boss_P{piso + 1}", raizPiso, posicionBoss);
        PuntosSpawnBoss.Add(puntoBoss);
    }
    private List<GameObject> CrearSpawnsEnemigosConLista(Transform padre, Sala sala, int piso, float alturaY)
{
    List<GameObject> listaInstanciados = new List<GameObject>();
    float offsetX = sala.ancho * 0.25f;
    float offsetZ = sala.profundidad * 0.25f;
    Vector3[] offsets =
    {
        new Vector3(-offsetX, 0f, -offsetZ),
        new Vector3(offsetX, 0f, offsetZ)
    };

    for (int i = 0; i < offsets.Length; i++)
    {
        if (i >= config.enemigosPorSala) continue;

        Vector3 posicion = sala.centro + offsets[i] + Vector3.up * (alturaY + 0.1f);
        Transform punto = CrearPunto($"Spawn_Enemigo_P{piso + 1}_S{sala.indice}_{i + 1}", padre, posicion);
        PuntosSpawnEnemigos.Add(punto);

        if (config.prefabEnemigo != null)
        {
            GameObject enemigoInst = Instantiate(config.prefabEnemigo, punto.position, punto.rotation, punto);
            listaInstanciados.Add(enemigoInst);
        }
    }

    return listaInstanciados;
}
private List<Vector3> ObtenerPosicionesPuertas(Sala sala, float alturaY)
{
    List<Vector3> posiciones = new List<Vector3>();

    for (int direccion = 0; direccion < 4; direccion++)
    {
        if (sala.conexiones[direccion])
        {
            Vector3 centroPuerta = sala.centro;
            if (direccion == 0) centroPuerta.z += sala.profundidad * 0.5f;
            else if (direccion == 2) centroPuerta.z -= sala.profundidad * 0.5f;
            else if (direccion == 1) centroPuerta.x += sala.ancho * 0.5f;
            else if (direccion == 3) centroPuerta.x -= sala.ancho * 0.5f;

            centroPuerta.y = alturaY;
            posiciones.Add(centroPuerta);
        }
    }

    return posiciones;
}

    private void ConstruirSala(Transform padre, Sala sala, float alturaY, bool llegaRampa,
        bool saleRampa, int orientacionLlegada, int orientacionSalida)
    {
        Rect area = AreaSala(sala);

        if (llegaRampa)
        {
            CrearRectanguloConHueco("Suelo", padre, area, HuecoRampa(sala, orientacionLlegada), alturaY, false);
        }
        else
        {
            CrearLosa("Suelo", padre, area, alturaY, false);
        }

        if (config.crearTechos)
        {
            if (saleRampa)
            {
                CrearRectanguloConHueco("Techo", padre, area, HuecoRampa(sala, orientacionSalida), alturaY, true);
            }
            else
            {
                CrearLosa("Techo", padre, area, alturaY, true);
            }
        }

        for (int direccion = 0; direccion < 4; direccion++)
        {
            Vector3 centro = sala.centro;
            float longitud;
            bool aLoLargoDeX;

            if (direccion == 0 || direccion == 2)
            {
                centro.z += (direccion == 0 ? 1f : -1f) * sala.profundidad * 0.5f;
                longitud = sala.ancho;
                aLoLargoDeX = true;
            }
            else
            {
                centro.x += (direccion == 1 ? 1f : -1f) * sala.ancho * 0.5f;
                longitud = sala.profundidad;
                aLoLargoDeX = false;
            }

            CrearPared(padre, $"Pared_{direccion}", centro, alturaY, longitud,
                aLoLargoDeX, sala.conexiones[direccion]);
        }
    }

    private void CrearPared(Transform padre, string nombre, Vector3 centro, float alturaY,
        float longitud, bool aLoLargoDeX, bool tienePuerta)
    {
        if (!tienePuerta)
        {
            CrearSegmentoPared(nombre, padre, centro, alturaY, longitud, config.alturaPiso, aLoLargoDeX);
            return;
        }

        float tramo = (longitud - config.anchoPasillo) * 0.5f;
        float desplazamiento = config.anchoPasillo * 0.5f + tramo * 0.5f;
        Vector3 direccion = aLoLargoDeX ? Vector3.right : Vector3.forward;

        CrearSegmentoPared($"{nombre}_A", padre, centro - direccion * desplazamiento,
            alturaY, tramo, config.alturaPiso, aLoLargoDeX);
        CrearSegmentoPared($"{nombre}_B", padre, centro + direccion * desplazamiento,
            alturaY, tramo, config.alturaPiso, aLoLargoDeX);

        float alturaDintel = config.alturaPiso - config.alturaPuerta;
        Vector3 centroDintel = centro + Vector3.up * config.alturaPuerta;
        CrearSegmentoPared($"{nombre}_Dintel", padre, centroDintel,
            alturaY, config.anchoPasillo, alturaDintel, aLoLargoDeX);
    }

    private void CrearSegmentoPared(string nombre, Transform padre, Vector3 centro, float alturaY,
        float longitud, float altura, bool aLoLargoDeX)
    {
        Vector3 escala = aLoLargoDeX
            ? new Vector3(longitud, altura, config.grosorEstructura)
            : new Vector3(config.grosorEstructura, altura, longitud);

        InstantiateBunkerElement(nombre, config.prefabPared, padre, centro + Vector3.up * (alturaY + altura * 0.5f),
            escala, config.materialParedes, Quaternion.identity);
    }

    private void CrearPasillo(Transform padre, Sala a, Sala b, int direccion, float alturaY)
    {
        Transform raizPasillo = CrearGrupo($"Pasillo_{a.indice}_{b.indice}", padre);
        bool horizontal = direccion == 1 || direccion == 3;
        float inicio;
        float fin;

        if (horizontal)
        {
            float signo = direccion == 1 ? 1f : -1f;
            inicio = a.centro.x + signo * a.ancho * 0.5f;
            fin = b.centro.x - signo * b.ancho * 0.5f;
        }
        else
        {
            float signo = direccion == 0 ? 1f : -1f;
            inicio = a.centro.z + signo * a.profundidad * 0.5f;
            fin = b.centro.z - signo * b.profundidad * 0.5f;
        }

        float minimo = Mathf.Min(inicio, fin);
        float maximo = Mathf.Max(inicio, fin);
        float longitud = maximo - minimo;
        float centroLongitud = (minimo + maximo) * 0.5f;
        Vector3 centro = horizontal
            ? new Vector3(centroLongitud, 0f, a.centro.z)
            : new Vector3(a.centro.x, 0f, centroLongitud);

        Rect area = horizontal
            ? new Rect(centroLongitud - longitud * 0.5f, a.centro.z - config.anchoPasillo * 0.5f, longitud, config.anchoPasillo)
            : new Rect(a.centro.x - config.anchoPasillo * 0.5f, centroLongitud - longitud * 0.5f, config.anchoPasillo, longitud);

        CrearLosa("Suelo_Pasillo", raizPasillo, area, alturaY, false);
        if (config.crearTechos)
        {
            CrearLosa("Techo_Pasillo", raizPasillo, area, alturaY, true);
        }

        Vector3 escalaSuelo = horizontal
            ? new Vector3(longitud, config.alturaPiso, config.grosorEstructura)
            : new Vector3(config.grosorEstructura, config.alturaPiso, longitud);
        Vector3 offset = horizontal ? Vector3.forward : Vector3.right;

        InstantiateBunkerElement("Pared_Pasillo_A", config.prefabPared, raizPasillo,
            centro + offset * (config.anchoPasillo * 0.5f) + Vector3.up * (alturaY + config.alturaPiso * 0.5f),
            escalaSuelo, config.materialParedes, Quaternion.identity);
        InstantiateBunkerElement("Pared_Pasillo_B", config.prefabPared, raizPasillo,
            centro - offset * (config.anchoPasillo * 0.5f) + Vector3.up * (alturaY + config.alturaPiso * 0.5f),
            escalaSuelo, config.materialParedes, Quaternion.identity);
    }

    private void CrearRampa(Transform padre, Sala sala, float alturaY, int orientacion)
    {
        float longitud = Mathf.Sqrt(config.recorridoRampa * config.recorridoRampa + config.alturaEntrePisos * config.alturaEntrePisos);
        float inclinacion = -Mathf.Atan2(config.alturaEntrePisos, config.recorridoRampa) * Mathf.Rad2Deg;
        Quaternion rotacion = Quaternion.Euler(0f, orientacion, 0f) * Quaternion.Euler(inclinacion, 0f, 0f);
        Vector3 centro = sala.centro + Vector3.up * (alturaY + config.alturaEntrePisos * 0.5f);

        InstantiateBunkerElement($"Rampa_Sala_{sala.indice}", config.prefabRampa, padre,
            centro - rotacion * Vector3.up * config.grosorEstructura * 0.5f,
            new Vector3(config.anchoRampa, config.grosorEstructura, longitud),
            config.materialRampa != null ? config.materialRampa : config.materialSuelo, rotacion);

        Vector3 derecha = Quaternion.Euler(0f, orientacion, 0f) * Vector3.right;
        float offset = config.anchoRampa * 0.5f + 0.08f;
        Vector3 alturaBaranda = rotacion * Vector3.up * 0.55f;

        InstantiateBunkerElement("Baranda_Rampa_A", config.prefabPared, padre,
            centro - derecha * offset + alturaBaranda,
            new Vector3(0.14f, 1f, longitud), config.materialParedes, rotacion);
        InstantiateBunkerElement("Baranda_Rampa_B", config.prefabPared, padre,
            centro + derecha * offset + alturaBaranda,
            new Vector3(0.14f, 1f, longitud), config.materialParedes, rotacion);

        rutasRampas.Add(new RutaRampa
        {
            inicio = sala.centro + Vector3.up * (alturaY + 0.2f),
            fin = sala.centro + Vector3.up * (alturaY + config.alturaEntrePisos + 0.2f)
        });
    }

    private void CrearProteccionesHueco(Transform padre, Sala sala, float alturaY, int orientacion)
    {
        bool vertical = orientacion % 180 == 0;
        float margen = 0.3f;
        Vector3 offset = vertical
            ? Vector3.right * (config.anchoRampa * 0.5f + margen)
            : Vector3.forward * (config.anchoRampa * 0.5f + margen);
        Vector3 escala = vertical
            ? new Vector3(0.14f, 1f, config.recorridoRampa)
            : new Vector3(config.recorridoRampa, 1f, 0.14f);
        Vector3 centro = sala.centro + Vector3.up * (alturaY + 0.5f);

        InstantiateBunkerElement("Proteccion_Hueco_A", config.prefabPared, padre, centro - offset,
            escala, config.materialParedes, Quaternion.identity);
        InstantiateBunkerElement("Proteccion_Hueco_B", config.prefabPared, padre, centro + offset,
            escala, config.materialParedes, Quaternion.identity);
    }

    private Rect AreaSala(Sala sala)
    {
        return new Rect(sala.centro.x - sala.ancho * 0.5f,
            sala.centro.z - sala.profundidad * 0.5f, sala.ancho, sala.profundidad);
    }

    private Rect HuecoRampa(Sala sala, int orientacion)
    {
        bool vertical = orientacion % 180 == 0;
        float ancho = vertical ? config.anchoRampa + 0.6f : config.recorridoRampa + 0.6f;
        float profundidad = vertical ? config.recorridoRampa + 0.6f : config.anchoRampa + 0.6f;
        return new Rect(sala.centro.x - ancho * 0.5f, sala.centro.z - profundidad * 0.5f,
            ancho, profundidad);
    }

    private void CrearRectanguloConHueco(string nombre, Transform padre, Rect area, Rect hueco,
        float alturaY, bool techo)
    {
        CrearLosa($"{nombre}_Oeste", padre,
            RectDesdeLimites(area.xMin, area.yMin, hueco.xMin, area.yMax), alturaY, techo);
        CrearLosa($"{nombre}_Este", padre,
            RectDesdeLimites(hueco.xMax, area.yMin, area.xMax, area.yMax), alturaY, techo);
        CrearLosa($"{nombre}_Sur", padre,
            RectDesdeLimites(hueco.xMin, area.yMin, hueco.xMax, hueco.yMin), alturaY, techo);
        CrearLosa($"{nombre}_Norte", padre,
            RectDesdeLimites(hueco.xMin, hueco.yMax, hueco.xMax, area.yMax), alturaY, techo);
    }

    private void CrearLosa(string nombre, Transform padre, Rect area, float alturaY, bool techo)
    {
        if (area.width <= 0.01f || area.height <= 0.01f)
        {
            return;
        }

        float y = techo
            ? alturaY + config.alturaPiso + config.grosorEstructura * 0.5f
            : alturaY - config.grosorEstructura * 0.5f;
        Material material = techo ? config.materialParedes : config.materialSuelo;
        GameObject prefab = techo ? config.prefabPared : config.prefabSuelo;

        InstantiateBunkerElement(nombre, prefab, padre,
            new Vector3(area.center.x, y, area.center.y),
            new Vector3(area.width, config.grosorEstructura, area.height), material, Quaternion.identity);
    }

    private Rect RectDesdeLimites(float xMin, float zMin, float xMax, float zMax)
    {
        return new Rect(xMin, zMin, Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, zMax - zMin));
    }

    private void CrearSpawnsEnemigos(Transform padre, Sala sala, int piso, float alturaY)
    {
        float offsetX = sala.ancho * 0.25f;
        float offsetZ = sala.profundidad * 0.25f;
        Vector3[] offsets =
        {
            new Vector3(-offsetX, 0f, -offsetZ),
            new Vector3(offsetX, 0f, offsetZ)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 posicion = sala.centro + offsets[i] + Vector3.up * (alturaY + 0.1f);
            Transform punto = CrearPunto($"Spawn_Enemigo_P{piso + 1}_S{sala.indice}_{i + 1}", padre, posicion);
            PuntosSpawnEnemigos.Add(punto);
        }
    }

    private void HornearNavMesh()
    {
        if (navMeshInstancia.valid)
        {
            navMeshInstancia.Remove();
        }

        float ancho = config.columnas * config.separacionSalas + config.tamanoSalaMax;
        float profundo = config.filas * config.separacionSalas + config.tamanoSalaMax;
        float alto = TotalPisos * config.alturaEntrePisos + config.alturaPiso + 6f;
        Bounds limites = new Bounds(new Vector3(0f, alto * 0.5f - 2f, 0f),
            new Vector3(ancho, alto, profundo));

        navMeshInstancia = RuntimeNavMesh.Hornear(raizGenerada, limites);
    }

    private void SpawnEnemigos()
    {
        if (config.prefabEnemigo == null || config.enemigosPorSala <= 0)
        {
            return;
        }

        // Cada sala aporta 2 puntos consecutivos; se respeta enemigosPorSala
        for (int i = 0; i < PuntosSpawnEnemigos.Count; i++)
        {
            if (i % 2 >= config.enemigosPorSala)
            {
                continue;
            }

            Transform punto = PuntosSpawnEnemigos[i];
            if (PosicionSpawnValida(punto.position, out Vector3 posicion))
            {
                Instantiate(config.prefabEnemigo, posicion, punto.rotation, punto);
            }
        }
    }

    private void SpawnBosses()
    {
        for (int indice = 0; indice < PuntosSpawnBoss.Count; indice++)
        {
            Transform punto = PuntosSpawnBoss[indice];
            int piso = indice + 1;
            GameObject prefabBoss = ObtenerPrefabBoss(piso);

            if (prefabBoss == null)
            {
                Debug.LogWarning($"AURA: no hay prefab asignado para el boss del piso {piso}.", this);
                continue;
            }

            if (PosicionSpawnValida(punto.position, out Vector3 posicion))
            {
                GameObject boss = Instantiate(prefabBoss, posicion, punto.rotation, punto);
                BossController controlador = boss.GetComponentInChildren<BossController>(true);
                if (controlador != null)
                {
                    controlador.pisoActual = piso;
                }
            }
        }
    }

    private GameObject ObtenerPrefabBoss(int piso)
    {
        GameObject prefabEspecifico = piso == 1
            ? config.prefabBossPiso1
            : piso == 2
                ? config.prefabBossPiso2
                : config.prefabBossPiso3;

        return prefabEspecifico != null ? prefabEspecifico : config.prefabBoss;
    }

    // Descarta spawns fuera del NavMesh o demasiado cerca del jugador.
    // Los puntos predefinidos ya están separados entre salas y dentro de cada sala,
    // por lo que no hace falta comprobar solapamiento entre enemigos.
    private bool PosicionSpawnValida(Vector3 propuesta, out Vector3 posicion)
    {
        posicion = propuesta;

        if (jugador != null
            && Vector3.Distance(propuesta, jugador.transform.position) < config.distanciaMinimaSpawnJugador)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(propuesta, out NavMeshHit impacto, 2f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"AURA: spawn en {propuesta} descartado por estar fuera del NavMesh.", this);
            return false;
        }

        posicion = impacto.position + Vector3.up * 0.05f;
        return true;
    }

    private void CrearEscombros(Transform padre, Sala sala, float alturaY, System.Random aleatorio)
    {
        for (int i = 0; i < 2; i++)
        {
            float ancho = Mathf.Lerp(0.3f, 0.9f, (float)aleatorio.NextDouble());
            float alto = Mathf.Lerp(0.12f, 0.4f, (float)aleatorio.NextDouble());
            float profundidad = Mathf.Lerp(0.3f, 0.8f, (float)aleatorio.NextDouble());
            float x = sala.centro.x + (aleatorio.Next(0, 2) == 0 ? -1f : 1f) * (sala.ancho * 0.5f - 0.8f);
            float z = sala.centro.z + Mathf.Lerp(-sala.profundidad * 0.35f, sala.profundidad * 0.35f,
                (float)aleatorio.NextDouble());
            Quaternion rotacion = Quaternion.Euler(0f, (float)aleatorio.NextDouble() * 360f, 0f);

            GameObject escombro = InstantiateBunkerElement($"Escombro_{i}", null, padre,
                new Vector3(x, alturaY + alto * 0.5f, z),
                new Vector3(ancho, alto, profundidad), config.materialParedes, rotacion);

            foreach (Collider colision in escombro.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying)
                {
                    Destroy(colision);
                }
                else
                {
                    DestroyImmediate(colision);
                }
            }
        }
    }

    private void ConfigurarAtmosfera()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = config.colorAmbiente;
        RenderSettings.ambientIntensity = 0.4f;
        RenderSettings.reflectionIntensity = 0.25f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = config.colorFondo;
        RenderSettings.fogDensity = config.densidadNiebla;

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = config.colorFondo;
        }

        foreach (Light luz in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (luz.type == LightType.Directional)
            {
                luz.intensity = 0.08f;
                luz.color = new Color(0.35f, 0.45f, 0.6f);
            }
        }
    }

    private void CrearLuz(Transform padre, string nombre, Vector3 posicion, Color color)
    {
        GameObject objetoLuz = new GameObject(nombre);
        objetoLuz.transform.SetParent(padre, false);
        objetoLuz.transform.localPosition = posicion;

        Light luz = objetoLuz.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 11f;
        luz.intensity = config.intensidadLuces;
        luz.color = color;
        luz.shadows = LightShadows.None;
    }

    [ContextMenu("Validar spawns")]
    public void ValidarSpawns()
    {
        if (raizGenerada == null)
        {
            Debug.LogWarning("AURA: genera el búnker antes de validar los spawns.", this);
            return;
        }

        Physics.SyncTransforms();
        int errores = 0;

        if (!SpawnEsValido(SpawnJugador)) errores++;
        foreach (Transform punto in PuntosSpawnEnemigos)
        {
            if (!SpawnEsValido(punto)) errores++;
        }
        foreach (Transform punto in PuntosSpawnBoss)
        {
            if (!SpawnEsValido(punto)) errores++;
        }

        if (errores == 0)
        {
            Debug.Log("AURA: todos los spawns tienen suelo y espacio libre.", this);
        }
        else
        {
            Debug.LogWarning($"AURA: se encontraron {errores} spawns bloqueados o sin suelo.", this);
        }
    }

    private bool SpawnEsValido(Transform punto)
    {
        if (punto == null)
        {
            return false;
        }

        bool tieneSuelo = false;
        foreach (RaycastHit impacto in Physics.RaycastAll(punto.position + Vector3.up * 0.5f, Vector3.down, 1.5f))
        {
            if (impacto.transform.IsChildOf(raizGenerada))
            {
                tieneSuelo = true;
                break;
            }
        }

        Collider[] obstaculos = Physics.OverlapCapsule(
            punto.position + Vector3.up * 0.5f,
            punto.position + Vector3.up * 1.9f,
            0.45f
        );

        foreach (Collider obstaculo in obstaculos)
        {
            if (obstaculo.transform.IsChildOf(raizGenerada))
            {
                Debug.LogWarning($"AURA: {punto.name} está bloqueado por {obstaculo.name}.", punto);
                return false;
            }
        }

        if (!tieneSuelo)
        {
            Debug.LogWarning($"AURA: {punto.name} no tiene suelo generado debajo.", punto);
        }

        return tieneSuelo;
    }

    private void MoverJugador()
    {
        if (config == null || !config.moverJugadorAlSpawn || jugador == null || SpawnJugador == null)
        {
            return;
        }

        CharacterController controller = jugador.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        jugador.transform.SetPositionAndRotation(SpawnJugador.position, SpawnJugador.rotation);
        if (controller != null) controller.enabled = true;
    }

    private Transform CrearGrupo(string nombre, Transform padre)
    {
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);
        return grupo.transform;
    }

    private Transform CrearPunto(string nombre, Transform padre, Vector3 posicion)
    {
        Transform punto = CrearGrupo(nombre, padre);
        punto.localPosition = posicion;
        return punto;
    }

    private GameObject InstantiateBunkerElement(string nombre, GameObject prefab, Transform padre,
        Vector3 posicion, Vector3 escala, Material material, Quaternion rotacion)
    {
        GameObject elemento = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        elemento.name = nombre;
        elemento.transform.SetParent(padre, false);
        elemento.transform.localPosition = posicion;
        elemento.transform.localRotation = rotacion;
        elemento.transform.localScale = escala;

        if (material != null)
        {
            foreach (Renderer renderer in elemento.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        if (elemento.GetComponentInChildren<Collider>() == null)
        {
            elemento.AddComponent<BoxCollider>();
        }

        return elemento;
    }

    private void OnDrawGizmosSelected()
    {
        if (SpawnJugador != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SpawnJugador.position + Vector3.up * 0.9f, 0.5f);
        }

        Gizmos.color = Color.yellow;
        foreach (Transform punto in PuntosSpawnEnemigos)
        {
            if (punto != null) Gizmos.DrawSphere(punto.position + Vector3.up * 0.9f, 0.45f);
        }

        Gizmos.color = Color.red;
        foreach (Transform punto in PuntosSpawnBoss)
        {
            if (punto != null)
            {
                Gizmos.DrawSphere(punto.position + Vector3.up * 1.1f, 1.5f);
                Gizmos.DrawWireSphere(punto.position + Vector3.up * 1.1f, 2.5f);
            }
        }

        Gizmos.color = Color.cyan;
        foreach (RutaRampa ruta in rutasRampas)
        {
            Gizmos.DrawLine(ruta.inicio, ruta.fin);
            Gizmos.DrawWireSphere(ruta.inicio, 0.35f);
            Gizmos.DrawWireSphere(ruta.fin, 0.35f);
        }
    }
}
