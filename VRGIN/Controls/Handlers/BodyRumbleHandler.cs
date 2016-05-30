using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Controls.Handlers
{
    public class BodyRumbleHandler : ProtectedBehaviour
    {
        private Controller _Controller;
        private int _TouchCounter = 0;
        private RumbleSession _Rumble;

        protected override void OnStart()
        {
            base.OnStart();

            _Controller = GetComponent<Controller>();
        }

        protected override void OnLevel(int level)
        {
            base.OnLevel(level);
            OnStop();

        }

        protected void OnDisable()
        {
            OnStop();

        }

        protected void OnTriggerEnter(Collider collider)
        {
            if (VR.Interpreter.IsBody(collider))
            {
                _TouchCounter++;

                if (_Rumble == null)
                {
                    _Rumble = new RumbleSession(50, 10, 1f);
                    _Controller.StartRumble(_Rumble);
                }
            }
        }

        protected void OnTriggerStay(Collider collider)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("ToLiquidCollision"))
            {
                _Rumble.Restart();
            }
        }

        protected void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("ToLiquidCollision"))
            {
                _TouchCounter--;

                if (_TouchCounter == 0)
                {
                    OnStop();
                }
            }
        }

        protected void OnStop()
        {
            _TouchCounter = 0;
            _Rumble.Close();
            _Rumble = null;
        }
    }
}
