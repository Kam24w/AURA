using UnityEngine;

public static class PlayerCheckpoint
{
    public static bool TieneCheckpoint { get; private set; }
    public static int PisoGuardado { get; private set; } = 1;
    public static int SemillaGuardada { get; private set; }

    public static void IniciarPartida(int semilla)
    {
        TieneCheckpoint = true;
        PisoGuardado = 1;
        SemillaGuardada = semilla;
    }

    public static void Guardar(int piso, int semilla)
    {
        if (piso <= PisoGuardado && TieneCheckpoint)
        {
            return;
        }

        TieneCheckpoint = true;
        PisoGuardado = Mathf.Clamp(piso, 1, 3);
        SemillaGuardada = semilla;
        Debug.Log($"AURA: checkpoint guardado en la terminal del piso {PisoGuardado}.");
    }

    public static void ReiniciarPartida()
    {
        TieneCheckpoint = false;
        PisoGuardado = 1;
        SemillaGuardada = 0;
    }
}