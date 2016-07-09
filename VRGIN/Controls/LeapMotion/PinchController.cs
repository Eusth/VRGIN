using Leap;
using Leap.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.U46.Helpers;
using VRGIN.Visuals;

namespace VRGIN.U46.Controls.Leap
{
    public class PinchController : ProtectedBehaviour
    {
        PinchDetector _Left;
        PinchDetector _Right;

        ProximityDetector _Proximity;

        DetectorLogicGate _StartDetector;
        DetectorLogicGate _Detector;

        bool _Pinching = false;

        GUIQuad _Current;
        GuiScaler _Scaler;
        private const float MIN_SCALE =0.3f;

        protected override void OnStart()
        {
            base.OnStart();
            SetUpDetectors();
        }

        void SetUpDetectors()
        {
            _Left = UnityHelper.CreateGameObjectAsChild("Pinch Detector", transform).gameObject.AddComponent<PinchDetector>();
            _Right = UnityHelper.CreateGameObjectAsChild("Pinch Detector", transform).gameObject.AddComponent<PinchDetector>();
            _Proximity = VR.Mode.LeftHand.PinchPoint.gameObject.AddComponent<ProximityDetector>();
            _Proximity.TargetObjects = new GameObject[] { VR.Mode.RightHand.PinchPoint.gameObject };
            _Proximity.OnDistance = 0.1f;
            _Proximity.OffDistance = 0.11f;
            _Detector = gameObject.AddComponent<DetectorLogicGate>();
            _StartDetector = gameObject.AddComponent<DetectorLogicGate>();

            _Left._handModel = VR.Mode.LeftHand;
            _Right._handModel = VR.Mode.RightHand;

            _StartDetector.AddDetector(_Left);
            _StartDetector.AddDetector(_Right);
            _StartDetector.AddDetector(_Proximity);

            _Detector.AddDetector(_Left);
            _Detector.AddDetector(_Right);

            _StartDetector.OnActivate.AddListener(OnStartPinch);
            _Detector.OnDeactivate.AddListener(OnStopPinch);
        }

        private void OnStartPinch()
        {
            _Pinching = true;

            if(_Current)
            {
                DestroyImmediate(_Current.gameObject);
            }
            _Current = GUIQuad.Create();
            _Current.transform.SetParent(VR.Camera.Origin, false);
            DontDestroyOnLoad(_Current);
            _Current.transform.position = Vector3.Lerp(VR.Mode.LeftHand.PinchPoint.position, VR.Mode.RightHand.PinchPoint.position, 0.5f);
            _Current.transform.rotation = Quaternion.Slerp(VR.Mode.LeftHand.PinchPoint.rotation, VR.Mode.RightHand.PinchPoint.rotation, 0.5f) * Quaternion.Euler(0, 0, 90);
            _Current.transform.localScale *= Vector3.Distance(VR.Mode.LeftHand.PinchPoint.position, VR.Mode.RightHand.PinchPoint.position);

            _Scaler = new GuiScaler(_Current, VR.Mode.LeftHand.PinchPoint, VR.Mode.RightHand.PinchPoint);
        }

        private void OnStopPinch()
        {
            if (!_Pinching) return;

            _Pinching = false;

            if (_Current && _Current.transform.localScale.magnitude < MIN_SCALE)
            {
                DestroyImmediate(_Current.gameObject);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if(_Pinching)
            {
                _Scaler.Update();
            }
        }

        

    }
}
