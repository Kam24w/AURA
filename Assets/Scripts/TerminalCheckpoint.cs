using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TerminalCheckpoint : MonoBehaviour
{
    [Min(1)] public int piso = 1;
    public int semilla;
    public Color colorIndicador = new Color(0.1f, 0.75f, 1f, 1f);

    private bool guardado;

    private void Awake()
    {
        Collider colisionador = GetComponent<Collider>();
        colisionador.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController jugador = other.GetComponentInParent<PlayerController>();
        if (jugador == null || guardado)
        {
            return;
        }

        guardado = true;
        PlayerCheckpoint.Guardar(piso, semilla);

        PlayerHealth salud = jugador.GetComponent<PlayerHealth>();
        if (salud != null)
        {
            salud.RestaurarSaludCompleta();
        }

        ActivarIndicador();
    }

    private void ActivarIndicador()
    {
        Renderer[] renderizadores = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderizador in renderizadores)
        {
            if (renderizador.material != null)
            {
                renderizador.material.color = colorIndicador;
            }
        }
    }
}