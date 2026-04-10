using UnityEngine;

// ─── Enums ────────────────────────────────────────────────────────────────────

public enum ResourceType
{
    // Mining
    Ore, Stone, Crystal, RareMetals,
    // Organic
    Wood, Biomass, HealingPlants, Resin,
    // Ice
    Water, Ice, CryoGas, Fossils,
    // Gas
    Gas, Energy, Plasma, EtherealVapor,
    // Volcanic
    Magma, Ash, Obsidian, GeothermalEnergy,
}

public enum PlanetType
{
    Mining, Organic, Ice, Gas, Volcanic, Abandoned
}

public enum ConnectionType
{
    Ancient,     // skrivena drevna veza
    PlayerBuilt  // igrač je izgradio
}

public enum ArtifactType
{
    Blueprint,       // otključava nacrte
    Amplifier,       // pojačivač za veze/strojeve
    NetworkFragment, // otkriva drevne veze u okolici
    EnergyCore,      // jednokratni boost
    Paired           // parnjak — treba kombinirati s drugim
}

public enum MachineState
{
    Active, Full, Broken
}

public enum HubLevel
{
    Level1, Level2, Level3
}

public enum MilestoneType
{
    EarlyProgress, MidProgress, LateProgress
}

// ─── Event Data Structs ───────────────────────────────────────────────────────

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
    public Transform From;
    public Transform To;
}

public struct ConnectionHealthChangedEvent
{
    public Transform PlanetA;
    public Transform PlanetB;
    public ConnectionType ConnectionType;
    public float Health; // 0–1
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
    public Transform Planet;
    public MachineState State;
}

public struct TransportRouteEvent
{
    public ResourceType Type;
    public int AmountPerCycle;
    public Transform From;
    public Transform To;
    public float IntervalSeconds;
}
