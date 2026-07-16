using UnityEditor;
using UnityEngine;
using xyz.germanfica.unity.planet.gravity;

// Play mode dijagnostika: ispiše svaki spawnani objekt koji lebdi iznad ili je
// utonuo u površinu planete, s izmjerenim razmakom.
public static class SurfacePlacementAuditMenu
{
    [MenuItem("Tools/Web of Planets/Audit Surface Placement")]
    private static void Run()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("SurfaceAudit radi samo u Play modu.");
            return;
        }

        SurfaceAudit.LogReport();
    }
}
