using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Navegación")]
    public string escenaJuego = "SampleScene";

    [Header("Estilo")]
    public Color acento = new Color(0.78f, 0.63f, 0.36f, 1f);
    public Color textoPrincipal = new Color(0.91f, 0.92f, 0.9f, 1f);
    public Color textoSecundario = new Color(0.5f, 0.57f, 0.6f, 1f);
    public Color fondoPanel = new Color(0.035f, 0.05f, 0.065f, 0.97f);

    [Header("Audio")]
    public AudioClip ambiente;
    public AudioClip clic;
    [Range(0f, 1f)] public float volumenAmbiente = 0.25f;

    private Font fuente;
    private AudioSource audioSource;
    private GameObject panelPrincipal;
    private GameObject panelAjustes;
    private GameObject panelCreditos;
    private Button botonIniciar;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConfigurarAudio();
        CrearEventSystem();
        CrearInterfaz();
        MostrarPrincipal();
    }

    private void OnDisable()
    {
        GameSettings.Guardar();
    }

    public void IniciarJuego()
    {
        GameSettings.Guardar();
        SceneManager.LoadScene(escenaJuego);
    }

    public void MostrarPrincipal()
    {
        MostrarPanel(panelPrincipal);
        if (botonIniciar != null) botonIniciar.Select();
    }

    public void MostrarAjustes()
    {
        MostrarPanel(panelAjustes);
    }

    public void MostrarCreditos()
    {
        MostrarPanel(panelCreditos);
    }

    public void Salir()
    {
        GameSettings.Guardar();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void MostrarPanel(GameObject panel)
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(panel == panelPrincipal);
        if (panelAjustes != null) panelAjustes.SetActive(panel == panelAjustes);
        if (panelCreditos != null) panelCreditos.SetActive(panel == panelCreditos);
    }

    private void ConfigurarAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volumenAmbiente;
        audioSource.loop = true;
        audioSource.clip = ambiente;
        if (ambiente != null) audioSource.Play();
    }

    private void ReproducirClic()
    {
        if (clic != null) audioSource.PlayOneShot(clic, 0.45f);
    }

    private void CrearEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(transform, false);
    }

    private void CrearInterfaz()
    {
        GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CrearElemento("Panel de comandos", canvasObject.transform);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(680f, 0f);
        panel.AddComponent<Image>().color = fondoPanel;

        Image linea = CrearImagen("Acento lateral", panel.transform, acento);
        Colocar(linea.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 1080f));

        CrearTexto("Etiqueta", panel.transform, "AURA  /  TERMINAL DE INCURSIÓN", new Vector2(64f, -54f), new Vector2(550f, 28f), 16, acento, TextAnchor.MiddleLeft);
        CrearTexto("Título", panel.transform, "AURA", new Vector2(58f, -94f), new Vector2(550f, 116f), 92, textoPrincipal, TextAnchor.MiddleLeft);
        CrearTexto("Subtítulo", panel.transform, "EL VACÍO TIENE CONSCIENCIA", new Vector2(64f, -211f), new Vector2(550f, 34f), 17, textoSecundario, TextAnchor.MiddleLeft);
        Image separador = CrearImagen("Línea de sección", panel.transform, new Color(0.3f, 0.35f, 0.36f, 0.45f));
        Colocar(separador.rectTransform, new Vector2(0f, 1f), new Vector2(64f, -272f), new Vector2(520f, 1f));

        panelPrincipal = CrearPanel("Menú principal", panel.transform);
        botonIniciar = CrearBoton(panelPrincipal.transform, "INICIAR INCURSIÓN", new Vector2(64f, -330f), IniciarJuego, true);
        CrearBoton(panelPrincipal.transform, "CONFIGURACIÓN", new Vector2(64f, -410f), MostrarAjustes, false);
        CrearBoton(panelPrincipal.transform, "CRÉDITOS", new Vector2(64f, -490f), MostrarCreditos, false);
        CrearBoton(panelPrincipal.transform, "SALIR", new Vector2(64f, -570f), Salir, false);

        CrearAjustes(panel.transform);
        CrearCreditos(panel.transform);

        CrearTexto("Versión", panel.transform, $"BUILD {Application.version}  /  NÚCLEO ESTABLE", new Vector2(64f, -1000f), new Vector2(540f, 24f), 13, textoSecundario, TextAnchor.MiddleLeft);

        Text estado = CrearTexto("Estado del sistema", canvasObject.transform, "SISTEMA LISTO  /  ACCESO AUTORIZADO", Vector2.zero, new Vector2(480f, 28f), 14, acento, TextAnchor.MiddleRight);
        Colocar(estado.rectTransform, new Vector2(1f, 1f), new Vector2(-56f, -48f), estado.rectTransform.sizeDelta);

        Text protocolo = CrearTexto("Protocolo", canvasObject.transform, "BÚNKER PROCEDURAL\nPROTOCOLO DE CONTENCIÓN 03", Vector2.zero, new Vector2(480f, 64f), 15, textoPrincipal, TextAnchor.LowerRight);
        Colocar(protocolo.rectTransform, new Vector2(1f, 0f), new Vector2(-56f, 52f), protocolo.rectTransform.sizeDelta);
    }

    private void CrearAjustes(Transform padre)
    {
        panelAjustes = CrearPanel("Configuración", padre);
        CrearTexto("Título", panelAjustes.transform, "CONFIGURACIÓN", new Vector2(64f, -318f), new Vector2(540f, 54f), 34, textoPrincipal, TextAnchor.MiddleLeft);

        Text valorVolumen = CrearTexto("Valor volumen", panelAjustes.transform, "", new Vector2(494f, -403f), new Vector2(90f, 28f), 17, acento, TextAnchor.MiddleRight);
        CrearTexto("Volumen", panelAjustes.transform, "VOLUMEN GENERAL", new Vector2(64f, -403f), new Vector2(390f, 28f), 17, textoSecundario, TextAnchor.MiddleLeft);
        Slider volumen = CrearSlider(panelAjustes.transform, new Vector2(64f, -449f), GameSettings.VolumenGeneral, 0f, 1f);
        volumen.onValueChanged.AddListener(valor =>
        {
            GameSettings.EstablecerVolumen(valor);
            valorVolumen.text = $"{Mathf.RoundToInt(valor * 100f)}%";
        });
        valorVolumen.text = $"{Mathf.RoundToInt(GameSettings.VolumenGeneral * 100f)}%";

        Text valorSensibilidad = CrearTexto("Valor sensibilidad", panelAjustes.transform, "", new Vector2(494f, -529f), new Vector2(90f, 28f), 17, acento, TextAnchor.MiddleRight);
        CrearTexto("Sensibilidad", panelAjustes.transform, "SENSIBILIDAD DEL RATÓN", new Vector2(64f, -529f), new Vector2(390f, 28f), 17, textoSecundario, TextAnchor.MiddleLeft);
        Slider sensibilidad = CrearSlider(panelAjustes.transform, new Vector2(64f, -575f), GameSettings.SensibilidadMouse, 0.5f, 8f);
        sensibilidad.onValueChanged.AddListener(valor =>
        {
            GameSettings.EstablecerSensibilidad(valor);
            valorSensibilidad.text = valor.ToString("0.0");
        });
        valorSensibilidad.text = GameSettings.SensibilidadMouse.ToString("0.0");

        CrearBoton(panelAjustes.transform, "VOLVER", new Vector2(64f, -684f), MostrarPrincipal, false);
    }

    private void CrearCreditos(Transform padre)
    {
        panelCreditos = CrearPanel("Créditos", padre);
        CrearTexto("Título", panelCreditos.transform, "CRÉDITOS", new Vector2(64f, -318f), new Vector2(540f, 54f), 34, textoPrincipal, TextAnchor.MiddleLeft);
        Text contenido = CrearTexto("Contenido", panelCreditos.transform,
            "AURA\nProyecto de videojuego\n\nExplora un búnker procedural, sobrevive a sus unidades de contención y desciende a los niveles inferiores.\n\nDiseño, programación y efectos de sonido\nProyecto AURA\n\nMotor\nUnity 6",
            new Vector2(64f, -394f), new Vector2(520f, 360f), 18, textoSecundario, TextAnchor.UpperLeft);
        contenido.horizontalOverflow = HorizontalWrapMode.Wrap;
        contenido.verticalOverflow = VerticalWrapMode.Overflow;
        CrearBoton(panelCreditos.transform, "VOLVER", new Vector2(64f, -770f), MostrarPrincipal, false);
    }

    private Button CrearBoton(Transform padre, string etiqueta, Vector2 posicion, UnityEngine.Events.UnityAction accion, bool primario)
    {
        GameObject objeto = CrearElemento(etiqueta, padre);
        Image imagen = objeto.AddComponent<Image>();
        imagen.color = primario ? new Color(0.22f, 0.2f, 0.14f, 1f) : new Color(0.075f, 0.105f, 0.125f, 1f);

        Button boton = objeto.AddComponent<Button>();
        boton.targetGraphic = imagen;
        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1.25f, 1.2f, 1.05f, 1f);
        colores.pressedColor = acento;
        colores.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colores.fadeDuration = 0.08f;
        boton.colors = colores;
        boton.onClick.AddListener(ReproducirClic);
        boton.onClick.AddListener(accion);

        RectTransform rect = (RectTransform)objeto.transform;
        Colocar(rect, new Vector2(0f, 1f), posicion, new Vector2(520f, 64f));

        Image marca = CrearImagen("Marca", objeto.transform, primario ? acento : new Color(0.35f, 0.4f, 0.42f, 1f));
        Colocar(marca.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(4f, 64f));
        CrearTexto("Texto", objeto.transform, etiqueta, new Vector2(24f, 0f), new Vector2(450f, 64f), 21, textoPrincipal, TextAnchor.MiddleLeft);
        return boton;
    }

    private Slider CrearSlider(Transform padre, Vector2 posicion, float valor, float minimo, float maximo)
    {
        GameObject objeto = CrearElemento("Control deslizante", padre);
        RectTransform rect = (RectTransform)objeto.transform;
        Colocar(rect, new Vector2(0f, 1f), posicion, new Vector2(520f, 28f));

        Image fondo = CrearImagen("Fondo", objeto.transform, new Color(0.1f, 0.14f, 0.16f, 1f));
        Colocar(fondo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 5f));

        GameObject area = CrearElemento("Área de relleno", objeto.transform);
        RectTransform areaRect = (RectTransform)area.transform;
        areaRect.anchorMin = new Vector2(0f, 0.5f);
        areaRect.anchorMax = new Vector2(1f, 0.5f);
        areaRect.offsetMin = new Vector2(0f, -2.5f);
        areaRect.offsetMax = new Vector2(0f, 2.5f);
        Image relleno = CrearImagen("Relleno", area.transform, acento);
        relleno.rectTransform.anchorMin = Vector2.zero;
        relleno.rectTransform.anchorMax = Vector2.one;
        relleno.rectTransform.offsetMin = Vector2.zero;
        relleno.rectTransform.offsetMax = Vector2.zero;

        Image manipulador = CrearImagen("Manipulador", objeto.transform, textoPrincipal);
        Colocar(manipulador.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 24f));

        Slider slider = objeto.AddComponent<Slider>();
        slider.fillRect = relleno.rectTransform;
        slider.handleRect = manipulador.rectTransform;
        slider.targetGraphic = manipulador;
        slider.minValue = minimo;
        slider.maxValue = maximo;
        slider.value = valor;
        return slider;
    }

    private Text CrearTexto(string nombre, Transform padre, string contenido, Vector2 posicion, Vector2 tamaño, int tamañoFuente, Color color, TextAnchor alineacion)
    {
        GameObject objeto = CrearElemento(nombre, padre);
        Text texto = objeto.AddComponent<Text>();
        texto.font = fuente;
        texto.text = contenido;
        texto.fontSize = tamañoFuente;
        texto.color = color;
        texto.alignment = alineacion;
        texto.raycastTarget = false;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        Colocar(texto.rectTransform, new Vector2(0f, 1f), posicion, tamaño);
        return texto;
    }

    private Image CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject objeto = CrearElemento(nombre, padre);
        Image imagen = objeto.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private GameObject CrearPanel(string nombre, Transform padre)
    {
        GameObject panel = CrearElemento(nombre, padre);
        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private static GameObject CrearElemento(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        return objeto;
    }

    private static void Colocar(RectTransform rect, Vector2 ancla, Vector2 posicion, Vector2 tamaño)
    {
        rect.anchorMin = ancla;
        rect.anchorMax = ancla;
        rect.pivot = ancla;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamaño;
    }
}
