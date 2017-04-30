using System;
using UnityEngine;
using VRGIN.Controls.Speech;
using VRGIN.Core;
using VRGIN.Visuals;

namespace VRGIN.Helpers
{
    /// <summary>
    /// Default IVRManagerContext with sensible defaults that you can extend.
    /// </summary>
    public class DefaultContext : IVRManagerContext
    {
        IMaterialPalette _Materials;
        VRSettings _Settings;

        public DefaultContext()
        {
            _Materials = CreateMaterialPalette();
            _Settings = CreateSettings();
        }

        protected virtual IMaterialPalette CreateMaterialPalette()
        {
            return new DefaultMaterialPalette();
        }

        protected virtual VRSettings CreateSettings()
        {
            return VRSettings.Load<VRSettings>("VRSettings.xml");
        }

        public virtual bool ConfineMouse
        {
            get
            {
                return true;
            }
        }

        public virtual bool EnforceDefaultGUIMaterials
        {
            get
            {
                return false;
            }
        }

        public virtual bool GUIAlternativeSortingMode
        {
            get
            {
                return false;
            }
        }

        public virtual float GuiFarClipPlane
        {
            get
            {
                return 10000;
            }
        }

        public virtual string GuiLayer
        {
            get
            {
                return "Default";
            }
        }

        public virtual float GuiNearClipPlane
        {
            get
            {
                return -10000;
            }
        }

        public virtual int IgnoreMask
        {
            get
            {
                return 0;
            }
        }

        public virtual string InvisibleLayer
        {
            get
            {
                return "Ignore Raycast";
            }
        }

        public IMaterialPalette Materials
        {
            get
            {
                return _Materials;
            }
        }

        public virtual float NearClipPlane
        {
            get
            {
                return 0.1f;
            }
        }

        public virtual GUIType PreferredGUI
        {
            get
            {
                return GUIType.uGUI;
            }
        }

        public virtual Color PrimaryColor
        {
            get
            {
                return Color.cyan;
            }
        }

        public virtual VRSettings Settings
        {
            get
            {
                return _Settings;
            }
        }

        public virtual bool SimulateCursor
        {
            get
            {
                return true;
            }
        }

        public virtual string UILayer
        {
            get
            {
                return "UI";
            }
        }

        public virtual int UILayerMask
        {
            get
            {
                return LayerMask.GetMask(UILayer);
            }
        }

        public virtual float UnitToMeter
        {
            get
            {
                return 1;
            }
        }

        public virtual Type VoiceCommandType
        {
            get
            {
                return typeof(VoiceCommand);
            }
        }
    }
}
