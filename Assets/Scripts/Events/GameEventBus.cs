using System;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
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
        public static event Action<ArtifactType>     OnBlueprintUnlocked;

        // ── Artifacts ─────────────────────────────────────────────────────────
        public static event Action<ArtifactEvent> OnArtifactFound;
        public static event Action<ArtifactEvent> OnArtifactActivated;
        public static event Action<Transform>     OnPairedArtifactCombined;

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

        // ── Quick Slots ───────────────────────────────────────────────────────
        public static event Action OnQuickSlotsChanged;

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
        public static void RaiseBlueprintUnlocked(ArtifactType t) => OnBlueprintUnlocked?.Invoke(t);

        public static void RaiseArtifactFound(ArtifactEvent e)      => OnArtifactFound?.Invoke(e);
        public static void RaiseArtifactActivated(ArtifactEvent e)  => OnArtifactActivated?.Invoke(e);
        public static void RaisePairedCombined(Transform planet)     => OnPairedArtifactCombined?.Invoke(planet);

        public static void RaiseMachinePlaced(MachineEvent e)   => OnMachinePlaced?.Invoke(e);
        public static void RaiseMachineBroken(MachineEvent e)   => OnMachineBroken?.Invoke(e);
        public static void RaiseMachineRepaired(MachineEvent e) => OnMachineRepaired?.Invoke(e);

        public static void Raise(MilestoneEvent e)                  => OnMilestoneReached?.Invoke(e);
        public static void RaiseStoryFragment(string fragment)      => OnStoryFragmentUnlocked?.Invoke(fragment);

        public static void Raise(MiningProgressEvent e)             => OnMiningProgress?.Invoke(e);

        public static void RaiseToolEquipped(ToolEquippedEvent e)        => OnToolEquipped?.Invoke(e);
        public static void RaiseToolDurabilityChanged(ToolDurabilityEvent e) => OnToolDurabilityChanged?.Invoke(e);

        public static void RaiseQuickSlotsChanged() => OnQuickSlotsChanged?.Invoke();
    }
}
