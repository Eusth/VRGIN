/************************************************************************************
Filename    :   OSPManager.cs
Content     :   Interface into the Oculus Spatializer Plugin
Created     :   Novemnber 4, 2014
Authors     :   Peter Giokaris
Copyright   :   Copyright 2014 Oculus VR, Inc. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.1 (the "License"); 
you may not use the Oculus VR Rift SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.1 

Unless required by applicable law or agreed to in writing, the Oculus VR SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/

// Add Minor releases as they come on-line
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2
#define UNITY5
#elif UNITY_6_0
#error support this!
#endif

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

//-------------------------------------------------------------------------------------
// ***** OSPManager
//
/// <summary>
/// OSPManager interfaces into the Oculus Spatializer. This component should be added
/// into the scene once. 
///
/// </summary>
public class OSPManager : MonoBehaviour 
{
    public const string strOSP = "OculusSpatializerPlugin";

	// * * * * * * * * * * * * *
	// RoomModel - Used to enable and define simple box room for early reflections
	[StructLayout(LayoutKind.Sequential)]
    public struct RoomModel
    {
        public bool  Enable;
        public int   ReflectionOrder;
		public float DimensionX;
		public float DimensionY;
		public float DimensionZ;
		public float Reflection_K0;
		public float Reflection_K1;
		public float Reflection_K2;
		public float Reflection_K3;
		public float Reflection_K4;
		public float Reflection_K5;
		public bool  ReverbOn;
	}

	// * * * * * * * * * * * * *
    // Import functions
	[DllImport(strOSP)]
	private static extern bool OSP_Init(int sample_rate, int buffer_size);
	[DllImport(strOSP)]
    private static extern bool OSP_Exit();
	[DllImport(strOSP)]
    private static extern bool OSP_UpdateRoomModel(ref RoomModel rm);
	[DllImport(strOSP)]
	private static extern void OSP_SetReflectionsRangeMax(float range);
	[DllImport(strOSP)]
	private static extern int  OSP_AcquireContext(int audioSourceInstanceID);
	[DllImport(strOSP)]
	private static extern void OSP_ReturnContext(int audioSourceInstanceID, int context);
	[DllImport(strOSP)]
	private static extern bool OSP_WasSoundStolen(int audioSourceInstanceID);
	[DllImport(strOSP)]
	private static extern bool OSP_GetBypass();
	[DllImport(strOSP)]
	private static extern void OSP_SetBypass(bool bypass);
	[DllImport(strOSP)]
	private static extern void OSP_SetGlobalScale(float globalScale);
	[DllImport(strOSP)]
	private static extern bool OSP_GetUseInverseSquareAttenuation();
	[DllImport(strOSP)]
	private static extern void OSP_SetUseInverseSquareAttenuation(bool useInvSq);
	[DllImport(strOSP)]
	private static extern void OSP_SetFalloffRangeGlobal(float nearRange, float farRange);
	[DllImport(strOSP)]
	private static extern void OSP_SetFalloffRangeLocal(int contextAndSound, float nearRange, float farRange);
	[DllImport(strOSP)]
	private static extern void OSP_SetGain(float gain);
	[DllImport(strOSP)]
	private static extern void OSP_SetDisableReflectionsOnSound(int contextAndSound, bool disable); 
	[DllImport(strOSP)]
	private static extern float OSP_GetDrainTime(int context);
	[DllImport(strOSP)]
	private static extern void OSP_SetPositonRelXYZ(int context, float x, float y, float z);
	[DllImport(strOSP)]
	private static extern void OSP_Spatialize(int context, float[] ioBuf, bool useInvSq, float near, float far);
	[DllImport(strOSP)]
	private static extern int OSP_GetMaxNumSpatializedSounds();

	// * * * * * * * * * * * * *
	// Public members
	private int bufferSize = 512; // Do not expose at this time
	public  int BufferSize
	{
		get{return bufferSize; }
		set{bufferSize = value;}
	}

	private int sampleRate = 48000; // Do not expose at this time
	public  int SampleRate
	{
		get{return sampleRate; }
		set{sampleRate = value;}
	}

	[SerializeField]
	private bool bypass = false;
	public  bool Bypass
	{
		get{return OSP_GetBypass(); }
		set{bypass = value; 
			OSP_SetBypass(bypass);}
	}
	
	[SerializeField]
	private float globalScale = 1.0f;
	public  float GlobalScale
	{
		get{return globalScale; }
		set
		{
			globalScale = Mathf.Clamp (value, 0.00001f, 10000.0f); 
			OSP_SetGlobalScale(globalScale);
		}
	}
	
	[SerializeField]
	private float gain = 0.0f;
	public  float Gain
	{
		get{return gain; }
		set
		{
			gain = Mathf.Clamp(value, -24.0f, 24.0f); 
			OSP_SetGain(gain);
		}
	}

	[SerializeField]
	private bool useInverseSquare = false;
	public  bool UseInverseSquare
	{
		get{return useInverseSquare;}
		set
		{
			useInverseSquare = value; 
			OSP_SetUseInverseSquareAttenuation(useInverseSquare);
		}
	}

	[SerializeField]
	private float falloffNear = 10.0f;
	public  float FalloffNear
	{
		get{return falloffNear; }
		set
		{
			falloffNear = Mathf.Clamp(value, 0.0f, 1000000.0f); 
			OSP_SetFalloffRangeGlobal(falloffNear, falloffFar);
		}
	}
	
	[SerializeField]
	private float falloffFar = 1000.0f;
	public  float FalloffFar
	{
		get{return falloffFar; }
		set
		{
			falloffFar = Mathf.Clamp(value, 0.0f, 1000000.0f); 
			OSP_SetFalloffRangeGlobal(falloffNear, falloffFar);
		}
	}
		
	// Access the values without calling through properties (and wrecking local state)
	public void GetNearFarFalloffValues (ref float n, ref float f)
	{
		n = falloffNear;
		f = falloffFar;
	}

	//----------------------
	// Reflection parameters
	private bool dirtyReflection;

	[SerializeField]
	private bool enableReflections = false;
	public bool  EnableReflections
	{
		get{return enableReflections; }
		set{enableReflections = value; dirtyReflection = true;}
	}

	[SerializeField]
	private bool enableReverb = false;
	public bool  EnableReverb
	{
		get{return enableReverb; }
		set{enableReverb = value; dirtyReflection = true;}
	}
	
	[SerializeField]
	private Vector3 dimensions = new Vector3 (0.0f, 0.0f, 0.0f);
	public Vector3 Dimensions
	{
		get{return dimensions; }
		set{dimensions = value; 
			dimensions.x = Mathf.Clamp (dimensions.x, 1.0f, 200.0f);
			dimensions.y = Mathf.Clamp (dimensions.y, 1.0f, 200.0f);
			dimensions.z = Mathf.Clamp (dimensions.z, 1.0f, 200.0f);
			dirtyReflection = true;}
	}

	[SerializeField]
	private Vector2 rK01 = new Vector2(0.0f, 0.0f);
	public Vector2 RK01
	{
		get{return rK01; }
		set{rK01 = value; 
			rK01.x = Mathf.Clamp (rK01.x, 0.0f, 0.97f);
			rK01.y = Mathf.Clamp (rK01.y, 0.0f, 0.97f);
			dirtyReflection = true;}
	}

	[SerializeField]
	private Vector2 rK23 = new Vector2(0.0f, 0.0f);
	public Vector2 RK23
	{
		get{return rK23; }
		set{rK23 = value; 
			rK23.x = Mathf.Clamp (rK23.x, 0.0f, 0.95f);
			rK23.y = Mathf.Clamp (rK23.y, 0.0f, 0.95f);
			dirtyReflection = true;}
	}

	[SerializeField]
	private Vector2 rK45 = new Vector2(0.0f, 0.0f);
	public Vector2 RK45
	{
		get{return rK45; }
		set{rK45 = value; 
			rK45.x = Mathf.Clamp (rK45.x, 0.0f, 0.95f);
			rK45.y = Mathf.Clamp (rK45.y, 0.0f, 0.95f);
			dirtyReflection = true;}
	}

	// * * * * * * * * * * * * *
	// Public members 

	// * * * * * * * * * * * * *
	// Private members

	// * * * * * * * * * * * * *
    // Static members

	// Our instance to allow this script to be called without a direct connection.
	private static bool sOSPInit = false;

	// Some functions in OSPAudioSource require probing into OSPManager, so they can 
	// interface through this static member.
	public static OSPManager sInstance = null;

	// * * * * * * * * * * * * *
	// MonoBehaviour overrides

	/// <summary>
	/// Awake this instance.
	/// </summary>
	void Awake () 
	{	
		// We can only have one instance of OSPManager in a scene (use this for local property query)
		if(sInstance == null)
		{
			sInstance = this;
		}
		else
		{
			Debug.LogWarning (System.String.Format ("OSPManager-Awake: Only one instance of OSPManager can exist in the scene."));
			return;
		}

		int samplerate;
		int bufsize;
		int numbuf;

#if (!UNITY5)
		// Used to override samplerate and buffer size with optimal values
		bool setvalues = true;

		// OSX is picky with samplerate and buffer sizes, so leave it alone
#if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
		setvalues = false;
#endif
#endif
		// Get the current sample rate
		samplerate = AudioSettings.outputSampleRate;
		// Get the current buffer size and number of buffers
		AudioSettings.GetDSPBufferSize (out bufsize, out numbuf);

		Debug.LogWarning (System.String.Format ("OSP: Queried SampleRate: {0:F0} BufferSize: {1:F0}", samplerate, bufsize));

		// We only know if OpenSL has been enabled if sample rate is 48K. 
		// We need to check another setting. 
#if (UNITY_ANDROID && !UNITY_EDITOR)
		if((samplerate == 48000))
		{
			Debug.LogWarning("OSP: Android OpenSL ENABLED (based on 48KHz sample-rate)");
#if (!UNITY5)
			setvalues = false;
#endif
		}
		else
		{
			Debug.LogWarning("OSP: Android OpenSL DISABLED");
		}
#endif

// We will only set values IF we are not Unity 5 (the ability to set DSP settings does not exist)
// NOTE: Unity 4 does not switch DSP buffer sizes using ProjectSettings->Audio, but Unity 5 does.
// At some point along Unity 5 maturity, the functionality to set DSP values directly might be removed.
#if (!UNITY5)
		if(setvalues == true)
		{
// NOTE: When setting DSP values in Unity 4, there may be a situation where using PlayOnAwake on 
// non-spatitalized audio objects will fail to play.
// Uncomment this code for achieving the best possibly latency with spatialized audio, but
// USE AT YOUR OWN RISK!
/*
			// Set the ideal sample rate
			AudioSettings.outputSampleRate = SampleRate;
			// Get the sample rate again (it may not take, depending on platform)
			samplerate = AudioSettings.outputSampleRate;
			// Set the ideal buffer size
			AudioSettings.SetDSPBufferSize (BufferSize, numbuf);
			// Get the current buffer size and number of buffers again
			AudioSettings.GetDSPBufferSize (out bufsize, out numbuf);
*/
	}
#endif

		Debug.LogWarning (System.String.Format ("OSP: sample rate: {0:F0}", samplerate));
		Debug.LogWarning (System.String.Format ("OSP: buffer size: {0:F0}", bufsize));
		Debug.LogWarning (System.String.Format ("OSP: num buffers: {0:F0}", numbuf));

		sOSPInit = OSP_Init(samplerate, bufsize);

		// Set global variables not set to dirty updates
		OSP_SetBypass             (bypass);
		OSP_SetGlobalScale        (globalScale);
		OSP_SetGain               (gain);
		OSP_SetFalloffRangeGlobal (falloffNear, falloffFar);

		// Update reflections for the first time
		dirtyReflection = true;
	}
   
	/// <summary>
	/// Start this instance.
	/// Note: make sure to always have a Start function for classes that have editor scripts.
	/// </summary>
	void Start()
	{
	}
	
	/// <summary>
	/// Run processes that need to be updated in our game thread
	/// </summary>
	void Update()
	{
		// Update reflections
		if (dirtyReflection == true) 
		{
			UpdateEarlyReflections();
			dirtyReflection = false;
		}
	}
		
	/// <summary>
	/// Raises the destroy event.
	/// </summary>
	void OnDestroy()
	{
		sOSPInit = false;
        // PGG Do not call, faster (but stuck with initial buffer resolution)
		//OSP_Exit();
	}
		
	// * * * * * * * * * * * * *
	// Public Functions
	
	/// <summary>
	/// Inited - Check to see if system has been initialized
	/// </summary>
	/// <returns><c>true</c> if is initialized; otherwise, <c>false</c>.</returns>
	public static bool IsInitialized()
	{
		return sOSPInit;
	}

	/// <summary>
	/// Gets a spatializer context for a sound.
	/// </summary>
	/// <param name="audioSourceInstanceID">Instance ID of caller</param>
	/// <returns>The context.</returns>
	public static int AcquireContext (int audioSourceInstanceID)
	{
		return OSP_AcquireContext(audioSourceInstanceID);
	}

	/// <summary>
	/// Releases the context for a sound.
	/// </summary>
	/// <param name="audioSourceInstanceID">Instance ID of caller</param>
	/// <param name="context">Context.</param>
	public static void ReleaseContext(int audioSourceInstanceID, int context)
	{
		// Drop back into OSP
		OSP_ReturnContext (audioSourceInstanceID, context);
	}

	/// <summary>
	/// Gets the bypass. Used by OSPAudioSource (cannot be written to; used for
	/// getting global bypass state).
	/// </summary>
	/// <returns><c>true</c>, if bypass was gotten, <c>false</c> otherwise.</returns>
	public static bool GetBypass()
	{
		return OSP_GetBypass ();
	}

	/// <summary>
	/// Sets a value indicating whether this <see cref="OSPManager"/> get use inverse square attenuation.
	/// </summary>
	/// <value><c>true</c> if get use inverse square attenuation; otherwise, <c>false</c>.</value>
	public static bool GetUseInverseSquareAttenuation()
	{
		return OSP_GetUseInverseSquareAttenuation();
	}

	/// <summary>
	/// Sets the disable reflections on sound.
	/// </summary>
	/// <param name="context">Context.</param>
	/// <param name="enable">If set to <c>true</c> enable.</param>
	public static void SetDisableReflectionsOnSound(int context, bool disable)
	{
		OSP_SetDisableReflectionsOnSound(context, disable);
	}

	/// <summary>
	/// Gets the drain time, based on reflection room size.
	/// </summary>
	/// <returns>The drain time.</returns>
	public static float GetDrainTime(int context)
	{
		return OSP_GetDrainTime (context);
	}

	/// <summary>
	/// Sets the position of the sound relative to the listener.
	/// </summary>
	/// <param name="context">Context.</param>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="z">The z coordinate.</param>
	public static void SetPositionRel(int context, float x, float y, float z)
	{
		if (sOSPInit == false) return;

		OSP_SetPositonRelXYZ (context, x, y, z);
	}

	/// <summary>
	/// Spatialize the specified ioBuf using context.
	/// </summary>
	/// <param name="ioBuf">Io buffer.</param>
	/// <param name="context">Context.</param>
	public static void Spatialize (int context, float[] ioBuf, bool useInvSq, float near, float far)
	{	
		if (sOSPInit == false) return;

		OSP_Spatialize (context, ioBuf, useInvSq, near, far);
	}

	/// <summary>
	/// Sets the falloff range local.
	/// </summary>
	/// <param name="contextAndSound">Context and sound.</param>
	/// <param name="nearRange">Near range.</param>
	/// <param name="farRange">Far range.</param>
	public static void SetFalloffRangeLocal(int contextAndSound, float nearRange, float farRange)
	{
		OSP_SetFalloffRangeLocal(contextAndSound, nearRange, farRange);
	}

	/// <summary>
	/// Get max number of Main Context sounds (used for debug logging)
	/// </summary>
	public static int GetMaxNumSpatializedSources()
	{
		return OSP_GetMaxNumSpatializedSounds();
	}
	// * * * * * * * * * * * * *
	// Private Functions

	/// <summary>
	/// Updates the early reflections.
	/// </summary>
	void UpdateEarlyReflections()
	{
		RoomModel rm;
		rm.Enable = enableReflections;
		rm.ReverbOn = enableReverb;
		rm.ReflectionOrder = 0; // Unused
		rm.DimensionX = dimensions.x;
		rm.DimensionY = dimensions.y;
		rm.DimensionZ = dimensions.z;
		rm.Reflection_K0 = rK01.x;
		rm.Reflection_K1 = rK01.y;
		rm.Reflection_K2 = rK23.x;
		rm.Reflection_K3 = rK23.y;
		rm.Reflection_K4 = rK45.x;
		rm.Reflection_K5 = rK45.y;

		OSP_UpdateRoomModel (ref rm);
	}
}
