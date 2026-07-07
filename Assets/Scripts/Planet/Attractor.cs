using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class Attractor : MonoBehaviour
    {
        public static List<Attractor> Attractors = new();

        [Tooltip("Treba li ovo tijelo samo sebe orijentirati prema gravitaciji drugih attractora. Planeti su čisti izvori gravitacije i ovo mora biti isključeno kod njih, inače ih igrač (kao attractor) rotira dok se kreće po njima.")]
        [SerializeField] private bool orientToGravity = true;
        public bool OrientToGravity { get => orientToGravity; set => orientToGravity = value; }

        private Rigidbody m_Rigidbody;
        public void Attract(Transform body)
        {
            Vector3 gravityUp = (body.position - transform.position).normalized;
            Vector3 bodyUp = body.up;
            Rigidbody rb = body.GetComponent<Rigidbody>();
            Quaternion targetRotation = Quaternion.FromToRotation(bodyUp, gravityUp) * body.rotation;
            rb.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, 50f * Time.fixedDeltaTime));
        }

        void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            m_Rigidbody.useGravity = false;
        }

        void FixedUpdate()
        {
            if (!orientToGravity) return;

            if (m_Rigidbody != null && transform != null)
            {
                foreach (Attractor attractor in Attractors)
                {
                    if (attractor != this)
                        attractor.Attract(transform);
                }
            }
        }

        void OnEnable()
        {
            Attractors.Add(this);
        }

        void OnDisable()
        {
            Attractors.Remove(this);
        }
    }
}
