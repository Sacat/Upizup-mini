using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Gadd420
{

    public class KeyBoardShortCuts : MonoBehaviour
    {
        public Transform currentBike;
        
        string sceneName;

        Rigidbody bikeRB;
        CrashController crashScript;
        RB_Controller rbController;
        RagdollManager ragdollManager;

        // Start is called before the first frame update
        void Start()
        {
            //Makes sure mouse is unlocked after restart
            Cursor.lockState = CursorLockMode.None;

            //Gets scene name for reloading the scene
            sceneName = SceneManager.GetActiveScene().name;
        }

        // Update is called once per frame
        void Update()
        {
            //When Bike is selected
            if (currentBike && !crashScript)
            {
                //Lock Cursor and get Components from the selected bike
                Cursor.lockState = CursorLockMode.Locked;
                bikeRB = currentBike.gameObject.GetComponent<Rigidbody>();
                rbController = currentBike.gameObject.GetComponent<RB_Controller>();
                ragdollManager = currentBike.gameObject.GetComponentInChildren<RagdollManager>();
                crashScript = currentBike.gameObject.GetComponent<CrashController>();
            }

            //Restart
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(sceneName);
            }

            //Flip over
            if (Input.GetKeyDown(KeyCode.F) && rbController.isCrashed)
            {
                if (ragdollManager)
                {
                    bikeRB.linearVelocity = Vector3.zero;
                    crashScript.rbSpeed = 0;
                    crashScript.lateRbSpeed = 0;
                    Transform playerPos = ragdollManager.gameObject.transform;
                    //sets bike position upright
                    currentBike.position = new Vector3(playerPos.position.x, playerPos.position.y + 1, playerPos.position.z);
                    currentBike.rotation = Quaternion.Euler(0, currentBike.eulerAngles.y, 0);
                }
                else
                {
                    //sets bike position upright
                    currentBike.position = new Vector3(currentBike.position.x, currentBike.position.y + 1, currentBike.position.z);
                    currentBike.rotation = Quaternion.Euler(0, currentBike.eulerAngles.y, 0);
                }
                

                //stop velocitys carrying over into respawn
                bikeRB.linearVelocity = Vector3.zero;
                bikeRB.angularVelocity = Vector3.zero;

                //Makes you be able to drive again
                if (ragdollManager)
                {
                    ragdollManager.resetRider = true;
                }
                
                rbController.isCrashed = false;
            }

        }
    }
}
