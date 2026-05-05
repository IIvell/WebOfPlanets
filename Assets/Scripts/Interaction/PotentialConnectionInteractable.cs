using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PotentialConnectionInteractable : BaseInteractable
    {
        private ConnectionManager _connectionManager;
        private Transform _sourcePlanet;
        private Transform _targetPlanet;
        private GameObject _mirrorMarker;

        public void Init(ConnectionManager connectionManager, Transform source, Transform target)
        {
            _connectionManager = connectionManager;
            _sourcePlanet = source;
            _targetPlanet = target;
        }

        public void SetMirror(GameObject mirror) => _mirrorMarker = mirror;

        public override void Interact()
        {
            _connectionManager.BuildConnection(_sourcePlanet, _targetPlanet);
            if (_mirrorMarker != null)
                Destroy(_mirrorMarker);
            Destroy(gameObject);
        }
    }
}
