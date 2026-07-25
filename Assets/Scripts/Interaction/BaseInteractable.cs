using UnityEngine;

namespace WebOfPlanets
{
    public abstract class BaseInteractable : MonoBehaviour, IInteractable
    {
        public virtual float HoldTime => 0f;
        public virtual bool CanInteract => true;
        public abstract void Interact();
    }
}
