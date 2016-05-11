using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Core.Controls;

namespace VRGIN.Core.Modes
{
    public abstract class ControlMode : ProtectedBehaviour
    {
        public abstract void Impersonate(IActor actor);
        public abstract void OnDestroy();
        public Controller Left { get; private set; }
        public Controller Right { get; private set; }

        protected SteamVR_ControllerManager ControllerManager;

        protected override void OnStart()
        {
            CreateControllers();
        }

        /// <summary>
        /// Creates both controllers by using <see cref="CreateRightController"/> and <see cref="CreateLeftController"/>.
        /// Override those methods to change the controller implementation to be used.
        /// </summary>
        protected virtual void CreateControllers()
        {
            var steamCam = VR.Camera.SteamCam;

            steamCam.origin.gameObject.SetActive(false);
            {
                ControllerManager = steamCam.origin.gameObject.AddComponent<SteamVR_ControllerManager>();

                Left = CreateLeftController();
                Left.transform.SetParent(steamCam.origin);

                Right = CreateRightController();
                Right.transform.SetParent(steamCam.origin);

                Left.Other = Right;
                Right.Other = Left;

                ControllerManager.left = Left.gameObject;
                ControllerManager.right = Right.gameObject;
            }
            steamCam.origin.gameObject.SetActive(true);
        }

        protected virtual void InitializeTools(Controller controller, bool isLeft)
        {
            // Combine
            var toolTypes = Tools.Concat(isLeft ? LeftTools : RightTools).Distinct();

            foreach(var type in toolTypes)
            {
                controller.AddTool(type);
            }
        }

        protected virtual Controller CreateLeftController()
        {
            return LeftController.Create();
        }

        protected virtual Controller CreateRightController()
        {
            return RightController.Create();
        }

        public virtual IList<Type> Tools
        {
            get { return new List<Type>(); }
        }

        public virtual IList<Type> LeftTools
        {
            get { return new List<Type>(); }
        }

        public virtual IList<Type> RightTools
        {
            get { return new List<Type>(); }
        }
    }
}
