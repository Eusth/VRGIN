using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace VRGIN.Controls.Tools
{

    /// <summary>
    /// A tool that can be used with a Vive controller.
    /// </summary>
    public abstract class Tool : ProtectedBehaviour
    {

        protected SteamVR_TrackedObject Tracking;
        protected Controller Owner;

        public abstract Texture2D Image
        {
            get;
        }

        public GameObject Icon
        {
            get; set;
        }

        protected override void OnStart()
        {
            base.OnStart();

            Tracking = GetComponent<SteamVR_TrackedObject>();
            Owner = GetComponent<Controller>();
        }

        protected abstract void OnDestroy();


        /// <summary>
        /// Gets whether or not the attached controlller is tracking.
        /// </summary>
        protected bool IsTracking
        {
            get
            {
                return Tracking && Tracking.isValid;
            }
        }

        /// <summary>
        /// Gets the attached controller input object.
        /// </summary>
        protected SteamVR_Controller.Device Controller
        {
            get
            {
                return SteamVR_Controller.Input((int)Tracking.index);
            }
        }


        protected virtual void OnEnable()
        {
            Core.Logger.Info("On Enable: {0}", GetType().Name);

            Icon.SetActive(true);
        }

        protected virtual void OnDisable()
        {
            Core.Logger.Info("On Disable: {0}", GetType().Name);
            Icon.SetActive(false);
        }

        public virtual List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>();
        }

        protected Transform FindAttachPosition(String name)
        {
            return transform.GetComponentsInChildren<Transform>().Where(t => t.name == name).First().Find("attach");
        }

    }
}
