using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class Attractor : MonoBehaviour
    {
        public static List<Attractor> Attractors;
        public float gravity = -10;
        private Transform m_Transform;
        private Rigidbody m_Rigidbody;

        public void Attract(Transform body)
        {
            Vector3 gravityUp = (body.position - transform.position).normalized;
            Vector3 bodyUp = body.up;
            body.GetComponent<Rigidbody>().AddForce(gravityUp * gravity);
            Quaternion targetRotation = Quaternion.FromToRotation(bodyUp, gravityUp) * body.rotation;
            body.rotation = Quaternion.Slerp(body.rotation, targetRotation, 50 * Time.deltaTime);
        }

        void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            m_Rigidbody.useGravity = false;

            m_Transform = GetComponent<Transform>();
        }

        void FixedUpdate()
        {
            if (m_Rigidbody != null && m_Transform != null)
            {
                foreach (Attractor attractor in Attractors)
                {
                    if (attractor != this)
                        attractor.Attract(m_Transform);
                }
            }
        }

        void OnEnable()
        {
            if (Attractors == null)
                Attractors = new List<Attractor>();
            Attractors.Add(this);
        }

        void OnDisable()
        {
            Attractors.Remove(this);
        }
    }
}
