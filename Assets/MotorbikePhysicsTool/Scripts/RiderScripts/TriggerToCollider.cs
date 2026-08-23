using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gadd420
{
    public class TriggerToCollider : MonoBehaviour
    {
        [Header("Make Sure All GameObjects on this bike use this tag")]

        public string dontCollideWithTag = "Player";
        RB_Controller rbScript;
        RagdollManager ragdollManager;
        Collider theCollider;

        // Start is called before the first frame update
        void Start()
        {
            rbScript = GetComponentInParent<RB_Controller>();
            ragdollManager = GetComponentInParent<RagdollManager>();
            theCollider = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {

            if (!other.gameObject.CompareTag(dontCollideWithTag))
            {
                rbScript.isCrashed = true;
            }

            if (!ragdollManager)
            {
                theCollider.isTrigger = false;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (!ragdollManager)
            {
                theCollider.isTrigger = true;
            }
        }
    }
}

