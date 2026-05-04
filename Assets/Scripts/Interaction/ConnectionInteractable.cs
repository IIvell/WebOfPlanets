using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ConnectionInteractable : BaseInteractable
    {
        private PlanetCreator _planetCreator;
        private Transform _sourcePlanet;
        private Transform _targetPlanet;

        public void Init(PlanetCreator planetCreator, Transform sourcePlanet, Transform targetPlanet)
        {
            _planetCreator = planetCreator;
            _sourcePlanet = sourcePlanet;
            _targetPlanet = targetPlanet;
        }

        public override void Interact()
        {
            _planetCreator.TeleportToPlanet(_targetPlanet, _sourcePlanet);
        }
    }
}
