using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VRGIN.Core
{
    [XmlRoot("Settings")]
    public class VRSettings
    {
        private VRSettings _OldSettings;

        [XmlIgnore]
        public string Path { get; set; }

        private float _Distance = 0.3f;
        public float Distance { get { return _Distance; } set { _Distance = value; TriggerPropertyChanged("Distance"); } }

        private float _Angle = 170f;
        public float Angle { get { return _Angle; } set { _Angle = value; TriggerPropertyChanged("Angle"); } }

        private float _IPDScale = 1f;
        public float IPDScale { get { return _IPDScale; } set { _IPDScale = value; TriggerPropertyChanged("IPDScale"); } }

        private float _OffsetY = 0f;
        public float OffsetY { get { return _OffsetY; } set { _OffsetY = value; TriggerPropertyChanged("OffsetY"); } }
        

        public event EventHandler<PropertyChangedEventArgs> PropertyChanged = delegate { };
        private void TriggerPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public virtual void Save()
        {
            if(Path != null)
            {
                var serializer = new XmlSerializer(GetType());
                using (var stream = File.OpenWrite(Path))
                {
                    serializer.Serialize(stream, this);
                }
            }

            _OldSettings = this.MemberwiseClone() as VRSettings;
        }

        public static T Load<T>(string path) where T : VRSettings
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var settings = serializer.Deserialize(stream) as T;
                settings.Path = path;
                return settings;
            }
        }
    }
}
