using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class BossPrefabSetup
{
    private const string SettingsPath = "Assets/Settings/BunkerSettings_Normal.asset";

    private static readonly BossDefinition[] Definitions =
    {
        new BossDefinition(
            "Boss_Piso1",
            "Assets/Models/jefe_1.fbx",
            "Assets/Animations/boss_1_animation.controller",
            1,
            "Assets/Prefabs/Boss_Piso1.prefab"),
        new BossDefinition(
            "Boss_Piso2",
            "Assets/Models/Jefe_2.fbx",
            "Assets/Animations/boss_2_animation.controller",
            2,
            "Assets/Prefabs/Boss_Piso2.prefab"),
        new BossDefinition(
            "Boss_Piso3",
            "Assets/Models/tripo_convert_fcf1ef26-da10-4d2e-af26-f98ef07e15ca.fbx",
            "Assets/Animations/boss_3_animation.controller",
            3,
            "Assets/Prefabs/Boss_Piso3.prefab")
    };

    [MenuItem("AURA/Crear prefabs de bosses por piso")]
    public static void CrearPrefabs()
    {
        BunkerSettingsSO settings = AssetDatabase.LoadAssetAtPath<BunkerSettingsSO>(SettingsPath);
        if (settings == null)
        {
            Debug.LogError($"AURA: no se encontró la configuración {SettingsPath}.");
            return;
        }

        GameObject[] prefabs = new GameObject[Definitions.Length];
        for (int i = 0; i < Definitions.Length; i++)
        {
            prefabs[i] = CrearPrefab(Definitions[i]);
            if (prefabs[i] == null)
            {
                return;
            }
        }

        settings.prefabBossPiso1 = prefabs[0];
        settings.prefabBossPiso2 = prefabs[1];
        settings.prefabBossPiso3 = prefabs[2];
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("AURA: prefabs de boss creados y asignados a BunkerSettings_Normal.");
    }

    private static GameObject CrearPrefab(BossDefinition definition)
    {
        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(definition.ModelPath);
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(definition.ControllerPath);
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(definition.ModelPath);

        if (modelo == null || controller == null || avatar == null)
        {
            Debug.LogError(
                $"AURA: no se pudo cargar el modelo, Avatar o controller del piso {definition.Floor}: " +
                $"{definition.ModelPath} / {definition.ControllerPath}.");
            return null;
        }

        GameObject raiz = new GameObject(definition.Name);
        NavMeshAgent agente = raiz.AddComponent<NavMeshAgent>();
        agente.speed = 4.5f;
        agente.angularSpeed = 360f;
        agente.acceleration = 12f;
        agente.stoppingDistance = 1.2f;
        agente.radius = 0.4f;
        agente.height = 2f;

        CapsuleCollider collider = raiz.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.4f;
        collider.center = new Vector3(0f, 1f, 0f);

        EnemyHealth health = raiz.AddComponent<EnemyHealth>();
        health.maxHealth = 1000f;

        BossController boss = raiz.AddComponent<BossController>();
        boss.pisoActual = definition.Floor;

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        visual.name = "Modelo";
        visual.transform.SetParent(raiz.transform, false);

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = visual.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = false;
        boss.animator = animator;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, definition.PrefabPath);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    private sealed class BossDefinition
    {
        public readonly string Name;
        public readonly string ModelPath;
        public readonly string ControllerPath;
        public readonly int Floor;
        public readonly string PrefabPath;

        public BossDefinition(string name, string modelPath, string controllerPath,
            int floor, string prefabPath)
        {
            Name = name;
            ModelPath = modelPath;
            ControllerPath = controllerPath;
            Floor = floor;
            PrefabPath = prefabPath;
        }
    }
}
