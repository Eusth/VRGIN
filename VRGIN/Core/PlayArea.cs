using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    public struct PlayArea
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
    }
}
