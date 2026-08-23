using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gadd420
{
    public class Speedometer : MonoBehaviour
    {
        public RB_Controller controller;

        public Text mph;
        public Text gear;


        // Update is called once per frame
        void Update()
        {
            //If the RB Controller is assigned
            if (controller)
            {
                //Gets and displays speed in Mph
                float speed = controller.currentSpeed * controller.msToMph;
                speed = Mathf.Round(speed);
                if (controller.useKmph)
                {
                    mph.text = (speed + " Kmph");
                }
                else
                {
                    mph.text = (speed + " Mph");
                }
                

                //Gets and displays the gears
                float currentGear = controller.currentGear + 1;
                gear.text = ("Gear " + currentGear);
            }
        }
    }
}
