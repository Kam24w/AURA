using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomEncounter : MonoBehaviour
{
    private List<GameObject> enemigosEnSala = new List<GameObject>();
    private List<GameObject> barrerasActivas = new List<GameObject>();
    private bool salaIniciada = false;
    private bool salaCompletada = false;

    // Configura la sala pasándole sus enemigos y los puntos/puertas de los pasillos conectados
    public void InicializarSala(List<GameObject> enemigos, List<Vector3> posicionesPuertas,
        Vector3 centroSala, float anchoPuerta, float alturaPuerta)
    {
        enemigosEnSala = enemigos;

        // Desactivar enemigos al inicio para que no ataquen antes de entrar
        foreach (GameObject enemigo in enemigosEnSala)
        {
            if (enemigo != null)
                enemigo.SetActive(false);
        }

        // Crear las barreras de energía azul en los pasillos pero mantenerlas ocultas hasta entrar
        foreach (Vector3 posPuerta in posicionesPuertas)
        {
            Vector3 diferencia = posPuerta - centroSala;
            bool puertaLateral = Mathf.Abs(diferencia.x) > Mathf.Abs(diferencia.z);
            GameObject barrera = CrearBarreraAzul(posPuerta, puertaLateral, anchoPuerta, alturaPuerta);
            barrera.SetActive(false);
            barrerasActivas.Add(barrera);
        }

        Debug.Log($"AURA: sala preparada con {enemigosEnSala.Count} enemigos y {barrerasActivas.Count} barreras.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController jugador = other.GetComponentInParent<PlayerController>();
        bool esJugador = jugador != null || other.CompareTag("Player");

        if (esJugador && !salaIniciada && !salaCompletada)
        {
            Debug.Log("AURA: jugador entró en una sala; activando encuentro.", this);
            EmpezarEncuentro();
        }
    }

    private void EmpezarEncuentro()
    {
        salaIniciada = true;

        // Activar barreras visuales para encerrar al jugador
        foreach (GameObject barrera in barrerasActivas)
        {
            if (barrera != null)
                barrera.SetActive(true);
        }

        // Activar los enemigos de la sala
        foreach (GameObject enemigo in enemigosEnSala)
        {
            if (enemigo != null)
                enemigo.SetActive(true);
        }

        // Iniciar el chequeo de enemigos vivos
        StartCoroutine(MonitorearEnemigos());
    }

    private IEnumerator MonitorearEnemigos()
    {
        while (!salaCompletada)
        {
            // Limpiar de la lista los enemigos que ya hayan sido destruidos (muertos)
            enemigosEnSala.RemoveAll(e => e == null);

            if (enemigosEnSala.Count == 0)
            {
                CompletarSala();
            }

            yield return new WaitForSeconds(0.5f); // Revisa cada medio segundo
        }
    }

    private void CompletarSala()
    {
        salaCompletada = true;

        // Quitar las barreras azules para dejar pasar al jugador
        foreach (GameObject barrera in barrerasActivas)
        {
            if (barrera != null)
                Destroy(barrera);
        }

        Debug.Log("¡Sala despejada! Puertas abiertas.");
    }

    private GameObject CrearBarreraAzul(Vector3 posicion, bool puertaLateral,
        float anchoPuerta, float alturaPuerta)
    {
        // Crear un cubo temporal que hará de muro de energía
        GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muro.name = "Barrera_Energia_Azul";
        muro.transform.position = posicion + Vector3.up * (alturaPuerta * 0.5f);
        float grosor = 0.35f;
        muro.transform.localScale = puertaLateral
            ? new Vector3(grosor, alturaPuerta, anchoPuerta)
            : new Vector3(anchoPuerta, alturaPuerta, grosor);

        // Ponerle un material azul semitransparente
        Renderer renderer = muro.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.6f, 1f, 0.4f); // Azul translúcido
            
            // Configurar transparencia si usa el Shader Standard
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            renderer.material = mat;
        }

        return muro;
    }
}