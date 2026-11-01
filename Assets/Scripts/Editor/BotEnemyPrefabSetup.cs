using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Utilidad de editor: genera Assets/Prefabs/Bot_Enemy_Common.prefab a partir
/// de Bot_animado.fbx y bot_Animation.controller, con la estructura que espera
/// la IA (raíz con NavMeshAgent + salud + collider, modelo hijo con Animator).
/// Menú: AURA > Crear prefab Bot_Enemy_Common.
/// </summary>
public static class BotEnemyPrefabSetup
{
    private const string RutaPrefab = "Assets/Prefabs/Bot_Enemy_Common.prefab";

    [MenuItem("AURA/Crear prefab Bot_Enemy_Common")]
    public static void CrearPrefab()
    {
        GameObject modelo = BuscarAsset<GameObject>("Bot_animado", ".fbx");
        RuntimeAnimatorController controller = BuscarAsset<RuntimeAnimatorController>("bot_Animation", ".controller");

        if (modelo == null || controller == null)
        {
            Debug.LogError("AURA: no se encontró Bot_animado.fbx o bot_Animation.controller en el proyecto.");
            return;
        }

        GameObject raiz = new GameObject("Bot_Enemy_Common");

        NavMeshAgent agente = raiz.AddComponent<NavMeshAgent>();
        agente.speed = 4.5f;
        agente.angularSpeed = 360f;
        agente.acceleration = 12f;
        agente.stoppingDistance = 1.2f;
        agente.radius = 0.4f;
        agente.height = 2f;

        CapsuleCollider colision = raiz.AddComponent<CapsuleCollider>();
        colision.height = 2f;
        colision.radius = 0.4f;
        colision.center = new Vector3(0f, 1f, 0f);

        EnemyHealth salud = raiz.AddComponent<EnemyHealth>();
        salud.maxHealth = 100f;

        EnemyController ia = raiz.AddComponent<EnemyController>();

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        visual.name = "Modelo";
        visual.transform.SetParent(raiz.transform, false);

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
        {
            animator = visual.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        visual.AddComponent<BotAnimationEventRelay>();
        ia.animator = animator;

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);

        Debug.Log($"AURA: prefab creado en {RutaPrefab}. Asígnalo al campo Prefab Enemigo de BunkerSettings_Normal. " +
                  "Ajusta los offsets Muzzle_L/R en el prefab a las puntas de los cañones y revisa " +
                  "capasObstaculos para la línea de visión.");
    }

    private static T BuscarAsset<T>(string nombre, string extension) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (ruta.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase)
                && System.IO.Path.GetFileNameWithoutExtension(ruta) == nombre)
            {
                return AssetDatabase.LoadAssetAtPath<T>(ruta);
            }
        }

        return null;
    }
}
