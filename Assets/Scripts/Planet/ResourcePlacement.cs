using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Zajednički završni korak spawna resursa na površinu planeta — svjež spawn
    // (ResourceSpawnManager), hub dekor (HubResourceSpawner) i povratak iz save
    // datoteke idu istim putem. Izbor TOČKE ostaje na pozivateljima jer se
    // namjerno razlikuje: hub izbjegava zonu baze (i preskače spawn ako ne
    // uspije), regularni spawn bježi od totema veza (i spawna svejedno).
    internal static class ResourcePlacement
    {
        // surfacePoint mora biti točka na površini; groundNormal je normala tla za
        // prizemljenje. isPickup bira prefab/skalu i prosljeđuje se interactableu.
        public static GameObject Spawn(Item item, bool isPickup, Transform planet,
            Vector3 surfacePoint, Vector3 groundNormal, Quaternion rotation, float surfaceGap)
        {
            GameObject prefab = isPickup ? item.pickupPrefab : item.miningPrefab;
            if (prefab == null) return null;

            GameObject go = Object.Instantiate(prefab, surfacePoint, rotation);
            go.name = item.displayName;
            go.transform.localScale = isPickup ? item.pickupWorldScale : item.miningWorldScale;

            // Bezuvjetno prizemljenje po stvarnoj geometriji: prije se korigiralo
            // samo uz pivotAtMeshCenter flag, pa su modeli s drugačijim pivotom
            // lebdjeli ili upadali u planet.
            SurfacePlacement.GroundToSurface(go, planet, surfacePoint, groundNormal, surfaceGap);

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Object.Destroy(rb);

            // Prefab bez collidera na rootu: box po stvarnoj geometriji umjesto
            // default 1x1x1 kocke na pivotu — 'Ice' (skinned fridge bez collidera)
            // je s default kockom imao collider pomaknut od vizuala.
            if (!go.TryGetComponent<Collider>(out _))
                SurfacePlacement.FitBoxColliderToGeometry(go);

            if (!go.TryGetComponent<ItemInteractable>(out var interactable))
                interactable = go.AddComponent<ItemInteractable>();
            interactable.Init(item, isPickup);

            return go;
        }
    }
}
