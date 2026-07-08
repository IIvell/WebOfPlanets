using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ConnectionInteractable : BaseInteractable
    {
        private PlanetCreator _planetCreator;
        private Transform _sourcePlanet;
        private Transform _targetPlanet;
        private Transform _destinationMarker;

        public Transform SourcePlanet => _sourcePlanet;
        public Transform TargetPlanet => _targetPlanet;

        public void Init(PlanetCreator planetCreator, Transform sourcePlanet, Transform targetPlanet)
        {
            _planetCreator = planetCreator;
            _sourcePlanet = sourcePlanet;
            _targetPlanet = targetPlanet;
        }

        public void SetDestinationMarker(Transform destinationMarker)
        {
            _destinationMarker = destinationMarker;
        }

        public override void Interact()
        {
            _planetCreator.TeleportToPlanet(_targetPlanet, _sourcePlanet, _destinationMarker);
        }
    }
}
