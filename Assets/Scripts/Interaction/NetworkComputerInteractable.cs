using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class NetworkComputerInteractable : BaseInteractable
    {
        [SerializeField] private NetworkMapUI networkMapUI;

        public override void Interact()
        {
            if (networkMapUI == null)
            {
                Debug.LogWarning("NetworkComputerInteractable: nije dodijeljen NetworkMapUI.");
                return;
            }

            if (networkMapUI.IsOpen)
                networkMapUI.Close();
            else
                networkMapUI.Open();
        }
    }
}
