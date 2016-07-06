using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using static VRGIN.Visuals.GUIMonitor;

namespace VRGIN.Core
{
    /// <summary>
    /// Class that holds settings for VR. Saved as an XML file.
    /// 
    /// In order to create your own settings file, extend this class and add your own properties. Make sure to call <see cref="TriggerPropertyChanged(string)"/> if you want to use
    /// the events.
    /// IMPORTANT: When extending, add an XmlRoot annotation to the class like so:
    /// <code>[XmlRoot("Settings")]</code>
    /// </summary>
    [XmlRoot("Settings")]
    public class VRSettings
    {
        private VRSettings _OldSettings;
        private IDictionary<string, IList<EventHandler<PropertyChangedEventArgs>>> _Listeners = new Dictionary<string, IList<EventHandler<PropertyChangedEventArgs>>>();

        [XmlIgnore]
        public string Path { get; set; }

        private float _Distance = 0.3f;
        /// <summary>
        /// Gets or sets the distance between the camera and the GUI at [0,0,0] [seated]
        /// </summary>
        public float Distance { get { return _Distance; } set { _Distance = Mathf.Clamp(value, 0.1f, 10f); TriggerPropertyChanged("Distance"); } }

        private float _Angle = 170f;
        /// <summary>
        /// Gets or sets the width of the arc the GUI takes up. [seated]
        /// </summary>
        public float Angle { get { return _Angle; } set { _Angle = Mathf.Clamp(value, 50f, 360f); TriggerPropertyChanged("Angle"); } }

        private float _IPDScale = 1f;
        /// <summary>
        /// Gets or sets the scale of the camera. The higher, the more gigantic the player is.
        /// </summary>
        public float IPDScale { get { return _IPDScale; } set { _IPDScale = Mathf.Clamp(value, 0.01f, 10f); TriggerPropertyChanged("IPDScale"); } }

        private float _OffsetY = 0f;
        /// <summary>
        /// Gets or sets the vertical offset of the GUI in meters. [seated]
        /// </summary>
        public float OffsetY { get { return _OffsetY; } set { _OffsetY = value; TriggerPropertyChanged("OffsetY"); } }

        private float _Rotation = 0f;
        /// <summary>
        /// Gets or sets by how many degrees the GUI is rotated (around the y axis) [seated]
        /// </summary>
        public float Rotation { get { return _Rotation; } set { _Rotation = value; TriggerPropertyChanged("Rotation"); } }

        private bool _Rumble = true;
        /// <summary>
        /// Gets or sets whether or not rumble is activated.
        /// </summary>
        public bool Rumble { get { return _Rumble; } set { _Rumble = value; TriggerPropertyChanged("Rumble"); } }

        private float _RenderScale = 1f;
        /// <summary>
        /// Gets or sets the render scale of the renderer. Increase for better quality but less performance, decrease for more performance but poor quality.
        /// </summary>
        public float RenderScale { get { return _RenderScale; } set { _RenderScale = Mathf.Clamp(value, 0.1f, 4f); TriggerPropertyChanged("RenderScale"); } }

        private bool _MirrorScreen = false;
        public bool MirrorScreen { get { return _MirrorScreen; } set { _MirrorScreen = value; TriggerPropertyChanged("MirrorScreen"); } }

        private bool _PitchLock = true;
        /// <summary>
        /// Gets or sets whether or not rotating around the horizontal axis is allowed.
        /// </summary>
        public bool PitchLock { get { return _PitchLock; } set { _PitchLock = value; TriggerPropertyChanged("PitchLock"); } }

        private CurvinessState _Projection = CurvinessState.Curved;
        /// <summary>
        /// Gets or sets the curviness of the monitor in seated mode.
        /// </summary>
        public CurvinessState Projection { get { return _Projection; } set { _Projection = value; TriggerPropertyChanged("Projection"); } }

        public event EventHandler<PropertyChangedEventArgs> PropertyChanged = delegate { };

        public VRSettings()
        {
            PropertyChanged += Distribute;

            _OldSettings = this.MemberwiseClone() as VRSettings;
        }
        
        /// <summary>
        /// Triggers a PropertyChanged event and notifies the listeners.
        /// </summary>
        /// <param name="name"></param>
        protected void TriggerPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Saves the settings to the path that was initially set.
        /// </summary>
        public virtual void Save()
        {
            Save(Path);
        }

        /// <summary>
        /// Saves the settings to a given path.
        /// </summary>
        /// <param name="path"></param>
        public virtual void Save(string path)
        {
            if(path != null)
            {
                var serializer = new XmlSerializer(GetType());
                using (var stream = File.OpenWrite(path))
                {
                    stream.SetLength(0);
                    serializer.Serialize(stream, this);
                }
            }

            _OldSettings = this.MemberwiseClone() as VRSettings;
        }

        /// <summary>
        /// Loads the settings from a file. Generic to enable handling of sub classes.
        /// </summary>
        /// <typeparam name="T">Type of the settings</typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Adds a listener for a certain property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="handler"></param>
        public void AddListener(string property, EventHandler<PropertyChangedEventArgs> handler)
        {
            if(!_Listeners.ContainsKey(property))
            {
                _Listeners[property] = new List<EventHandler<PropertyChangedEventArgs>>();
            }

            _Listeners[property].Add(handler);
        }


        /// <summary>
        /// Removes a listener for a certain property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="handler"></param>
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

        /// <summary>
        /// Resets all values.
        /// </summary>
        public void Reset()
        {
            var blueprint = Activator.CreateInstance(this.GetType()) as VRSettings;
            this.CopyFrom(blueprint);
        }

        /// <summary>
        /// Restores the last saved state.
        /// </summary>
        public void Reload()
        {
            this.CopyFrom(_OldSettings);
        }

        /// <summary>
        /// Clone settings from another instance.
        /// </summary>
        /// <param name="settings"></param>
        public void CopyFrom(VRSettings settings)
        {
            foreach (var key in _Listeners.Keys)
            {
                var prop = settings.GetType().GetProperty(key, BindingFlags.Instance | BindingFlags.Public);
                if (prop != null)
                {
                    try
                    {
                        prop.SetValue(this, prop.GetValue(settings, null), null);
                    }
                    catch (Exception e)
                    {
                        Logger.Warn(e);
                    }
                }
            }
        } 

        
    }
}
