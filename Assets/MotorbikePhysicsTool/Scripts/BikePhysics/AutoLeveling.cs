using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gadd420
{
    public class AutoLeveling : MonoBehaviour
    {
        //Vector Directions drawn in game gizmos
        Vector3 flatFwd;
        Vector3 flatRight;

        //Dot Products
        float forwardDot;
        float rightDot;

        //Player Components
        Rigidbody rb;
        RB_Controller rbController;
        Input_Manager inputs;
        GroundAngle groundAngle;

        [Header("Gyro Strength Recommended 2")]
        //Bikes will work without this script but will not return to upright position
        public float autoLevelForce = 2;

        //To know when the bike is crashed to stop the gyro
        CrashController crashScript;

        public bool safeWheelies;
        public float antiLoopStrength = 10;
        public float maxWheelieAngle = 45;
        public float maxAngleMultiplier = 1.25f;

        [Range(0, 1)]
        public float dotForAutoLevel = 0.2f;

        float forwardAngle;
        public float antiSpinTorque = 5;
        float upAngluarVelocity;


        // Start is called before the first frame update
        void Start()
        {
            //Getting Components
            rb = GetComponent<Rigidbody>();
            rbController = GetComponent<RB_Controller>();
            inputs = GetComponent<Input_Manager>();
            crashScript = GetComponent<CrashController>();
            groundAngle = GetComponent<GroundAngle>();
        }

        private void Update()
        {
            CalculateAngles();

            Vector3 forwardDir = (rb.transform.forward);
            Vector3 forwardVelocity = rb.linearVelocity.normalized;

            forwardAngle = Vector3.Angle(forwardDir, forwardVelocity);

            upAngluarVelocity = rb.angularVelocity.y;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            //If bike isn't Crashed
            if (!crashScript.isCrashed)
            {
                AutoLevel(rb);
            }
            if (rbController.isGrounded && forwardAngle > 45 && upAngluarVelocity > 0f)
            {
                rb.AddTorque(Vector3.up * -antiSpinTorque, ForceMode.Acceleration);
            }
            else if (rbController.isGrounded && forwardAngle > 45 && upAngluarVelocity < 0f)
            {
                rb.AddTorque(Vector3.up * antiSpinTorque, ForceMode.Acceleration);
            }
        }

        void CalculateAngles()
        {
            //Calculate Flat Forward
            flatFwd = transform.forward;

            flatFwd.y = groundAngle.forwardY;
            flatFwd = flatFwd.normalized;

            //Draw Z Axis
            Debug.DrawRay(transform.position, flatFwd, Color.blue);

            //Calculate Flat Right
            flatRight = transform.right;
            flatRight.y = 0;
            flatRight = flatRight.normalized;

            //Draw X Axis
            Debug.DrawRay(transform.position, flatRight, Color.red);

            //Calculate Angles
            forwardDot = Vector3.Dot(transform.up, flatFwd);
            rightDot = Vector3.Dot(transform.up, flatRight);
        }

        void AutoLevel(Rigidbody rb)
        {
            //Calculate Forces
            float forwardForce = rightDot * autoLevelForce;
            float rightForce = forwardDot * autoLevelForce;
            
            //Add Forces if Upright
            if (rb.transform.eulerAngles.z < 90 || rb.transform.eulerAngles.z > 270 && rbController.isGrounded)
            {
                //If Input Leaning Left and Turning Right whilst actually leaning left 

                if (inputs.LeanInput == -inputs.HzInput && inputs.LeanInput != 0 && inputs.HzInput != 0)
                {
                    //Add Force to Lean Left
                    rb.AddRelativeTorque(Vector3.forward * (forwardForce * 2f), ForceMode.Acceleration);
                }

                //Normal Corrective Force

                if (rightDot > dotForAutoLevel || rightDot < -dotForAutoLevel)
                {
                    rb.AddRelativeTorque(Vector3.forward * forwardForce, ForceMode.Acceleration);
                }
            }

            //If safeWheelies is true and doing a wheelie
            if (safeWheelies && rbController.wheelColliders[0].isGrounded && !rbController.wheelColliders[1].isGrounded)
            {
                //If wheelie angle is more than set maximum will add a multiplied force to keep the bike down
                if (groundAngle.wheelieAngleMinusSurfaceAngle > maxWheelieAngle)
                {
                    rb.AddRelativeTorque(Vector3.right * -(rightForce * antiLoopStrength * maxAngleMultiplier), ForceMode.Acceleration);
                }
                //Adds the regular wheelie assistance force
                else
                {
                    rb.AddRelativeTorque(Vector3.right * -(rightForce * antiLoopStrength), ForceMode.Acceleration);
                }
            }
        }
    }
}

