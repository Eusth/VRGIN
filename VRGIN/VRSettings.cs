using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace VRGIN.Core
{
    /// <summary>
    /// Class that holds settings for VR. Saved as an XML file.
    /// </summary>
    [XmlRoot("Settings")]
    public class VRSettings
    {
        private VRSettings _OldSettings;
        private IDictionary<string, IList<EventHandler<PropertyChangedEventArgs>>> _Listeners = new Dictionary<string, IList<EventHandler<PropertyChangedEventArgs>>>();

        [XmlIgnore]
        public string Path { get; set; }

        private float _Distance = 0.3f;
        public float Distance { get { return _Distance; } set { _Distance = Mathf.Clamp(value, 0.1f, 10f); TriggerPropertyChanged("Distance"); } }

        private float _Angle = 170f;
        public float Angle { get { return _Angle; } set { _Angle = Mathf.Clamp(value, 50f, 360f); TriggerPropertyChanged("Angle"); } }

        private float _IPDScale = 1f;
        public float IPDScale { get { return _IPDScale; } set { _IPDScale = Mathf.Clamp(value, 0.01f, 10f); TriggerPropertyChanged("IPDScale"); } }

        private float _OffsetY = 0f;
        public float OffsetY { get { return _OffsetY; } set { _OffsetY = value; TriggerPropertyChanged("OffsetY"); } }

        private float _Rotation = 0f;
        public float Rotation { get { return _Rotation; } set { _Rotation = value; TriggerPropertyChanged("Rotation"); } }

        public event EventHandler<PropertyChangedEventArgs> PropertyChanged = delegate { };

        public VRSettings()
        {
            PropertyChanged += Distribute;

            _OldSettings = this.MemberwiseClone() as VRSettings;
        }

        private void TriggerPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public virtual void Save()
        {
            Save(Path);
        }

        public virtual void Save(string path)
        {
            if(path != null)
            {
                var serializer = new XmlSerializer(GetType());
                using (var stream = File.OpenWrite(path))
                {
                    serializer.Serialize(stream, this);
                }
            }

            _OldSettings = this.MemberwiseClone() as VRSettings;
        }

        public static T Load<T>(string path) where T : VRSettings
        {
            if(!File.Exists(path))
            {
                var settings = Activator.CreateInstance<T>();
                settings.Save(path);
                return settings;
            }  else { 
             var serializer = new XmlSerializer(typeof(T));
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    var settings = serializer.Deserialize(stream) as T;
                    settings.Path = path;
                    return settings;
                }
            }
        }

        public void AddListener(string property, EventHandler<PropertyChangedEventArgs> handler)
        {
            if(!_Listeners.ContainsKey(property))
            {
                _Listeners[property] = new List<EventHandler<PropertyChangedEventArgs>>();
            }

            _Listeners[property].Add(handler);
        }


        public void RemoveListener(string property, EventHandler<PropertyChangedEventArgs> handler)
        {
            if (_Listeners.ContainsKey(property))
            {
                _Listeners[property].Remove(handler);
            }
        }

        private void Distribute(object sender, PropertyChangedEventArgs e)
        {
            if (!_Listeners.ContainsKey(e.PropertyName))
            {
                _Listeners[e.PropertyName] = new List<EventHandler<PropertyChangedEventArgs>>();
            }

            foreach (var listener in _Listeners[e.PropertyName])
            {
                listener(sender, e);
            }
        }

        public void Reset()
        {
            this.CopyFrom(new VRSettings());
        }

        public void Reload()
        {
            this.CopyFrom(_OldSettings);
        }

        public void CopyFrom(VRSettings settings)
        {
            foreach(var key in _Listeners.Keys)
            {
                var prop = settings.GetType().GetProperty(key, BindingFlags.Instance | BindingFlags.Public);
                if(prop != null)
                {
                    try
                    {
                        prop.SetValue(this, prop.GetValue(settings, null), null);
                    } catch(Exception e)
                    {
                        Logger.Warn(e);
                    }
                }
            }
        } 

        
    }
}
