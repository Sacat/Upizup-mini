using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gadd420
{
    public class SkidManager : MonoBehaviour
    {
        public bool enableSkidSettings = true;
        public bool frontWheelSkids = false;
        [Range(0, 1)]
        public float minForwadSlipForSkid = 0.4f;
        [Range(0, 1)]
        public float minSidewaysSlipForSkid = 0.4f;
        RB_Controller rbController;

        public Transform skidTrail;
        Transform[] skidTrails = new Transform[2];

        public ParticleSystem smokePrefab;
        ParticleSystem[] skidSmoke = new ParticleSystem[2];
        public AudioSource skidSound;

        public float soundStopDelay = 0.5f;
        public float skidMarkStopDelay = 0.5f;
        float soundTimer;
        float skidMarkTimer;

        public void StartSkidTrail(int i)
        {
            if (skidTrails[i] == null)
            {
                skidTrails[i] = Instantiate(skidTrail);
            }
            skidTrails[i].parent = rbController.wheelColliders[i].transform;
            skidTrails[i].localRotation = Quaternion.Euler(90, 0, 0);
            skidTrails[i].localPosition = -Vector3.up * (rbController.wheelColliders[i].radius + 0.1f);
        }

        public void EndSkidTrail(int i)
        {
            if (skidTrails[i] == null)
            {
                return;
            }
            Transform holder = skidTrails[i];
            skidTrails[i] = null;
            holder.parent = null;
            holder.rotation = Quaternion.Euler(90, 0, 0);
            Destroy(holder.gameObject, 30);
        }

        // Start is called before the first frame update
        void Start()
        {
            rbController = GetComponent<RB_Controller>();
            for (int i = 0; i < 2; i++)
            {
                skidSmoke[i] = Instantiate(smokePrefab);
                skidSmoke[i].Stop();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (enableSkidSettings)
            {
                CheckForSkid();
            }

        }

        void CheckForSkid()
        {
            int numSkidding = 0;
            for (int i = 0; i < rbController.wheelColliders.Length; i++)
            {
                WheelHit wheelHit;
                rbController.wheelColliders[i].GetGroundHit(out wheelHit);

                if (rbController.wheelColliders[i].isGrounded && Mathf.Abs(wheelHit.forwardSlip) >= minForwadSlipForSkid || Mathf.Abs(wheelHit.sidewaysSlip) >= minSidewaysSlipForSkid)
                {
                    numSkidding++;
                    if (!frontWheelSkids && i == 1)
                    {

                    }
                    else
                    {
                        StartSkidTrail(i);
                        skidSmoke[i].transform.position = rbController.wheelColliders[i].transform.position - rbController.wheelColliders[i].transform.up * rbController.wheelColliders[i].radius;
                        skidSmoke[i].Emit(1);
                        if (!skidSound.isPlaying)
                        {
                            skidSound.Play();
                        }
                        skidMarkTimer = 0;
                    }


                }
                else
                {
                    skidMarkTimer += Time.deltaTime;
                    skidMarkTimer = Mathf.Clamp(skidMarkTimer, 0, skidMarkStopDelay);
                    if (skidMarkTimer >= skidMarkStopDelay || !rbController.wheelColliders[i].isGrounded)
                    {
                        EndSkidTrail(i);
                    }

                }
                if (numSkidding == 0 && skidSound.isPlaying)
                {
                    soundTimer += Time.deltaTime;
                    soundTimer = Mathf.Clamp(soundTimer, 0, soundStopDelay);
                    if (soundTimer >= soundStopDelay)
                    {
                        skidSound.Stop();
                    }
                }
                else
                {
                    soundTimer = 0;
                }
            }
        }
    }
}

