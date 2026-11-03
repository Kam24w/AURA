using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class FloorPuzzle : MonoBehaviour
{
    [Min(1)] public int piso = 1;
    [Min(2)] public int pasosNecesarios = 3;
    [Min(1f)] public float tiempoEntrePasos = 4f;
    public Color colorBloqueado = new Color(0.9f, 0.1f, 0.05f);
    public Color colorResuelto = new Color(0.1f, 0.9f, 0.35f);

    private int pasos;
    private float ultimoPaso = -Mathf.Infinity;
    private bool resuelto;
    private Text mensaje;
    private Renderer indicador;
    private GameObject barrera;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        CrearInterfaz();
        ActualizarMensaje("PISO " + piso + "  -  TERMINAL BLOQUEADA\nAcércate y pulsa E tres veces");
    }

    private void Update()
    {
        if (resuelto || mensaje == null || !mensaje.gameObject.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Time.time - ultimoPaso > tiempoEntrePasos)
            {
                pasos = 0;
            }

            pasos++;
            ultimoPaso = Time.time;

            if (pasos >= pasosNecesarios)
            {
                Resolver();
            }
            else
            {
                ActualizarMensaje($"SECUENCIA {pasos}/{pasosNecesarios}\nContinúa pulsando E");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null && !resuelto)
        {
            mensaje.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null && mensaje != null)
        {
            mensaje.gameObject.SetActive(false);
        }
    }

    public void Configurar(Renderer indicadorTerminal, GameObject barreraSalida)
    {
        indicador = indicadorTerminal;
        barrera = barreraSalida;
        CambiarColor(colorBloqueado);
    }

    private void Resolver()
    {
        resuelto = true;
        if (barrera != null)
        {
            barrera.SetActive(false);
        }

        CambiarColor(colorResuelto);
        ActualizarMensaje("PUZZLE RESUELTO\nSALIDA DESBLOQUEADA");
        BunkerGenerator generador = FindAnyObjectByType<BunkerGenerator>();
        if (generador != null)
        {
            PlayerCheckpoint.Guardar(piso, generador.SemillaUsada);
        }
    }

    private void CambiarColor(Color color)
    {
        if (indicador != null)
        {
            indicador.material.color = color;
        }
    }

    private void CrearInterfaz()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(
                "PuzzleCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject textObject = new GameObject("PuzzleMessage", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvas.transform, false);
        mensaje = textObject.GetComponent<Text>();
        mensaje.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mensaje.fontSize = 26;
        mensaje.fontStyle = FontStyle.Bold;
        mensaje.color = Color.white;
        mensaje.alignment = TextAnchor.MiddleCenter;
        mensaje.horizontalOverflow = HorizontalWrapMode.Overflow;
        mensaje.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = mensaje.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 90f);
        rect.sizeDelta = new Vector2(700f, 90f);
        mensaje.gameObject.SetActive(false);
    }

    private void ActualizarMensaje(string texto)
    {
        if (mensaje != null)
        {
            mensaje.text = texto;
        }
    }
}