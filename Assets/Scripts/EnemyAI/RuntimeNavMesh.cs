using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hornea un NavMesh en tiempo de ejecución a partir de los colliders del
/// búnker generado, usando el módulo AI nativo de Unity (sin paquetes extra).
/// Se llama una vez tras generar el mapa, antes de colocar enemigos.
/// </summary>
public static class RuntimeNavMesh
{
    public static NavMeshDataInstance Hornear(Transform raiz, Bounds limites)
    {
        NavMeshBuildSettings ajustes = NavMesh.GetSettingsByID(0);
        ajustes.agentClimb = 0.3f;
        ajustes.agentSlope = 40f;

        List<NavMeshBuildSource> fuentes = new List<NavMeshBuildSource>();
        List<NavMeshBuildMarkup> marcas = new List<NavMeshBuildMarkup>();
        NavMeshBuilder.CollectSources(raiz, ~0, NavMeshCollectGeometry.PhysicsColliders,
            0, marcas, fuentes);

        NavMeshData datos = NavMeshBuilder.BuildNavMeshData(ajustes, fuentes,
            limites, Vector3.zero, Quaternion.identity);

        return NavMesh.AddNavMeshData(datos);
    }
}
