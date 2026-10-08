using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gadd420
{
    public class VehicleSelect : MonoBehaviour
    {
        public GameObject motocross;
        public GameObject moped;
        public GameObject chopper;
        public GameObject bicycle;

        [Header("Chest or Hip is Ideal")]
        public Transform motocrossRiderCamFollowPos;
        public Transform mopedRiderCamFollowPos;
        public Transform chopperRiderCamFollowPos;
        public Transform bicycleRiderCamFollowPos;
        [Space]
        public ThirdPersonCamera cameraScript;
        public KeyBoardShortCuts shortCutScript;

        public GameObject canvas;
        public Speedometer speedo;
        public NitrousUI nitrousUI;



        //These functions are for the Buttons when you play the test scene



        public void SelectMotocross()
        {
            if (motocrossRiderCamFollowPos)
            {
                cameraScript.lookAt = motocrossRiderCamFollowPos;
            }
            else
            {
                cameraScript.lookAt = motocross.transform;
            }
            speedo.controller = motocross.gameObject.GetComponent<RB_Controller>();
            shortCutScript.currentBike = motocross.transform;
            nitrousUI.nitrousScript = motocross.gameObject.GetComponent<NitrousManager>();
            moped.SetActive(false);
            chopper.SetActive(false);
            bicycle.SetActive(false);
            this.gameObject.SetActive(false);
        }

        public void SelectMoped()
        {
            if (mopedRiderCamFollowPos)
            {
                cameraScript.lookAt = mopedRiderCamFollowPos;
            }
            else
            {
                cameraScript.lookAt = moped.transform;
            }
            speedo.controller = moped.gameObject.GetComponent<RB_Controller>();
            shortCutScript.currentBike = moped.transform;
            nitrousUI.nitrousScript = moped.gameObject.GetComponent<NitrousManager>();
            motocross.SetActive(false);
            chopper.SetActive(false);
            bicycle.SetActive(false);
            canvas.SetActive(false);
        }

        public void SelectChopper()
        {

            if (chopperRiderCamFollowPos)
            {
                cameraScript.lookAt = chopperRiderCamFollowPos;
            }
            else
            {
                cameraScript.lookAt = chopper.transform;
            }
            speedo.controller = chopper.gameObject.GetComponent<RB_Controller>();
            shortCutScript.currentBike = chopper.transform;
            nitrousUI.nitrousScript = chopper.gameObject.GetComponent<NitrousManager>();
            motocross.SetActive(false);
            moped.SetActive(false);
            bicycle.SetActive(false);
            canvas.SetActive(false);
        }

        public void SelectBicycle()
        {
            if (bicycleRiderCamFollowPos)
            {
                cameraScript.lookAt = bicycleRiderCamFollowPos;
            }
            else
            {
                cameraScript.lookAt = bicycle.transform;
            }
            speedo.controller = bicycle.gameObject.GetComponent<RB_Controller>();
            shortCutScript.currentBike = bicycle.transform;
            nitrousUI.nitrousScript = bicycle.gameObject.GetComponent<NitrousManager>();
            motocross.SetActive(false);
            moped.SetActive(false);
            chopper.SetActive(false);
            canvas.SetActive(false);
        }
    }
}

