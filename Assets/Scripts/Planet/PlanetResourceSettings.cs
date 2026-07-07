using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Planets/Resource Settings")]
    public class PlanetResourceSettings : ScriptableObject
    {
        [System.Serializable]
        public class ResourceEntry
        {
            public Item item;
            [Min(0f)] public float minDensity = 0.1f;
            [Min(0f)] public float maxDensity = 0.2f;
            [Tooltip("Vjerojatnost da se pojedina instanca spawna kao pickup verzija (bez timera) umjesto mining verzije.")]
            [Range(0f, 1f)] public float pickupChance = 0.5f;
        }

        [System.Serializable]
        public class PlanetTypeConfig
        {
            public PlanetType planetType;
            public List<ResourceEntry> resources = new();
        }

        public List<PlanetTypeConfig> configs = new();

        public PlanetTypeConfig GetConfig(PlanetType type)
        {
            foreach (var c in configs)
                if (c.planetType == type) return c;
            return null;
        }
    }
}
