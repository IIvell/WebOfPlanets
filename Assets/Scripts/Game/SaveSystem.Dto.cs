using System;
using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    // DTO shema save datoteke (JsonUtility). Odvojena od capture/restore logike
    // (SaveSystem.Capture.cs / SaveSystem.Restore.cs) — imena polja SU format
    // datoteke, pa se ovdje ništa ne preimenuje bez migracije.
    public static partial class SaveSystem
    {
        [Serializable] public class ItemCountSave { public string item; public int count; }

        [Serializable] public class PlanetSave
        {
            public string name;
            public Vector3 position;
            public float scale;
            public float gravity;
            public int type;
        }

        [Serializable] public class ConnectionSave
        {
            public string planetA;
            public string planetB;
            public int type;
            public float health;
        }

        [Serializable] public class SlotSave { public int index; public string item; public int durability; }

        [Serializable] public class ResourceSave
        {
            public string item;
            public string planet;
            public Vector3 position;
            public Quaternion rotation;
            public bool pickup;
        }

        [Serializable] public class MachineSave
        {
            public int kind;
            public string data;
            public string planet;
            public Vector3 position;
            public Quaternion rotation;
            public float scale;
            public string linkedPlanet;   // collector: cilj transporta
            public int linkedIndex = -1;  // collector→storage / teleporter par (indeks u machines listi)
            public bool totemActive;
            public bool broken;
            public List<ItemCountSave> stored = new();  // storage/collector/extractor/uplink buffer; smelter INPUT
            public List<ItemCountSave> storedB = new(); // smelter OUTPUT
        }

        [Serializable] public class SaveData
        {
            public int hubTier;
            public float playerHealth;
            public string playerPlanet;
            public Vector3 playerPosition;
            public Quaternion playerRotation;
            public int selectedSlot = -1;
            public List<PlanetSave> planets = new();
            public List<ConnectionSave> connections = new();
            public List<ItemCountSave> hubStorage = new();
            public List<ItemCountSave> inventory = new();
            public List<SlotSave> quickSlots = new();
            public List<MachineSave> machines = new();
            public List<ResourceSave> resources = new();
        }
    }
}
