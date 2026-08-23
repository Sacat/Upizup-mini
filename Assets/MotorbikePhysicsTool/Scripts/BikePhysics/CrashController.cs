using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gadd420
{

    public class CrashController : MonoBehaviour
    {
        RB_Controller rbScript;
        public bool isCrashed;
        public string[] crashTag;

        Rigidbody rb;

        [HideInInspector]public float rbSpeed;
        [HideInInspector]public float lateRbSpeed;
        float counter = 0;

        public float decelerationSpeedForCrash = 5;

        private void Start()
        {
            rbScript = GetComponent<RB_Controller>();
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if(counter == 0)
            {
                rbSpeed = rb.linearVelocity.magnitude;
            }

            counter += Time.deltaTime;

            if (counter >= 0.05f)
            {
                lateRbSpeed = rb.linearVelocity.magnitude;

                if (rbSpeed - lateRbSpeed > decelerationSpeedForCrash)
                {
                    rbScript.isCrashed = true;
                }

                counter = 0;
            }
        }

        //When the bikes crash trigger hits the ground isCrashed is set and only reset after respawning 
        //Check KeyboardShortucts script for isCrashed = false; 
        private void OnTriggerEnter(Collider other)
        {
            for(int i = 0; i < crashTag.Length; i++)
            {
                if (other.gameObject.CompareTag(crashTag[i]))
                {
                    rbScript.isCrashed = true;
                }
            }
            
        }

    }
}
