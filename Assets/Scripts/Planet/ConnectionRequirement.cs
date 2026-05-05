using System;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [Serializable]
    public class ConnectionRequirement
    {
        public Item item;
        [Min(1)] public int amount = 1;
    }
}
