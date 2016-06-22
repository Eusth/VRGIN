using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    public interface IRumbleSession : IComparable<IRumbleSession>
    {
        bool IsOver { get; }

        ushort MicroDuration { get; }
        float MilliInterval { get; }

        void Consume();
    }

    public class RumbleSession : IRumbleSession
    {
        public bool IsOver { get; private set; }

        public ushort MicroDuration
        {
            get; set;
        }

        public float MilliInterval
        {
            get; set;
        }

        public float Lifetime
        {
            get; set;
        }

        private float _Time = 0;

        public RumbleSession(ushort microDuration, float milliInterval)
        {
            MicroDuration = microDuration;
            MilliInterval = milliInterval;
            _Time = Time.time;
        }

        public RumbleSession(ushort microDuration, float milliInterval, float lifetime)
        {
            MicroDuration = microDuration;
            MilliInterval = milliInterval;
            Lifetime = lifetime;
            _Time = Time.time;
        }

        public void Close()
        {
            IsOver = true;
        }

        public int CompareTo(IRumbleSession other)
        {
            return MicroDuration.CompareTo(other.MicroDuration);
        }

        public void Restart()
        {
            _Time = Time.time;
        }

        public void Consume()
        {
            if (Lifetime > 0 && (Time.time - _Time > Lifetime))
            {
                IsOver = true;
            }
        }


    }

    public class RumbleImpulse : IRumbleSession
    {

        private bool _Over = false;

        public bool IsOver
        {
            get
            {
                return _Over;
            }
        }

        public ushort MicroDuration
        {
            get; set;
        }

        public float MilliInterval
        {
            get
            {
                return 0;
            }
        }

        public void Consume()
        {
            _Over = true;
        }

        public RumbleImpulse(ushort strength)
        {
            MicroDuration = strength;
        }


        public int CompareTo(IRumbleSession other)
        {
            return MicroDuration.CompareTo(other.MicroDuration);
        }
    }

    public class TravelDistanceRumble : IRumbleSession
    {
        private Transform _Transform;
        private float _Distance;
        protected Vector3 PrevPosition;
        protected Vector3 CurrentPosition;

        public void Reset()
        {
            PrevPosition = _Transform.position;
        }
        public bool IsOver
        {
            get; private set;
        }

        public ushort MicroDuration
        {
            get; set;
        }

        public float MilliInterval
        {
            get
            {
                CurrentPosition = _Transform.position;
                var distance = DistanceTraveled;
                if (distance > _Distance)
                {
                    VRLog.Info(distance);
                    PrevPosition = CurrentPosition;

                    return 0;
                }
                else
                {
                    return float.MaxValue;
                }
            }
        }

        public TravelDistanceRumble(ushort intensity, float distance, Transform transform)
        {
            MicroDuration = intensity;
            _Transform = transform;
            _Distance = distance;
            PrevPosition = transform.position;
        }

        protected virtual float DistanceTraveled
        {
            get
            {
                return Vector3.Distance(PrevPosition, CurrentPosition);
            }
        }

        public int CompareTo(IRumbleSession other)
        {
            return MicroDuration.CompareTo(other.MicroDuration);
        }

        public void Consume()
        {
        }

        public void Close()
        {
            IsOver = true;
        }
    }

    public class AxisBoundTravelDistanceRumble : TravelDistanceRumble
    {
        private Vector3 _Axis;
        public AxisBoundTravelDistanceRumble(ushort intensity, float distance, Transform transform, Vector3 axis) : base(intensity, distance, transform)
        {
            _Axis = axis;
        }

        protected override float DistanceTraveled
        {
            get
            {
                return Mathf.Abs(Vector3.Dot(CurrentPosition - PrevPosition, _Axis));
            }
        }
    }
}
