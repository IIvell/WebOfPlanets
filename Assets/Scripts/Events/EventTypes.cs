using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public enum ResourceType { Ore, Crystal, Biomass, Ice, Gas, VolcanicMatter }
    public enum PlanetType { Mining, Organic, Ice, Gaseous, Volcanic, Abandoned }
    public enum ConnectionType { Ancient, Weak, Mid, Strong }
    public enum ArtifactType { Blueprint, Enhancer, NetworkFragment, EnergyCore, PairedArtifact }
    public enum MachineState { Active, Idle, Broken }
    public enum HubLevel { Basic, Upgraded, Advanced }
    public enum MilestoneType { FirstResource, FirstConnection, FirstArtifact, HubUpgraded, NetworkComplete }

    public struct ResourceCollectedEvent
    {
        public ResourceType Type;
        public int Amount;
        public Transform Planet;
    }

    public struct ResourceTransportedEvent
    {
        public ResourceType Type;
        public int Amount;
        public Transform FromPlanet;
        public Transform ToPlanet;
    }

    public struct ConnectionHealthChangedEvent
    {
        public float Health; // 0–100
        public Transform PlanetA;
        public Transform PlanetB;
        public ConnectionType ConnectionType;
    }

    public struct ConnectionEvent
    {
        public Transform PlanetA;
        public Transform PlanetB;
        public ConnectionType ConnectionType;
    }

    public struct ArtifactEvent
    {
        public ArtifactType Type;
        public Transform Planet;
    }

    public struct MachineEvent
    {
        public MachineState State;
        public Transform Planet;
        public ResourceType ResourceType;
    }

    public struct TransportRouteEvent
    {
        public Transform FromPlanet;
        public Transform ToPlanet;
        public ResourceType ResourceType;
    }

    public struct PlayerPlanetEvent
    {
        public Transform Planet;
        public Vector3 Position;
    }

    public struct PlayerTeleportEvent
    {
        public Transform FromPlanet;
        public Transform ToPlanet;
        public bool UsedConnection;
        public int ResourceCost;
    }

    public struct HubUpgradedEvent
    {
        public HubLevel NewLevel;
        public string UpgradeType;
    }

    public struct MilestoneEvent
    {
        public MilestoneType Type;
        public string StoryFragment;
    }

    public struct MiningProgressEvent
    {
        public float Progress; // 0–1
        public bool IsMining;
    }

    public struct ToolEquippedEvent
    {
        public string ToolName;       // null ili prazan = odložen alat
        public float SpeedMultiplier;
        public int CurrentDurability;
        public int MaxDurability;
    }

    public struct ToolDurabilityEvent
    {
        public int Current;
        public int Max;
    }
}
