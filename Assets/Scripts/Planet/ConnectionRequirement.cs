using System;
using UnityEngine;

namespace WebOfPlanets
{
    [Serializable]
    public class ConnectionRequirement
    {
        public Item item;
        [Min(1)] public int amount = 1;
    }
}
