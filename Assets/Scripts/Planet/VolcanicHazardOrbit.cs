using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Kružna putanja zone po površini planeta: rotacija oko fiksne osi kroz centar
    // planeta. Vulkanski planeti su uniformno skalirane primitivne sfere, pa je
    // udaljenost od centra duž cijele kružnice konstantna — zona ostaje jednako
    // ukopana bez raycasta po frameu.
    public class VolcanicHazardOrbit : MonoBehaviour
    {
        private Transform _planet;
        private Vector3 _axis;
        private float _angularSpeed; // stupnjevi u sekundi

        public void Init(Transform planet, Vector3 axis, float angularSpeed)
        {
            _planet = planet;
            _axis = axis;
            _angularSpeed = angularSpeed;
        }

        void Update()
        {
            if (_planet == null)
            {
                enabled = false;
                return;
            }

            transform.RotateAround(_planet.position, _axis, _angularSpeed * Time.deltaTime);
        }
    }
}
