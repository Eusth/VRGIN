using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core.Helpers
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
        
        public RumbleSession(ushort microDuration, float milliInterval) {
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

        public void Consume() {
            if(Lifetime > 0 && (Time.time - _Time > Lifetime))
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

        public void Consume() {
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
}
