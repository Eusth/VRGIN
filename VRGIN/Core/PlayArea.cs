using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    public class PlayArea
    {
        public float Scale { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public float Height
        {
            get
            {
                return Position.y;
            }
            set
            {
                Position = new Vector3(Position.x, value, Position.z);
            }
        }

        public PlayArea()
        {
            Scale = 1;
        }
        
        public void Apply()
        {
            var rotOffset = Quaternion.Euler(0, Rotation, 0);
            var steamCam = VR.Camera.SteamCam;

            steamCam.origin.position = Position
                - rotOffset * new Vector3(steamCam.head.transform.localPosition.x, 0, steamCam.head.transform.localPosition.z) * Scale;
            steamCam.origin.rotation = rotOffset;

            VR.Settings.IPDScale = Scale;
        }

        public void Reset()
        {
            Position = new Vector3(VR.Camera.Head.position.x, VR.Camera.Origin.position.y, VR.Camera.Head.position.z);
            Scale = VR.Settings.IPDScale;
            Rotation = VR.Camera.Origin.rotation.eulerAngles.y;
        }
    }
}
