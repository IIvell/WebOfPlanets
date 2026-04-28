using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public abstract class BaseInteractable : MonoBehaviour, IInteractable
    {
        public abstract void Interact();
    }
}
