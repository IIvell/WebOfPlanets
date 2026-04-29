using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public abstract class BaseInteractable : MonoBehaviour, IInteractable
    {
        public virtual float HoldTime => 0f;
        public abstract void Interact();
    }
}
