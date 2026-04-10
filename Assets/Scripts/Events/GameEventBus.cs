using System;
using UnityEngine;

/// <summary>
/// Centralni statički event bus za cijelu igru.
///
/// Pretplata:   GameEventBus.OnResourceCollected += HandleResource;
/// Odjavljivanje: GameEventBus.OnResourceCollected -= HandleResource;
/// Okidanje:    GameEventBus.Raise(new ResourceCollectedEvent { ... });
/// </summary>
public static class GameEventBus
{
    // ─── Player ───────────────────────────────────────────────────────────────

    /// Igrač je sletio na planet
    public static event Action<Transform> OnPlayerLandedOnPlanet;

    /// Igrač je napustio planet (otišao u svemir / teleport)
    public static event Action<Transform> OnPlayerLeftPlanet;

    /// Igrač se teleportirao (plaća resurse ako nema veze)
    public static event Action<Transform, Transform> OnPlayerTeleported; // from, to

    // ─── Resources ────────────────────────────────────────────────────────────

    /// Igrač je ručno skupio resurs
    public static event Action<ResourceCollectedEvent> OnResourceCollected;

    /// Automatski transport je isporučio resurse
    public static event Action<ResourceTransportedEvent> OnResourceTransported;

    /// Lokalno skladište nekog planeta je puno
    public static event Action<Transform> OnStorageFull;

    /// Ruta transporta je postavljena
    public static event Action<TransportRouteEvent> OnTransportRouteCreated;

    /// Ruta transporta je uklonjena
    public static event Action<TransportRouteEvent> OnTransportRouteRemoved;

    // ─── Network / Connections ────────────────────────────────────────────────

    /// Nova veza između dva planeta je kreirana
    public static event Action<ConnectionEvent> OnConnectionCreated;

    /// Veza između dva planeta je uništena (zdravlje = 0)
    public static event Action<ConnectionEvent> OnConnectionDestroyed;

    /// Zdravlje veze se promijenilo (svaki tick)
    public static event Action<ConnectionHealthChangedEvent> OnConnectionHealthChanged;

    /// Veza je u kritičnom stanju (< 20%)
    public static event Action<ConnectionEvent> OnConnectionCritical;

    /// Igrač je otkrio skrivenu drevnu vezu
    public static event Action<ConnectionEvent> OnAncientConnectionDiscovered;

    /// Igrač je aktivirao drevnu vezu
    public static event Action<ConnectionEvent> OnAncientConnectionActivated;

    // ─── Planets ──────────────────────────────────────────────────────────────

    /// Igrač je po prvi puta posjetio planet
    public static event Action<Transform, PlanetType> OnPlanetDiscovered;

    /// Planet je unaprijeđen u sekundarni hub
    public static event Action<Transform> OnSecondaryHubCreated;

    // ─── Hub ──────────────────────────────────────────────────────────────────

    /// Glavni hub je unaprijeđen
    public static event Action<HubLevel> OnHubUpgraded;

    /// Novi nacrt je otključan
    public static event Action<string> OnBlueprintUnlocked; // blueprintId

    // ─── Artifacts ────────────────────────────────────────────────────────────

    /// Artefakt je pronađen na planetu
    public static event Action<ArtifactEvent> OnArtifactFound;

    /// Artefakt je aktiviran / upotrijebljen
    public static event Action<ArtifactEvent> OnArtifactActivated;

    /// Parni artefakti su kombinirani
    public static event Action<Transform, Transform> OnPairedArtifactCombined; // planetA, planetB

    // ─── Machines ─────────────────────────────────────────────────────────────

    /// Stroj je postavljen na planet
    public static event Action<MachineEvent> OnMachinePlaced;

    /// Stroj se pokvario
    public static event Action<MachineEvent> OnMachineBroken;

    /// Stroj je popravljen
    public static event Action<MachineEvent> OnMachineRepaired;

    // ─── Game State ───────────────────────────────────────────────────────────

    /// Milestone je dostignut (napredak priče)
    public static event Action<MilestoneType> OnMilestoneReached;

    /// Fragment priče je otključan na računalu
    public static event Action<int> OnStoryFragmentUnlocked; // fragmentId

    // ─── Raise helpers ───────────────────────────────────────────────────────

    public static void Raise(Transform planet, bool landed)
    {
        if (landed) OnPlayerLandedOnPlanet?.Invoke(planet);
        else        OnPlayerLeftPlanet?.Invoke(planet);
    }

    public static void Raise(Transform from, Transform to)               => OnPlayerTeleported?.Invoke(from, to);
    public static void Raise(ResourceCollectedEvent e)                   => OnResourceCollected?.Invoke(e);
    public static void Raise(ResourceTransportedEvent e)                 => OnResourceTransported?.Invoke(e);
    public static void RaiseStorageFull(Transform planet)                => OnStorageFull?.Invoke(planet);
    public static void Raise(TransportRouteEvent e, bool created)
    {
        if (created) OnTransportRouteCreated?.Invoke(e);
        else         OnTransportRouteRemoved?.Invoke(e);
    }
    public static void RaiseConnectionCreated(ConnectionEvent e)         => OnConnectionCreated?.Invoke(e);
    public static void RaiseConnectionDestroyed(ConnectionEvent e)       => OnConnectionDestroyed?.Invoke(e);
    public static void Raise(ConnectionHealthChangedEvent e)             => OnConnectionHealthChanged?.Invoke(e);
    public static void RaiseConnectionCritical(ConnectionEvent e)        => OnConnectionCritical?.Invoke(e);
    public static void RaiseAncientDiscovered(ConnectionEvent e)         => OnAncientConnectionDiscovered?.Invoke(e);
    public static void RaiseAncientActivated(ConnectionEvent e)          => OnAncientConnectionActivated?.Invoke(e);
    public static void RaisePlanetDiscovered(Transform p, PlanetType t)  => OnPlanetDiscovered?.Invoke(p, t);
    public static void RaiseSecondaryHub(Transform p)                    => OnSecondaryHubCreated?.Invoke(p);
    public static void RaiseHubUpgraded(HubLevel level)                  => OnHubUpgraded?.Invoke(level);
    public static void RaiseBlueprintUnlocked(string id)                 => OnBlueprintUnlocked?.Invoke(id);
    public static void Raise(ArtifactEvent e, bool activated)
    {
        if (activated) OnArtifactActivated?.Invoke(e);
        else           OnArtifactFound?.Invoke(e);
    }
    public static void RaisePairedArtifact(Transform a, Transform b)    => OnPairedArtifactCombined?.Invoke(a, b);
    public static void Raise(MachineEvent e)
    {
        switch (e.State)
        {
            case MachineState.Active:  OnMachineRepaired?.Invoke(e); break;
            case MachineState.Full:    OnStorageFull?.Invoke(e.Planet); break;
            case MachineState.Broken:  OnMachineBroken?.Invoke(e); break;
        }
    }
    public static void RaiseMachinePlaced(MachineEvent e)               => OnMachinePlaced?.Invoke(e);
    public static void RaiseMilestone(MilestoneType m)                  => OnMilestoneReached?.Invoke(m);
    public static void RaiseStoryFragment(int id)                       => OnStoryFragmentUnlocked?.Invoke(id);
}
