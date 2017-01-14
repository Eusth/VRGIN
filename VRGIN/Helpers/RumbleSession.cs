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


    public class VelocityRumble : IRumbleSession
    {
        public bool IsOver
        {
            get; set;
        }

        public ushort MicroDuration
        {
            get
            {
                return (ushort)(_MicroDuration + (Device.velocity.magnitude / _MaxVelocity) * (_MaxMicroDuration - _MicroDuration));
            }
        }

        public float MilliInterval
        {
            get
            {
                return Mathf.Lerp(_MilliInterval, _MaxMilliInterval, Device.velocity.magnitude / _MaxVelocity);
            }
        }

        readonly ushort _MicroDuration;
        readonly float _MilliInterval;
        readonly float _MaxVelocity;
        readonly ushort _MaxMicroDuration;
        readonly float _MaxMilliInterval;

        public SteamVR_Controller.Device Device { get; set; }

        public VelocityRumble(SteamVR_Controller.Device device, ushort microDuration, float milliInterval, float maxVelocity, ushort maxMicroDuration, float maxMilliInterval)
        {
            Device = device;
            this._MaxMilliInterval = maxMilliInterval;
            this._MaxMicroDuration = maxMicroDuration;
            this._MaxVelocity = maxVelocity;
            this._MilliInterval = milliInterval;
            this._MicroDuration = microDuration;
        }

        public int CompareTo(IRumbleSession other)
        {
            return MicroDuration.CompareTo(other.MicroDuration);
        }

        public void Consume()
        {

        }
    }


    public class TravelDistanceRumble : IRumbleSession
    {
        private Transform _Transform;
        private float _Distance;
        protected Vector3 PrevPosition;
        protected Vector3 CurrentPosition;

        private bool _UseLocalPosition = false;
        public bool UseLocalPosition { get { return _UseLocalPosition; } set { _UseLocalPosition = value; Reset(); } }

        public void Reset()
        {
            PrevPosition = _UseLocalPosition ? _Transform.localPosition : _Transform.position;
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
                CurrentPosition = _UseLocalPosition ? _Transform.localPosition : _Transform.position;
                var distance = DistanceTraveled;
                if (distance > _Distance)
                {
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
