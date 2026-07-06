using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    internal static class SurfacePlacement
    {
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
