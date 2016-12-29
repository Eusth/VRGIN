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
        private VelocityRumble _Rumble;

        protected override void OnStart()
        {
            base.OnStart();

            _Controller = GetComponent<Controller>();
            _Rumble = new VelocityRumble(
                SteamVR_Controller.Input((int)_Controller.Tracking.index),
                            30, 10, 3f, 1500, 10);
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

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _Rumble.Device = SteamVR_Controller.Input((int)_Controller.Tracking.index);
        }

        protected void OnTriggerEnter(Collider collider)
        {
            if (VR.Interpreter.IsBody(collider))
            {
                _TouchCounter++;

                _Controller.StartRumble(_Rumble);
                if (_TouchCounter == 1)
                {
                    _Controller.StartRumble(new RumbleImpulse(1000));
                }
            }
        }

        protected void OnTriggerExit(Collider collider)
        {
            if (VR.Interpreter.IsBody(collider))
            {
                _TouchCounter--;

                if (_TouchCounter == 0)
                {
                    _Controller.StopRumble(_Rumble);
                }
            }
        }

        protected void OnStop()
        {
            _TouchCounter = 0;
            if (_Controller)
            {
                _Controller.StopRumble(_Rumble);
            }
        }
    }
}