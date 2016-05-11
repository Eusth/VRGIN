using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace VRGIN.Core.Controls
{

    /// <summary>
    /// A tool that can be used with a Vive controller.
    /// </summary>
    public abstract class Tool : ProtectedBehaviour
    {

        protected SteamVR_TrackedObject Tracking;

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
            base.OnAwake();

            Tracking = GetComponent<SteamVR_TrackedObject>();
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
            Icon.SetActive(true);
        }

        protected virtual void OnDisable()
        {
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
