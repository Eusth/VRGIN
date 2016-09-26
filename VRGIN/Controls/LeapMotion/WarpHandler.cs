using Leap.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.U46.Visuals;

namespace VRGIN.Controls.LeapMotion
{
    public class WarpHandler : ProtectedBehaviour
    {
        PlayAreaVisualization _Visualization;

        PalmDirectionDetector _PalmDownwardsDetector;
        ExtendedFingerDetector _ExtendedFingerDetector;

        DetectorLogicGate _OpenPalmDownwardsDetector;
        ExtendedFingerDetector _Fistdetector;

        HandAttachments _Hand;
        float _LastFist;
        float _LastShow;

        Vector3 _PrevPosition;
        bool _MoveHeight;
        float _HeightChange;


        private const float TIME_THRESHOLD = 0.3f;

        bool _Showing = false;
        
        protected override void OnStart()
        {
            base.OnStart();
            _Visualization = PlayAreaVisualization.Create();
            DontDestroyOnLoad(_Visualization.gameObject);
            _Visualization.Disable();

            _Hand = GetComponent<HandAttachments>();

            SetUpDetectors();
        }

        protected virtual void OnDestroy()
        {
            Destroy(_Visualization.gameObject);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (_Showing)
            {
                // Update play area position
                var hand = _Hand.GetLeapHand();
                Vector3 currentPosition = _Hand.Palm.position;
                var motion = currentPosition - _PrevPosition;

                if (motion.magnitude < 0.1f)
                {
                    float heightChange = 0;
                    //if(_MoveHeight)
                    //{
                    //    heightChange = motion.y;
                    //} else
                    //{
                    //    _HeightChange += motion.y;
                    //    if (Mathf.Abs(_HeightChange) > 0.05f)
                    //    {
                    //        _MoveHeight = true;
                    //        heightChange = _HeightChange;
                    //    }
                    //}

                    _Visualization.Area.Position += Vector3.Scale(new Vector3(motion.x, heightChange, motion.z), new Vector3(10, 5, 10));

                    _PrevPosition = currentPosition;
                }
            }
        }

        void SetUpDetectors()
        {
            var detectorHolder = UnityHelper.CreateGameObjectAsChild("Warp Detector Holder", transform).gameObject;

            _OpenPalmDownwardsDetector = detectorHolder.AddComponent<DetectorLogicGate>();
            
            _PalmDownwardsDetector = detectorHolder.AddComponent<PalmDirectionDetector>();
            _PalmDownwardsDetector.HandModel = _Hand;
            _PalmDownwardsDetector.PointingDirection = new Vector3(0,-1,0.5f).normalized;
            _PalmDownwardsDetector.OnAngle = 10;
            //_PalmDownwardsDetector.OffAngle = 60;


            _ExtendedFingerDetector = UnityHelper.CreateGameObjectAsChild("_ExtendedFingerDetector", detectorHolder.transform).gameObject.AddComponent<ExtendedFingerDetector>();
            _ExtendedFingerDetector.HandModel = _Hand;
            _ExtendedFingerDetector.Thumb = PointingState.Extended;
            _ExtendedFingerDetector.Index = PointingState.Extended;
            _ExtendedFingerDetector.Middle = PointingState.Extended;
            _ExtendedFingerDetector.Ring = PointingState.Extended;
            _ExtendedFingerDetector.Pinky = PointingState.Extended;

            _Fistdetector = UnityHelper.CreateGameObjectAsChild("_Fistdetector", detectorHolder.transform).gameObject.AddComponent<ExtendedFingerDetector>();
            _Fistdetector.HandModel = _Hand;
            _Fistdetector.Thumb = PointingState.Either;
            _Fistdetector.Index = PointingState.NotExtended;
            _Fistdetector.Middle = PointingState.NotExtended;
            _Fistdetector.Ring = PointingState.NotExtended;
            _Fistdetector.Pinky = PointingState.NotExtended;


            _OpenPalmDownwardsDetector.AddDetector(_PalmDownwardsDetector);
            _OpenPalmDownwardsDetector.AddDetector(_ExtendedFingerDetector);
            _OpenPalmDownwardsDetector.OnActivate.AddListener(OnOpenPalmDownwardStart);
            _OpenPalmDownwardsDetector.OnDeactivate.AddListener(OnOpenPalmDownwardEnd);
            
            _Fistdetector.OnActivate.AddListener(OnFist);
        }

        private void OnFist()
        {
            VRLog.Info("Fist");
            if (Time.unscaledTime - _LastShow < TIME_THRESHOLD)
            {
                // Warp!
                _Visualization.Area.Apply();
            }
            else
            {
                _LastFist = Time.unscaledTime;
            }
        }

        private void OnOpenPalmDownwardEnd()
        {

            if (_Showing)
            {
                VRLog.Info("Stop!");
                _LastShow = Time.unscaledTime;
                _Visualization.Disable();
                _Showing = false;
            }
        }

        private void OnOpenPalmDownwardStart()
        {
            VRLog.Info("Palm");

            if (_Showing) return;

            if (Time.unscaledTime - _LastFist < TIME_THRESHOLD)
            {
                VRLog.Info("Visualize!");
                var hand = _Hand.GetLeapHand();
                _Visualization.Area.Height = VR.Camera.Origin.position.y;
                var plane = new Plane(Vector3.up, _Visualization.Area.Position);

                _Visualization.Enable();
                _Showing = true;
                _MoveHeight = false;
                _HeightChange = 0;
                _PrevPosition = _Hand.Palm.position;

                float enter;
                var ray = new Ray(hand.StabilizedPalmPosition.ToVector3(), hand.PalmNormal.ToVector3());
                if (plane.Raycast(ray, out enter))
                {
                    if (enter < 5)
                    {
                        _Visualization.Area.Position = ray.origin + ray.direction * enter;
                    }
                }

            }
            //throw new NotImplementedException();
        }
    }
}
