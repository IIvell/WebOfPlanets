namespace xyz.germanfica.unity.planet.gravity
{
    public interface IInteractable
    {
        float HoldTime { get; }
        // Smije li interakcija krenuti (npr. ItemInteractable traži odgovarajući
        // alat) — u sučelju da Interactor ne mora downcastati na BaseInteractable.
        bool CanInteract { get; }
        void Interact();
    }
}
