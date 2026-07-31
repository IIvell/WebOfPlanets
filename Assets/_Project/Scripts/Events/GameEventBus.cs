using System;
using UnityEngine;

namespace WebOfPlanets
{
    // Središnja statična sabirnica događaja. NAPOMENA O REZERVIRANIM EVENTIMA
    // (odluka iz audita 14.7.2026. — ne brisati): eventi za buduće featuree T5/T6
    // trenutno nemaju publishera i/ili subscribera, ali su namjerno ostavljeni kao
    // dogovoreno sučelje: ancient veze (OnAncientConnectionDiscovered/Activated),
    // sekundarni hub (OnSecondaryHubCreated), transportne rute (OnTransportRoute
    // Created/Removed), slijetanje (OnPlayerLandedOnPlanet/OnPlayerLeftPlanet),
    // resursi (OnResourceCollected/OnResourceTransported), hub nadogradnje
    // (OnHubUpgraded), story lanac (OnMilestoneReached/OnStoryFragmentUnlocked)
    // te OnToolEquipped. OnMachineRepaired se diže, subscriber je rezerviran.
    public static class GameEventBus
    {
        // ── Player ────────────────────────────────────────────────────────────
        public static event Action<PlayerPlanetEvent>   OnPlayerLandedOnPlanet;
        public static event Action<PlayerPlanetEvent>   OnPlayerLeftPlanet;
        public static event Action<PlayerTeleportEvent> OnPlayerTeleported;

        // ── Resources ─────────────────────────────────────────────────────────
        public static event Action<ResourceCollectedEvent>  OnResourceCollected;
        public static event Action<ResourceTransportedEvent> OnResourceTransported;
        public static event Action<ResourceType>            OnStorageFull;
        public static event Action<TransportRouteEvent>     OnTransportRouteCreated;
        public static event Action<TransportRouteEvent>     OnTransportRouteRemoved;

        // ── Network / Connections ─────────────────────────────────────────────
        public static event Action<ConnectionEvent>              OnConnectionCreated;
        public static event Action<ConnectionEvent>              OnConnectionDestroyed;
        public static event Action<ConnectionHealthChangedEvent> OnConnectionHealthChanged;
        public static event Action<ConnectionHealthChangedEvent> OnConnectionCritical;
        public static event Action<ConnectionEvent>              OnAncientConnectionDiscovered;
        public static event Action<ConnectionEvent>              OnAncientConnectionActivated;

        // ── Planets ───────────────────────────────────────────────────────────
        public static event Action<Transform> OnPlanetDiscovered;
        public static event Action<Transform> OnSecondaryHubCreated;

        // ── Hub ───────────────────────────────────────────────────────────────
        public static event Action<HubUpgradedEvent> OnHubUpgraded;
        public static event Action<int>              OnRecipeTierUnlocked;

        // ── Machines ──────────────────────────────────────────────────────────
        public static event Action<MachineEvent> OnMachinePlaced;
        public static event Action<MachineEvent> OnMachineBroken;
        public static event Action<MachineEvent> OnMachineRepaired;

        // ── Game State ────────────────────────────────────────────────────────
        public static event Action<MilestoneEvent> OnMilestoneReached;
        public static event Action<string>         OnStoryFragmentUnlocked;

        // ── Mining ────────────────────────────────────────────────────────────
        public static event Action<MiningProgressEvent> OnMiningProgress;

        // ── Tools ─────────────────────────────────────────────────────────────
        public static event Action<ToolEquippedEvent>    OnToolEquipped;
        public static event Action<ToolDurabilityEvent>  OnToolDurabilityChanged;

        // ── Quick Slots / Inventar ────────────────────────────────────────────
        public static event Action OnQuickSlotsChanged;
        // Resurs ušao u inventar igrača (kopanje, preuzimanje iz strojeva) — bez
        // payloada: jedini potrošač (AudioManager) treba samo činjenicu ulaska.
        public static event Action OnInventoryItemAdded;

        // ── Player Health ─────────────────────────────────────────────────────
        public static event Action<PlayerHealthChangedEvent> OnPlayerHealthChanged;
        public static event Action<PlayerDamagedEvent>       OnPlayerDamaged;
        public static event Action<PlayerDiedEvent>          OnPlayerDied;

        // ── Raise overloads ───────────────────────────────────────────────────
        public static void Raise(PlayerPlanetEvent e)            => OnPlayerLandedOnPlanet?.Invoke(e);
        public static void RaiseLeftPlanet(PlayerPlanetEvent e)  => OnPlayerLeftPlanet?.Invoke(e);
        public static void Raise(PlayerTeleportEvent e)          => OnPlayerTeleported?.Invoke(e);

        public static void Raise(ResourceCollectedEvent e)       => OnResourceCollected?.Invoke(e);
        public static void Raise(ResourceTransportedEvent e)     => OnResourceTransported?.Invoke(e);
        public static void RaiseStorageFull(ResourceType type)   => OnStorageFull?.Invoke(type);
        public static void RaiseRouteCreated(TransportRouteEvent e) => OnTransportRouteCreated?.Invoke(e);
        public static void RaiseRouteRemoved(TransportRouteEvent e) => OnTransportRouteRemoved?.Invoke(e);

        public static void RaiseConnectionCreated(ConnectionEvent e)   => OnConnectionCreated?.Invoke(e);
        public static void RaiseConnectionDestroyed(ConnectionEvent e) => OnConnectionDestroyed?.Invoke(e);
        public static void Raise(ConnectionHealthChangedEvent e)
        {
            OnConnectionHealthChanged?.Invoke(e);
            if (e.Health <= 20f) OnConnectionCritical?.Invoke(e);
        }
        public static void RaiseAncientDiscovered(ConnectionEvent e)  => OnAncientConnectionDiscovered?.Invoke(e);
        public static void RaiseAncientActivated(ConnectionEvent e)   => OnAncientConnectionActivated?.Invoke(e);

        public static void RaisePlanetDiscovered(Transform planet)    => OnPlanetDiscovered?.Invoke(planet);
        public static void RaiseSecondaryHubCreated(Transform planet) => OnSecondaryHubCreated?.Invoke(planet);

        public static void Raise(HubUpgradedEvent e)             => OnHubUpgraded?.Invoke(e);
        public static void RaiseRecipeTierUnlocked(int tier)      => OnRecipeTierUnlocked?.Invoke(tier);

        public static void RaiseMachinePlaced(MachineEvent e)   => OnMachinePlaced?.Invoke(e);
        public static void RaiseMachineBroken(MachineEvent e)   => OnMachineBroken?.Invoke(e);
        public static void RaiseMachineRepaired(MachineEvent e) => OnMachineRepaired?.Invoke(e);

        public static void Raise(MilestoneEvent e)                  => OnMilestoneReached?.Invoke(e);
        public static void RaiseStoryFragment(string fragment)      => OnStoryFragmentUnlocked?.Invoke(fragment);

        public static void Raise(MiningProgressEvent e)             => OnMiningProgress?.Invoke(e);

        public static void RaiseToolEquipped(ToolEquippedEvent e)        => OnToolEquipped?.Invoke(e);
        public static void RaiseToolDurabilityChanged(ToolDurabilityEvent e) => OnToolDurabilityChanged?.Invoke(e);

        public static void RaiseQuickSlotsChanged() => OnQuickSlotsChanged?.Invoke();
        public static void RaiseInventoryItemAdded() => OnInventoryItemAdded?.Invoke();

        public static void Raise(PlayerHealthChangedEvent e) => OnPlayerHealthChanged?.Invoke(e);
        public static void Raise(PlayerDamagedEvent e)       => OnPlayerDamaged?.Invoke(e);
        public static void Raise(PlayerDiedEvent e)          => OnPlayerDied?.Invoke(e);
    }

    // ── Event payload tipovi ──────────────────────────────────────────────────
    // Konsolidirano iz EventTypes.cs (čišćenje malih datoteka, srpanj 2026.):
    // sabirnica i njezini payloadi su jedno sučelje. Ista napomena o rezerviranim
    // tipovima kao gore — dio ih čeka buduće featuree, ne brisati.

    public enum ResourceType { Ore, Biomass, Ice, Gas, VolcanicMatter }
    public enum PlanetType { Mining, Organic, Ice, Gaseous, Volcanic }
    public enum ConnectionType { Ancient, Weak, Mid, Strong }
    public enum MachineState { Active, Idle, Broken }
    public enum HubLevel { Basic, Upgraded, Advanced }
    public enum MilestoneType { FirstResource, FirstConnection, HubUpgraded, NetworkComplete }

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

    public struct MachineEvent
    {
        public MachineState State;
        public Transform Planet;
        public ResourceType ResourceType;
        public string MachineName;
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

    public struct PlayerHealthChangedEvent
    {
        public float Current;
        public float Max;
    }

    public struct PlayerDamagedEvent
    {
        public float Amount;
        public float Current;
        public Vector3 Position;
    }

    public struct PlayerDiedEvent
    {
        public Vector3 Position;
    }
}
