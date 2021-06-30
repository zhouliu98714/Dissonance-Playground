using System.Collections;
using System.Collections.Generic;
using CEMSIM.Network;
using UnityEngine;
using UnityEngine.UI;

namespace CEMSIM
{
    // display debug message on screen (server side)
    public class NetworkOverlayMenu : Menu<NetworkOverlayMenu>
    {
        public GameObject debugTextOutput;
        private string _log;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Log(string outputString)
        {
            if(_log == null)
            {
                _log = outputString;
            }
            else
            {
                _log = _log + "\n" + outputString;               
            }
            ServerThreadManager.ExecuteOnMainThread(() =>
            {
                debugTextOutput.GetComponent<Text>().text = _log;
            });
            

        }
    }
}
