using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        
        public RumbleSession(ushort microDuration, float milliInterval) {
            MicroDuration = microDuration;
            MilliInterval = milliInterval;
        }

        public void Close()
        {
            IsOver = true;
        }

        public int CompareTo(IRumbleSession other)
        {
            return MicroDuration.CompareTo(other.MicroDuration);
        }

        public void Consume() { }


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
