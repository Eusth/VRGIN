//========= Copyright 2014, Valve Corporation, All rights reserved. ===========
//
// Purpose: Adds SteamVR render support to existing camera objects
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Reflection;
using Valve.VR;
using System;
using VRGIN.Core;
using VRGIN.Helpers;

[RequireComponent(typeof(Camera))]
public class SteamVR_Camera : ProtectedBehaviour
{
	[SerializeField]
	private Transform _head;
	public Transform head { get { return _head; } }
	public Transform offset { get { return _head; } } // legacy
	public Transform origin { get { return _head.parent; } }

	[SerializeField]
	private Transform _ears;
	public Transform ears { get { return _ears; } }

	public Ray GetRay()
	{
		return new Ray(_head.position, _head.forward);
	}

	public bool wireframe = false;

	[SerializeField]
	private SteamVR_CameraFlip flip;

	#region Materials

	static public Material blitMaterial;

	// Using a single shared offscreen buffer to render the scene.  This needs to be larger
	// than the backbuffer to account for distortion correction.  The default resolution
	// gives us 1:1 sized pixels in the center of view, but quality can be adjusted up or
	// down using the following scale value to balance performance.
	static public float sceneResolutionScale = 1.0f;
	static private RenderTexture _sceneTexture;
	static public RenderTexture GetSceneTexture(bool hdr)
	{
		var vr = SteamVR.instance;
		if (vr == null)
			return null;

		int w = (int)(vr.sceneWidth * sceneResolutionScale);
		int h = (int)(vr.sceneHeight * sceneResolutionScale);
		int aa = QualitySettings.antiAliasing == 0 ? 1 : QualitySettings.antiAliasing;
		var format = hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

		if (_sceneTexture != null)
		{
			if (_sceneTexture.width != w || _sceneTexture.height != h || _sceneTexture.antiAliasing != aa || _sceneTexture.format != format)
			{
				Console.WriteLine(string.Format("Recreating scene texture.. Old: {0}x{1} MSAA={2} [{3}] New: {4}x{5} MSAA={6} [{7}]",
					_sceneTexture.width, _sceneTexture.height, _sceneTexture.antiAliasing, _sceneTexture.format, w, h, aa, format));
                UnityEngine.Object.Destroy(_sceneTexture);
				_sceneTexture = null;
			}
		}

		if (_sceneTexture == null)
		{
            System.Console.WriteLine("creating render texture...");
			_sceneTexture = new RenderTexture(w, h, 0, format);
			_sceneTexture.antiAliasing = aa;

#if (UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0 || UNITY_4_5)
			// OpenVR assumes floating point render targets are linear unless otherwise specified.
			var colorSpace = (hdr && QualitySettings.activeColorSpace == ColorSpace.Gamma) ? EColorSpace.Gamma : EColorSpace.Auto;
			SteamVR.Unity.SetColorSpace(colorSpace);
#endif
            _sceneTexture.Create();
		}

		return _sceneTexture;
	}

	#endregion

	#region Enable / Disable

	void OnDisable()
	{
		SteamVR_Render.Remove(this);
	}

	void OnEnable()
	{
        try
        {
#if !(UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0 || UNITY_4_5)
		// Convert camera rig for native OpenVR integration.
		var t = transform;
		if (head != t)
		{
			Expand();

			t.parent = origin;

			while (head.childCount > 0)
				head.GetChild(0).parent = t;
			DestroyImmediate(head.gameObject);
			_head = t;
		}

		if (flip != null)
		{
			DestroyImmediate(flip);
			flip = null;
		}

		if (!SteamVR.usingNativeSupport)
		{
			enabled = false;
			return;
		}
#else
            System.Console.WriteLine("OK");
            // Bail if no hmd is connected
            var vr = SteamVR.instance;
            if (vr == null)
            {
                if (head != null)
                {
                    head.GetComponent<SteamVR_GameView>().enabled = false;
                    head.GetComponent<SteamVR_TrackedObject>().enabled = false;
                }

                if (flip != null)
                    flip.enabled = false;

                enabled = false;
                return;
            }

            // Ensure rig is properly set up
            Expand();
            System.Console.WriteLine("OK2");

            if (blitMaterial == null)
            {
                System.Console.WriteLine("OK3");

                blitMaterial = new Material(UnityHelper.GetShader("SteamVR_Blit"));
                System.Console.WriteLine("OK4" + blitMaterial == null);

            }

            // Set remaining hmd specific settings
            var camera = GetComponent<Camera>();
            camera.fieldOfView = vr.fieldOfView;
            camera.aspect = vr.aspect;
            camera.eventMask = 0;           // disable mouse events
            camera.orthographic = false;    // force perspective
            camera.enabled = false;         // manually rendered by SteamVR_Render
            System.Console.WriteLine("OK5");

            if (camera.actualRenderingPath != RenderingPath.Forward && QualitySettings.antiAliasing > 1)
            {
                Console.WriteLine("MSAA only supported in Forward rendering path. (disabling MSAA)");
                QualitySettings.antiAliasing = 0;
            }

            // Ensure game view camera hdr setting matches
            var headCam = head.GetComponent<Camera>();
            if (headCam != null)
            {
                headCam.hdr = camera.hdr;
                headCam.renderingPath = camera.renderingPath;
            }
#endif
            System.Console.WriteLine("OK6");

            try
            {
                ears.GetComponent<SteamVR_Ears>().vrcam = this;
            }
            catch (System.Exception e)
            {
                Logger.Info("Ears not found");
            }
            System.Console.WriteLine("OK7");

            SteamVR_Render.Add(this);
            System.Console.WriteLine("OK8");
        } catch(Exception e2)
        {
            Console.WriteLine(e2);
        }
    }

    #endregion

    #region Functionality to ensure SteamVR_Camera component is always the last component on an object

    protected override void OnAwake() { ForceLast(); }

	static Hashtable values;

	public void ForceLast()
	{
        try
        {
            if (values != null)
            {
                // Restore values on new instance
                foreach (DictionaryEntry entry in values)
                {
                    var f = entry.Key as FieldInfo;
                    f.SetValue(this, entry.Value);
                }
                values = null;
            }
            else
            {
                // Make sure it's the last component
                var components = GetComponents<Component>();

                // But first make sure there aren't any other SteamVR_Cameras on this object.
                for (int i = 0; i < components.Length; i++)
                {
                    var c = components[i] as SteamVR_Camera;
                    if (c != null && c != this)
                    {
                        if (c.flip != null)
                            DestroyImmediate(c.flip);
                        DestroyImmediate(c);
                    }
                }

                components = GetComponents<Component>();

#if !(UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0 || UNITY_4_5)
			if (this != components[components.Length - 1])
			{
#else
                if (this != components[components.Length - 1] || flip == null)
                {
                    if (flip == null)
                        flip = gameObject.AddComponent<SteamVR_CameraFlip>();
#endif
                    // Store off values to be restored on new instance
                    values = new Hashtable();
                    var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var f in fields)
                        if (f.IsPublic || f.IsDefined(typeof(SerializeField), true))
                            values[f] = f.GetValue(this);

                    var go = gameObject;
                    DestroyImmediate(this);
                    go.AddComponent<SteamVR_Camera>().ForceLast();
                }
            }
        } catch(Exception e)
        {
            Console.WriteLine(e);
        }
	}

	#endregion

	#region Expand / Collapse object hierarchy

#if UNITY_EDITOR
	public bool isExpanded { get { return head != null && transform.parent == head; } }
#endif
	const string eyeSuffix = " (eye)";
	const string earsSuffix = " (ears)";
	const string headSuffix = " (head)";
	const string originSuffix = " (origin)";
	public string baseName { get { return name.EndsWith(eyeSuffix) ? name.Substring(0, name.Length - eyeSuffix.Length) : name; } }

	// Object hierarchy creation to make it easy to parent other objects appropriately,
	// otherwise this gets called on demand at runtime. Remaining initialization is
	// performed at startup, once the hmd has been identified.
	public void Expand()
	{
        try
        {
            var _origin = transform.parent;
            if (_origin == null)
            {
                _origin = new GameObject(name + originSuffix).transform;
                _origin.localPosition = transform.localPosition;
                _origin.localRotation = transform.localRotation;
                _origin.localScale = transform.localScale;
            }

            if (head == null)
            {
                _head = new GameObject(name + headSuffix, typeof(SteamVR_GameView), typeof(SteamVR_TrackedObject)).transform;
                head.parent = _origin;
                head.position = transform.position;
                head.rotation = transform.rotation;
                head.localScale = Vector3.one;
                head.tag = tag;

                var camera = head.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Nothing;
                camera.cullingMask = 0;
                camera.eventMask = 0;
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.nearClipPlane = 0;
                camera.farClipPlane = 1;
                camera.useOcclusionCulling = false;
            }

            if (transform.parent != head)
            {
                transform.parent = head;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                while (transform.childCount > 0)
                    transform.GetChild(0).parent = head;

                var guiLayer = GetComponent<GUILayer>();
                if (guiLayer != null)
                {
                    DestroyImmediate(guiLayer);
                    head.gameObject.AddComponent<GUILayer>();
                }

                var audioListener = GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    DestroyImmediate(audioListener);
                    _ears = new GameObject(name + earsSuffix, typeof(SteamVR_Ears)).transform;
                    ears.parent = _head;
                    ears.localPosition = Vector3.zero;
                    ears.localRotation = Quaternion.identity;
                    ears.localScale = Vector3.one;
                }
            }

            if (!name.EndsWith(eyeSuffix))
                name += eyeSuffix;
            
        } catch(Exception e) {
            Console.WriteLine(e);
        }
	}

	public void Collapse()
	{
        try
        {
            transform.parent = null;

            // Move children and components from head back to camera.
            while (head.childCount > 0)
                head.GetChild(0).parent = transform;

            var guiLayer = head.GetComponent<GUILayer>();
            if (guiLayer != null)
            {
                DestroyImmediate(guiLayer);
                gameObject.AddComponent<GUILayer>();
            }

            if (ears != null)
            {
                while (ears.childCount > 0)
                    ears.GetChild(0).parent = transform;

                DestroyImmediate(ears.gameObject);
                _ears = null;

                gameObject.AddComponent(typeof(AudioListener));
            }

            if (origin != null)
            {
                // If we created the origin originally, destroy it now.
                if (origin.name.EndsWith(originSuffix))
                {
                    // Reparent any children so we don't accidentally delete them.
                    var _origin = origin;
                    while (_origin.childCount > 0)
                        _origin.GetChild(0).parent = _origin.parent;

                    DestroyImmediate(_origin.gameObject);
                }
                else
                {
                    transform.parent = origin;
                }
            }

            DestroyImmediate(head.gameObject);
            _head = null;

            if (name.EndsWith(eyeSuffix))
                name = name.Substring(0, name.Length - eyeSuffix.Length);
        } catch(Exception e)
        {
            Console.WriteLine(e);
        }
	}

	#endregion

#if (UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0 || UNITY_4_5)

	#region Render callbacks

	void OnPreRender()
	{
        try
        {
            if (flip)
                flip.enabled = (SteamVR_Render.Top() == this && SteamVR.instance.graphicsAPI == EGraphicsAPIConvention.API_DirectX);

            var headCam = head.GetComponent<Camera>();
            if (headCam != null)
                headCam.enabled = (SteamVR_Render.Top() == this);

            if (wireframe)
                GL.wireframe = true;
        } catch( Exception e)
        {
            Console.WriteLine(e);
        }
	}

	void OnPostRender()
	{
        try
        {
            if (wireframe)
                GL.wireframe = false;
            
        } catch(Exception e) {
            Console.WriteLine(e);

        }
	}

	void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
        try
        {
            if (SteamVR_Render.Top() == this)
            {
                int eventID;
                if (SteamVR_Render.eye == EVREye.Eye_Left)
                {
                    // Get gpu started on work early to avoid bubbles at the top of the frame.
                    SteamVR_Utils.QueueEventOnRenderThread(SteamVR.Unity.k_nRenderEventID_Flush);

                    eventID = SteamVR.Unity.k_nRenderEventID_SubmitL;
                }
                else
                {
                    eventID = SteamVR.Unity.k_nRenderEventID_SubmitR;
                }
                // Queue up a call on the render thread to Submit our render target to the compositor.
                SteamVR_Utils.QueueEventOnRenderThread(eventID);
                //Texture_t t = new Texture_t() { eColorSpace = EColorSpace.Auto, eType = EGraphicsAPIConvention.API_DirectX, handle = _sceneTexture.GetNativeTexturePtr() };
                //VRTextureBounds_t b = SteamVR_Render.eye == EVREye.Eye_Left ? SteamVR.instance.textureBounds[0] : SteamVR.instance.textureBounds[1];
                //var res = OpenVR.Compositor.Submit(SteamVR_Render.eye, ref t, ref b, EVRSubmitFlags.Submit_Default);
                //Console.WriteLine(res);
            }
            Graphics.SetRenderTarget(dest);

            SteamVR_Camera.blitMaterial.mainTexture = src;

            GL.PushMatrix();
            GL.LoadOrtho();
            SteamVR_Camera.blitMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-1, 1, 0);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1, 1, 0);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1, -1, 0);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-1, -1, 0);
            GL.End();
            GL.PopMatrix();

            Graphics.SetRenderTarget(null);
        } catch(Exception e)
        {
            Console.WriteLine(e);
        }
	}

	#endregion

#endif
}

