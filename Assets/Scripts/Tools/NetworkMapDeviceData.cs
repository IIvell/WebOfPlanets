using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Ručni uređaj — ne postavlja se na planetu i ne troši se korištenjem.
    // Dok je odabran u hotbaru, tipka P otvara mapu mreže planeta (NetworkMapUI).
    [CreateAssetMenu(fileName = "NetworkMapDevice", menuName = "Inventory/Network Map Device")]
    public class NetworkMapDeviceData : QuickSlotItem
    {
    }
}
