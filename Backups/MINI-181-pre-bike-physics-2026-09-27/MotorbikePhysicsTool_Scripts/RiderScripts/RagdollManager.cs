using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gadd420
{
    public class RagdollManager : MonoBehaviour
    {
        Collider[] colliders;
        Rigidbody[] rbs;
        IK[] ikScripts;
        RB_Controller rbScript;
        Rigidbody bikeRB;

        bool velocitySet;
        [HideInInspector] public bool resetRider;

        // Start is called before the first frame update
        void Start()
        {
            colliders = GetComponentsInChildren<Collider>();
            rbs = GetComponentsInChildren<Rigidbody>();
            ikScripts = GetComponentsInChildren<IK>();
            rbScript = GetComponentInParent<RB_Controller>();
            bikeRB = rbScript.gameObject.GetComponent<Rigidbody>();

            foreach (Rigidbody rb in rbs)
            {
                rb.isKinematic = true;
            }

            foreach (Collider collider in colliders)
            {
                collider.isTrigger = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (rbScript.isCrashed)
            {
                //This will stop the rider following the bike follow possitions
                foreach (IK ikScript in ikScripts)
                {
                    ikScript.hasCrashed = true;
                }
                //Sets the player triggers to colliders
                foreach (Collider collider in colliders)
                {
                    collider.isTrigger = false;
                }
                //Activates the Rigidbodies
                foreach (Rigidbody rb in rbs)
                {
                    rb.isKinematic = false;
                    //Sets the velocity to what the bike is at the time of the crash
                    if (!velocitySet)
                    {
                        rb.linearVelocity = bikeRB.linearVelocity;
                    }
                    //Makes sure the velocity is only set once
                    velocitySet = true;
                }
            }

            if (resetRider)
            {
                rbScript.isCrashed = false;
                velocitySet = false;
                //Makes the rider grab follow the tracking positions again
                foreach (IK ikScript in ikScripts)
                {
                    ikScript.hasCrashed = false;
                }
                //Turns colliders back to triggers
                foreach (Collider collider in colliders)
                {
                    collider.isTrigger = true;
                }
                //Deactivates the rigidbodies
                foreach (Rigidbody rb in rbs)
                {
                    rb.isKinematic = true;
                }
                //Cancels this part of the code
                resetRider = false;
            }
        }


    }
}

