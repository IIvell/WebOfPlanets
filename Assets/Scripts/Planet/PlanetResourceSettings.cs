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
            [Min(1)] public int minCount = 2;
            [Min(1)] public int maxCount = 5;
            public Color fallbackColor = Color.gray;
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
