using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Visuals;

namespace VRGIN.Core
{
    public interface IVRManagerContext
    {
        /// <summary>
        /// Gets the layer where the VR GUI should be placed. This is mainly used for raycasting and should ideally not be used by anything else.
        /// </summary>
        string GuiLayer { get; }

        /// <summary>
        /// Gets the layer the game uses for its UI.
        /// </summary>
        string UILayer { get; }

        /// <summary>
        /// Gets the mask that can be used for the camera to *not* display the game's GUI. The VR cameras will ignore this, the GUI camera will look for this.
        /// This is almost the same as <see cref="UILayer"/> but more flexible.
        /// </summary>
        int UILayerMask { get; }

        /// <summary>
        /// Gets a mask of layers to ignore in the VR camera.
        /// </summary>
        int IgnoreMask { get; }

        /// <summary>
        /// Gets the color used for the tools and effects. (e.g. teleport)
        /// </summary>
        Color PrimaryColor { get; }

        /// <summary>
        /// Gets the palette that contains all materials used by the library.
        /// </summary>
        IMaterialPalette Materials { get; }

        /// <summary>
        /// Gets the settings object.
        /// </summary>
        VRSettings Settings { get; }

        /// <summary>
        /// Gets the layer that can be used to add objects that will be ignored by the in-game player but that will appear on screen.
        /// </summary>
        string InvisibleLayer { get; }

        /// <summary>
        /// Gets whether the library should make a cursor of its own. Needed when the game uses a hardware cursor.
        /// </summary>
        bool SimulateCursor { get; }

        /// <summary>
        /// Gets whether or not the GUI should run in an alternative mode with custom sorting.
        /// </summary>
        bool GUIAlternativeSortingMode { get; }

        /// <summary>
        /// Gets an enum type that contains a list of voice commands that you can listen to. Return <see cref="VRGIN.Controls.Speech.VoiceCommand"/> if not used.
        /// </summary>
        Type VoiceCommandType { get; }


        /// <summary>
        /// Gets the near clip plane for the GUI camera. [e.g. 0]
        /// </summary>
        float GuiNearClipPlane { get; }

        /// <summary>
        /// Gets the far clip plane for the GUI camera. [e.g. 10000]
        /// </summary>
        float GuiFarClipPlane { get; }
    }
}
