using UnityEngine;

namespace WebOfPlanets
{
    public class NetworkComputerInteractable : BaseInteractable
    {
        public override void Interact()
        {
            var menu = ComputerMenuUI.Instance;
            if (menu == null)
            {
                Debug.LogWarning("NetworkComputerInteractable: ComputerMenuUI nije u sceni.");
                return;
            }

            if (menu.IsOpen)
                menu.Hide();
            else
                menu.Show();
        }
    }
}
