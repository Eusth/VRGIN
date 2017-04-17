using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Helpers;
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

        /// <summary>
        /// Gets or sets the distance between the camera and the GUI at [0,0,0] [seated]
        /// </summary>
        [XmlComment("The distance between the camera and the GUI at [0,0,0] [seated]")]
        public float Distance { get { return _Distance; } set { _Distance = Mathf.Clamp(value, 0.1f, 10f); TriggerPropertyChanged("Distance"); } }
        private float _Distance = 0.3f;

        /// <summary>
        /// Gets or sets the width of the arc the GUI takes up. [seated]
        /// </summary>
        [XmlComment("The width of the arc the GUI takes up. [seated]")]
        public float Angle { get { return _Angle; } set { _Angle = Mathf.Clamp(value, 50f, 360f); TriggerPropertyChanged("Angle"); } }
        private float _Angle = 170f;

        /// <summary>
        /// Gets or sets the scale of the camera. The higher, the more gigantic the player is.
        /// </summary>
        [XmlComment("Scale of the camera. The higher, the more gigantic the player is.")]
        public float IPDScale { get { return _IPDScale; } set { _IPDScale = Mathf.Clamp(value, 0.01f, 50f); TriggerPropertyChanged("IPDScale"); } }
        private float _IPDScale = 1f;

        /// <summary>
        /// Gets or sets the vertical offset of the GUI in meters. [seated]
        /// </summary>
        [XmlComment("The vertical offset of the GUI in meters. [seated]")]
        public float OffsetY { get { return _OffsetY; } set { _OffsetY = value; TriggerPropertyChanged("OffsetY"); } }
        private float _OffsetY = 0f;

        /// <summary>
        /// Gets or sets by how many degrees the GUI is rotated (around the y axis) [seated]
        /// </summary>
        [XmlComment("Degrees the GUI is rotated around the y axis [seated]")]
        public float Rotation { get { return _Rotation; } set { _Rotation = value; TriggerPropertyChanged("Rotation"); } }
        private float _Rotation = 0f;

        /// <summary>
        /// Gets or sets whether or not rumble is activated.
        /// </summary>
        [XmlComment("Whether or not rumble is activated.")]
        public bool Rumble { get { return _Rumble; } set { _Rumble = value; TriggerPropertyChanged("Rumble"); } }
        private bool _Rumble = true;

        /// <summary>
        /// Gets or sets the render scale of the renderer. Increase for better quality but less performance, decrease for more performance but poor quality.
        /// </summary>
        [XmlComment("The render scale of the renderer. Increase for better quality but less performance, decrease for more performance but poor quality. ]0..2]")]
        public float RenderScale { get { return _RenderScale; } set { _RenderScale = Mathf.Clamp(value, 0.1f, 4f); TriggerPropertyChanged("RenderScale"); } }
        private float _RenderScale = 1f;

        [XmlComment("Whether or not to display anything on the mirror screen. (Broken)")]
        public bool MirrorScreen { get { return _MirrorScreen; } set { _MirrorScreen = value; TriggerPropertyChanged("MirrorScreen"); } }
        private bool _MirrorScreen = false;

        /// <summary>
        /// Gets or sets whether or not rotating around the horizontal axis is allowed.
        /// </summary>
        [XmlComment("Whether or not rotating around the horizontal axis is allowed.")]
        public bool PitchLock { get { return _PitchLock; } set { _PitchLock = value; TriggerPropertyChanged("PitchLock"); } }
        private bool _PitchLock = true;

        /// <summary>
        /// Gets or sets the curviness of the monitor in seated mode.
        /// </summary>
        [XmlComment("The curviness of the monitor in seated mode.")]
        public CurvinessState Projection { get { return _Projection; } set { _Projection = value; TriggerPropertyChanged("Projection"); } }
        private CurvinessState _Projection = CurvinessState.Curved;
        
        /// <summary>
        /// Gets or sets whether or not speech recognition is enabled.
        /// </summary>
        [XmlComment("Whether or not speech recognition is enabled. Refer to the manual for details.")]
        public bool SpeechRecognition { get { return _SpeechRecognition; } set { _SpeechRecognition = value; TriggerPropertyChanged("SpeechRecognition"); } }
        private bool _SpeechRecognition = false;
        
        /// <summary>
        /// Gets or sets which locale to use for speech recognition. A dictionary file will automatically be generated at <i>UserData/dictionaries</i>.
        /// </summary>
        [XmlComment("Locale to use for speech recognition. Make sure that you have installed the corresponding language pack. A dictionary file will automatically be generated at `UserData/dictionaries`.")]
        public string Locale { get { return _Locale; } set { _Locale = value; TriggerPropertyChanged("Locale"); } }
        private string _Locale = "en-US";

        /// <summary>
        /// Gets or sets whether or not Leap Motion support is activated.
        /// </summary>
        [XmlComment("Whether or not Leap Motion support is activated.")]
        public bool Leap { get { return _Leap; } set { _Leap = value; TriggerPropertyChanged("Leap"); } }
        private bool _Leap = false;

        [XmlComment("Determines the rotation mode. If enabled, pulling the trigger while grabbing will immediately rotate you. When disabled, doing the same thing will let you 'drag' the view.")]
        public bool GrabRotationImmediateMode { get { return _GrabRotationImmediateMode; } set { _GrabRotationImmediateMode = value; TriggerPropertyChanged("GrabRotationImmediateMode"); } }
        private bool _GrabRotationImmediateMode = true;

        [XmlComment("How quickly the the view should rotate when doing so with the controllers.")]
        public float RotationMultiplier { get { return _RotationMultiplier; } set { _RotationMultiplier = value; TriggerPropertyChanged("RotationMultiplier"); } }
        private float _RotationMultiplier = 1f;

        //[XmlElement("VRGIN.Shortcuts")]
        [XmlComment("Shortcuts used by VRGIN. Refer to https://docs.unity3d.com/ScriptReference/KeyCode.html for a list of available keys.")]
        public virtual Shortcuts Shortcuts { get { return _Shortcuts; } protected set { _Shortcuts = value; } }
        private Shortcuts _Shortcuts = new Shortcuts();


        public CaptureConfig Capture { get { return _CaptureConfig; } protected set { _CaptureConfig = value; } }
        private CaptureConfig _CaptureConfig = new CaptureConfig();

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

                PostProcess(path);

                Path = path;
            }

            _OldSettings = this.MemberwiseClone() as VRSettings;
        }

        protected virtual void PostProcess(string path)
        {
            // Add comments
            var doc = XDocument.Load(path);
            foreach(var element in doc.Root.Elements())
            {
                var property = FindProperty(element.Name.LocalName);
                if(property != null)
                {
                    var commentAttribute = property.GetCustomAttributes(typeof(XmlCommentAttribute), true).FirstOrDefault() as XmlCommentAttribute;
                    if(commentAttribute != null)
                    {
                        element.AddBeforeSelf(new XComment(" " + commentAttribute.Value + " "));
                    }
                }
            }
            doc.Save(path);
        }

        private PropertyInfo FindProperty(string name)
        {
            return GetType()
                .FindMembers(MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public, Type.FilterName, name)
                .FirstOrDefault() as PropertyInfo;
        }

        /// <summary>
        /// Loads the settings from a file. Generic to enable handling of sub classes.
        /// </summary>
        /// <typeparam name="T">Type of the settings</typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T Load<T>(string path) where T : VRSettings
        {
            try
            {
                if (!File.Exists(path))
                {
                    var settings = Activator.CreateInstance<T>();
                    settings.Save(path);
                    return settings;
                }
                else
                {
                    var serializer = new XmlSerializer(typeof(T));
                    using (var stream = new FileStream(path, FileMode.Open))
                    {
                        var settings = serializer.Deserialize(stream) as T;
                        settings.Path = path;
                        return settings;
                    }
                }
            } catch(Exception e)
            {
                VRLog.Error("Fatal exception occured while loading XML! (Make sure System.Xml exists!) {0}", e);
                throw e;
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
    
    public class Shortcuts
    {
        public XmlKeyStroke ResetView = new XmlKeyStroke("F12");
        public XmlKeyStroke ChangeMode = new XmlKeyStroke("Ctrl+C, Ctrl+C");
        public XmlKeyStroke ShrinkWorld = new XmlKeyStroke("Alt + KeypadMinus", KeyMode.Press);
        public XmlKeyStroke EnlargeWorld = new XmlKeyStroke("Alt + KeypadPlus", KeyMode.Press);
        public XmlKeyStroke ToggleUserCamera = new XmlKeyStroke("Ctrl+C, Ctrl+V");
        public XmlKeyStroke SaveSettings = new XmlKeyStroke("Alt + S");
        public XmlKeyStroke LoadSettings = new XmlKeyStroke("Alt + L");
        public XmlKeyStroke ResetSettings = new XmlKeyStroke("Ctrl + Alt + L");
        public XmlKeyStroke ApplyEffects = new XmlKeyStroke("Ctrl + F5");

        [XmlElement("GUI.Raise")]
        public XmlKeyStroke GUIRaise = new XmlKeyStroke("KeypadMinus", KeyMode.Press);
        [XmlElement("GUI.Lower")]
        public XmlKeyStroke GUILower = new XmlKeyStroke("KeypadPlus", KeyMode.Press);
        [XmlElement("GUI.IncreaseAngle")]
        public XmlKeyStroke GUIIncreaseAngle = new XmlKeyStroke("Ctrl + KeypadMinus", KeyMode.Press);
        [XmlElement("GUI.DecreaseAngle")]
        public XmlKeyStroke GUIDecreaseAngle = new XmlKeyStroke("Ctrl + KeypadPlus", KeyMode.Press);
        [XmlElement("GUI.IncreaseDistance")]
        public XmlKeyStroke GUIIncreaseDistance = new XmlKeyStroke("Shift + KeypadMinus", KeyMode.Press);
        [XmlElement("GUI.DecreaseDistance")]
        public XmlKeyStroke GUIDecreaseDistance = new XmlKeyStroke("Shift + KeypadPlus", KeyMode.Press);
        [XmlElement("GUI.RotateRight")]
        public XmlKeyStroke GUIRotateRight= new XmlKeyStroke("Ctrl + Shift + KeypadMinus", KeyMode.Press);
        [XmlElement("GUI.RotateLeft")]
        public XmlKeyStroke GUIRotateLeft = new XmlKeyStroke("Ctrl + Shift + KeypadPlus", KeyMode.Press);
        [XmlElement("GUI.ChangeProjection")]
        public XmlKeyStroke GUIChangeProjection = new XmlKeyStroke("F4");

        public XmlKeyStroke ToggleRotationLock = new XmlKeyStroke("F5");
        public XmlKeyStroke ImpersonateApproximately = new XmlKeyStroke("Ctrl + X");
        public XmlKeyStroke ImpersonateExactly = new XmlKeyStroke("Ctrl + Shift + X");
    }

    public class CaptureConfig
    {
        public XmlKeyStroke Shortcut = new XmlKeyStroke("Ctrl + F12");
        public bool Stereoscopic = true;
        public bool WithEffects = true;
        public bool SetCameraUpright = true;
        public bool HideGUI = false;
        //public bool HideControllers = false;
    }
    public class XmlKeyStroke
    {
        [XmlAttribute("on")]
        public KeyMode CheckMode { get; private set; }
        
        [XmlText]
        public string Keys { get; private set; }

        public XmlKeyStroke()
        {
        }

        public XmlKeyStroke(string strokeString, KeyMode mode = KeyMode.PressUp)
        {
            CheckMode = mode;
            Keys = strokeString;
        }

        public KeyStroke[] GetKeyStrokes()
        {
            return Keys.Split(',', '|').Select(part => new KeyStroke(part.Trim())).ToArray();
        }
    }
    

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class XmlCommentAttribute : Attribute
    {
        public XmlCommentAttribute(string value)
        {
            Value = value;
        }
        public string Value { get; set; }
    }
}
