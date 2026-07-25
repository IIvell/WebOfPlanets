using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Zajednička baza za sve stvari koje mogu sjediti u hotbar slotu (alati, strojevi).
    public abstract class QuickSlotItem : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
    }
}
