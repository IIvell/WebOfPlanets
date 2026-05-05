using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PotentialConnectionInteractable : BaseInteractable
    {
        private ConnectionManager _connectionManager;
        private Transform _sourcePlanet;
        private Transform _targetPlanet;

        public void Init(ConnectionManager connectionManager, Transform source, Transform target)
        {
            _connectionManager = connectionManager;
            _sourcePlanet = source;
            _targetPlanet = target;
        }

        public override void Interact()
        {
            ConnectionChoiceUI.Instance?.Show(_connectionManager, _sourcePlanet, _targetPlanet);
        }
    }
}
