using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ball Trajectory & Trail
// Kyrylo Avramenko 
// https://github.com/kir-avramenko

namespace Shorka.BallTrajectory
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ThrownObject : MonoBehaviour
    {
        private Rigidbody2D rigidBody;
        private Collider2D coll2D;

        public Collider2D Collider { get { return coll2D; } }

        void Awake()
        {
            rigidBody = GetComponent<Rigidbody2D>();

            coll2D = GetComponent<Collider2D>();
            if (coll2D == null)
                Debug.LogError(gameObject.name + " gameobject doesn't have attached Collider2D component.");
        }

        private Vector2 velocityToRg = Vector2.zero;
        public void ThrowObj(Vector3 velocity)
        {
            Debug.Log("ThrowObj");

            velocityToRg = velocity;

            rigidBody.velocity = velocityToRg;
            rigidBody.isKinematic = false;
        }

        private Vector3 velocity = Vector3.zero;
        public void Reset(Vector3 pos)
        {
            rigidBody.velocity = velocity;
            transform.position = pos;
        }
    }
}