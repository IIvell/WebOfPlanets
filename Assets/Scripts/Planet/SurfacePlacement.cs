using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    internal static class SurfacePlacement
    {
        // Jedinstveni izračun radijusa planeta: za primitivne sfere (PlanetCreator)
        // isto kao localScale.x * 0.5, a za mesh planete (Hub, Planet.fbx) localScale
        // laže pa se čita iz renderer boundsa.
        public static float GetPlanetRadius(Transform planet)
        {
            Renderer rend = planet.GetComponentInChildren<Renderer>();
            return rend != null ? rend.bounds.size.x * 0.5f : planet.localScale.x * 0.5f;
        }

        // Raycast can otherwise hit a previously spawned resource's collider instead of
        // the planet, so only accept hits against the planet's own collider.
        public static bool TryRaycastSurface(Transform planet, Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance);
            float closestDistance = float.MaxValue;
            RaycastHit closestHit = default;
            bool found = false;

            foreach (var h in hits)
            {
                if (h.collider.transform != planet) continue;
                if (h.distance >= closestDistance) continue;
                closestDistance = h.distance;
                closestHit = h;
                found = true;
            }

            hit = closestHit;
            return found;
        }
    }
}
