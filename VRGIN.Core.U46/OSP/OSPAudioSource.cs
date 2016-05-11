/************************************************************************************
Filename    :   OSPAudioSource.cs
Content     :   Interface into the Oculus Spatializer Plugin (from audio source)
Created     :   Novemnber 11, 2014
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

//#define DEBUG_AudioSource

using System;
using UnityEngine;

/// <summary>
/// OSP audio source.
/// This component should be added to a scene with an audio source
/// </summary>
public class OSPAudioSource : MonoBehaviour 
{
	// Public members
	public AudioSource audioSource = null;

	[SerializeField]
	private bool bypass = false;
	public  bool Bypass
	{
		get{return bypass;}
		set{bypass = value;}
	}

	[SerializeField]
	private bool playOnAwake = false;
	public  bool PlayOnAwake
	{
		get{return playOnAwake;}
		set{playOnAwake = value;}
	}

	[SerializeField]
	private bool disableReflections = false;
	public  bool DisableReflections
	{
		get{return disableReflections; }
		set{disableReflections = value;}
	}

	[SerializeField]
	private bool useInverseSquare = false;
	public  bool UseInverseSquare
	{
		get{return useInverseSquare;}
		set
		{
			useInverseSquare = value;
			UpdateLocalInvSq();
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
			UpdateLocalInvSq();
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
			UpdateLocalInvSq();
		}
	}



	// Private members
	AudioListener audioListener = null;

	private int   context   = sNoContext;
	private bool  isPlaying = false;
	private float panLevel  = 0.0f;
	private float spread    = 0.0f;
	// We must account for the early reflection tail, don't but the 
	// context back until it's been rendered
	private bool  drain     = false;
	private float drainTime = 0.0f;
	// We will set the relative position in the Update function
	// Capture the previous so that we can interpolate into the
	// latest position in AudioFilterRead
	private Vector3 relPos      = new Vector3(0,0,0);
	private Vector3 relVel      = new Vector3(0,0,0);
	private float   relPosTime  = 0.0f;

	// Consts
	private const int   sNoContext        = -1;
	private const float sSetPanLevel      = 1.0f;
	private const float sSetPanLevelInvSq = 0.0f;
	private const float sSetSpread        = 180.0f;

#if DEBUG_AudioSource
	// Debug
	private const bool  debugOn		 = false; // TURN THIS OFF FOR NO VERBOSE
	private float dTimeMainLoop      = 0.0f;
	private int   audioFrames		 = 100;
	private float PrevAudioTime 	 = 0.0f;
#endif

	//* * * * * * * * * * * * *
	// MonoBehaviour functions
	//* * * * * * * * * * * * *

	/// <summary>
	/// Awake this instance.
	/// </summary>
	void Awake()
	{
		// First thing to do, cache the unity audio source (can be managed by the
		// user if audio source can change)
		if (!audioSource) audioSource = GetComponent<AudioSource>();
		if (!audioSource) return;

		// Set this in Start; we need to ensure spatializer has been initialized
		// It's MUCH better to set playOnAwake in the audio source script, will avoid
		// having the sound play then stop and then play again)
		if (audioSource.playOnAwake == true || playOnAwake == true)
		{
			audioSource.Stop();
		}
	}

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start() 
	{		
		if(!audioSource)
		{
			Debug.LogWarning ("Start - Warning: No assigned AudioSource");
			return;
		}

		// Start a play on awake sound here, after spatializer has been initialized
		// Only do this IF it didn't happen in Awake
		if (((audioSource.playOnAwake == true) || playOnAwake == true) && 
		     (isPlaying == false))
			Play();
	}
	
	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update() 
	{
		if(!audioSource)return;

		// If user called Play on the acutal AudioSource, account for this
		// NOTE: There may be a potential issue with calling AudioSource.Stop on a 
		// looping sound, since the Update may not catch this. We might need to catch
		// this edge case at some point; a one-shot sound will stop automatically and
		// return spatialization resources.
		if((isPlaying == false) && (audioSource.isPlaying))
		{
			// This is a copy of OSPAudioSource Play function, minus 
			// calling Play on audio source since it's already been called

			// Bail if manager has not even started
			if (OSPManager.IsInitialized () == false) 
				return;
			
			// We will grab a context at this point, and set the right values
			// to allow for spatialization to take effect
			Acquire();
			
			// We will set the relative position of this sound now before we start playing
			SetRelativeSoundPos(true);
			
			// We are ready to play the sound
			// Commented out, kept for readability 
			// audioSource.Play();
			
			lock(this) isPlaying = true;

			drain = false;
		}
	
		// If sound is playing on it's own and dies off, we need to
		// Reset
		if (isPlaying == true) 
		{
			// If we stopped the sound using AudioSource, Release resources
			if(audioSource.isPlaying == false)
			{
				lock (this) isPlaying = false;
				Release();
				return;
			}

			// We will go back and forth between spatial processing
			// and native 2D panning
			if((Bypass == true) || (OSPManager.GetBypass() == true))
			{
#if (UNITY5)
				audioSource.spatialBlend = panLevel;
#endif
				audioSource.spread   = spread;
			}
			else
			{
				// We want to remove FMod curve attenuation if we are doing our
				// own internal calc
				float pl = sSetPanLevel;
				if (OSPManager.GetUseInverseSquareAttenuation() == true || useInverseSquare == true)
					pl = sSetPanLevelInvSq;
#if (!UNITY5)
#else
				audioSource.spatialBlend = pl;
#endif
				audioSource.spread   = sSetSpread;
			}

			// Update local reflection enable/disable
			if(context != sNoContext)
			{
				OSPManager.SetDisableReflectionsOnSound(context, disableReflections);
			}

			// Hack - this is mic-input, so allow for processing to occur, but there is no
			// time cutoff here, caller manages starting and stopping sound implicitly
			if((audioSource.time == 0) && (audioSource.loop == true))
			{
				SetRelativeSoundPos(false);
			}
			else
			// return the context when the sound has finished playing
			if((audioSource.time >= audioSource.clip.length) && (audioSource.loop == false))
			{
				// One last pass to allow for the tail portion of the sound
				// to finish
				drainTime = OSPManager.GetDrainTime(context);
				drain     = true;
			}
			else
			{	
				// We must set all positional properties here, not in  
				// OnAudioFilterRead. We might consider a better approach
				// to interpolate the current location for better localization,
				// should the resolution of setting it here sound jittery due
				// to thread frequency mismatch.
				SetRelativeSoundPos(false);

#if DEBUG_AudioSource
				// Time main thread and audio thread
				if(debugOn)
				{
					// Get audio frequency
					if(audioFrames == 0)
					{
						float ct = 1.0f / (GetTime(false) - dTimeMainLoop);
						Debug.LogWarning (System.String.Format ("U: {0:F0}", ct));
						ct = 100.0f / (GetTime(true) - PrevAudioTime);
						Debug.LogWarning (System.String.Format ("A: {0:F0}", ct));
						audioFrames = 100;
						PrevAudioTime = (float)GetTime(true);
					}

					dTimeMainLoop = (float)GetTime(false);
				}
#endif
			}

			if(drain == true)
			{
				// Keep playing until we safely drain out the early reflections
				drainTime -= Time.deltaTime;
				if(drainTime < 0.0f)
				{
					drain = false;

					lock (this) isPlaying = false;
					// Stop will both stop audio from playing (keeping OnAudioFilterRead from 
					// running with a held audio source) as well as release the spatialization
					// resources via Release()
					Stop(); 
				}
			}
		}
	}

	/// <summary>
	/// Raises the enable event.
	/// </summary>
	void OnEnable() 
	{		
		// MonoBehaviour function, contains all calls to 
		// start a playing sound
		Start();
	}

	/// <summary>
	/// Raises the disable event.
	/// </summary>
	void OnDisable()
	{
		// Public OSPAudioSource call
		Stop();
	}

	/// <summary>
	/// Raises the destroy event.
	/// </summary>
	void OnDestroy()
	{
		if(!audioSource)return;

		lock (this) isPlaying = false;
		Release();
	}
		
	//* * * * * * * * * * * * *
	// Private functions
	//* * * * * * * * * * * * *

	/// <summary>
	/// Acquire resources for spatialization
	/// </summary>
	private void Acquire()
	{
		if(!audioSource)return;

		// Cache pan and spread
#if (!UNITY5)
#else
		panLevel = audioSource.spatialBlend;
#endif
		spread = audioSource.spread;
	
		// Reserve a context in OSP that will be used for spatializing sound
		// (Don't grab a new one if is already has a context attached to it)
		if(context == sNoContext)
			context = OSPManager.AcquireContext(GetInstanceID());

		// We don't have a context here, so bail
		if(context == sNoContext)
			return;

		// Set pan to full (spread at 180 will keep attenuation curve working, but all 2D
		// panning will be removed)

		// We want to remove FMod curve attenuation if we are doing our
		// own internal calc
		float pl = sSetPanLevel;
		if (OSPManager.GetUseInverseSquareAttenuation() == true)
			pl = sSetPanLevelInvSq;
		
		#if (!UNITY5)

#else
		audioSource.spatialBlend = pl;
#endif

		// set spread to 180
		audioSource.spread = sSetSpread;
	}

	/// <summary>
	/// Reset cached variables and free the sound context back to OSPManger
	/// </summary>
	private void Release()
	{
		if(!audioSource)return;

		// Reset all audio variables that were changed during play
#if (!UNITY5)

#else
		audioSource.spatialBlend = panLevel;
#endif
		audioSource.spread   = spread;

		// Put context back into pool
		if(context != sNoContext)
		{
			OSPManager.ReleaseContext (GetInstanceID(), context);
			context = sNoContext;
		}
	}

	/// <summary>
	/// Set the position of the sound relative to the listener
	/// </summary>
	/// <param name="firstTime">If set to <c>true</c> first time.</param>
	private void SetRelativeSoundPos(bool firstTime)
	{
		// Find the audio listener (first time used)
		if(audioListener == null)
		{
			audioListener = FindObjectOfType<AudioListener>();

			// If still null, then we have a problem;
			if(audioListener == null)
			{
				Debug.LogWarning ("SetRelativeSoundPos - Warning: No AudioListener present");
				return;
			}
		}

		// Get the location of this sound
		Vector3 sp    = transform.position;
		// Get the main camera in the scene
		Vector3    cp = audioListener.transform.position;
		Quaternion cq = audioListener.transform.rotation;
		// transform the vector relative to the camera
		Quaternion cqi = Quaternion.Inverse (cq);

		// Get the vector between the sound and camera
		lock (this) 
		{
			if(firstTime)
			{
				relPos = cqi * (sp - cp);
				relVel.x = relVel.y = relVel.z = 0.0f;
				relPosTime = GetTime(true);
			}
			else
			{
				Vector3 prevRelPos     = relPos;
				float   prevRelPosTime = relPosTime;
				relPos = cqi * (sp - cp);
				// Reverse z (Unity is left-handed co-ordinates)
				relPos.z = -relPos.z;
				relPosTime = GetTime(true);
				// Calculate velocity
				relVel = relPos - prevRelPos;
				float dTime = relPosTime - prevRelPosTime;
				relVel *= dTime;
			}
		}
	}


	//* * * * * * * * * * * * *
	// Public functions
	//* * * * * * * * * * * * *

	/// <summary>
	/// Play this sound.
	/// </summary>
	public void Play()
	{
		if(!audioSource)
		{
			Console.WriteLine ("Play - Warning: No AudioSource assigned");
			return;
		}
        Console.WriteLine("Playing");
        // Bail if manager has not even started
        if (OSPManager.IsInitialized () == false) 
			return;

		// We will grab a context at this point, and set the right values
		// to allow for spatialization to take effect
		Acquire();

		// We will set the relative position of this sound now before we start playing
		SetRelativeSoundPos(true);

		// We are ready to play the sound
		audioSource.Play();

		lock(this) isPlaying = true;
	}

	/// <summary>
	/// Plays the sound with a delay (in sec.)
	/// </summary>
	/// <param name="delay">Delay.</param>
	public void PlayDelayed(float delay)
	{
		if(!audioSource)
		{
			Debug.LogWarning ("PlayDelayed - Warning: No AudioSource assigned");
			return;
		}
		
		// Bail if manager has not even started
		if (OSPManager.IsInitialized () == false) 
			return;
		
		// We will grab a context at this point, and set the right values
		// to allow for spatialization to take effect
		Acquire();
		
		// We will set the relative position of this sound now before we start playing
		SetRelativeSoundPos(true);
		
		// We are ready to play the sound
		audioSource.PlayDelayed(delay);
		
		lock(this) isPlaying = true;
	}

	/// <summary>
	/// Plays the time scheduled relative to current AudioSettings.dspTime.
	/// </summary>
	/// <param name="time">Time.</param>
	public void PlayScheduled(double time)
	{
		if(!audioSource)
		{
			Debug.LogWarning ("PlayScheduled - Warning: No AudioSource assigned");
			return;
		}
		
		// Bail if manager has not even started
		if (OSPManager.IsInitialized () == false) 
			return;
		
		// We will grab a context at this point, and set the right values
		// to allow for spatialization to take effect
		Acquire();
		
		// We will set the relative position of this sound now before we start playing
		SetRelativeSoundPos(true);
		
		// We are ready to play the sound
		audioSource.PlayScheduled(time);
		
		lock(this) isPlaying = true;
	}

	/// <summary>
	/// Stop this instance.
	/// </summary>
	public void Stop()
	{
		if(!audioSource)
		{
			Debug.LogWarning ("Stop - Warning: No AudioSource assigned");
			return;
		}

		lock(this) isPlaying = false;

		// Stop audio from playing, and reset any cached values that we
		// have set from Play
		audioSource.Stop();

		// Return spatializer context
		Release();
	}

	/// <summary>
	/// Pause this instance.
	/// </summary>
	public void Pause()
	{
		if(!audioSource) 
		{
			Debug.LogWarning ("Pause - Warning: No AudioSource assigned");
			return;
		}

		audioSource.Pause();
	}

	/// <summary>
	/// UnPause this instance.
	/// </summary>
	public void UnPause()
	{
		if(!audioSource) 
		{
			Debug.LogWarning ("UnPause - Warning: No AudioSource assigned");
			return;
		}
#if (UNITY5)
		audioSource.UnPause();
#endif
	}

	/// <summary>
	/// Returns true if this source is currently playing.
	/// </summary>
	public bool IsPlaying()
	{
		return isPlaying;
	}

	/// <summary>
	/// Returns true if this source is playing and has a valid context.
	/// </summary>
	public bool IsSpatialized()
	{
		if(isPlaying)
		{
			return context != sNoContext;
		}

		return false;
	}

	//* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
	// OnAudioFilterRead (separate thread)
	//* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *

	/// <summary>
	/// Raises the audio filter read event.
	/// We will spatialize the audio here
	/// NOTE: It's important that we get the audio attenuation curve for volume,
	/// that way we can feed it into our spatializer and have things sound match
	/// with bypass on/off
	/// </summary>
	/// <param name="data">Data.</param>
	/// <param name="channels">Channels.</param>
	///
	void OnAudioFilterRead(float[] data, int channels)
	{
#if DEBUG_AudioSource
		if(debugOn)
			if(audioFrames > 0)
				audioFrames--;
#endif
		// Problem: We cannot read timer here.
		// However, we can read time-stamp via plugin
		// This is required to smooth out the position,
		// since the main loop is only allowed to update position
		// of sound, but is running at a different frequency then
		// the audio loop.
		// 
		// Therefore, we will need to dead reckon the position of the sound.

		// Do not spatialize if we are not playing
		if ( (isPlaying == false) || 
		     (Bypass == true) ||
		     (OSPManager.GetBypass () == true) || 
		     (OSPManager.IsInitialized() == false) )
			return;
	

		float dTime = GetTime(true) - relPosTime;
		lock(this)
		{
			relPos += relVel * dTime;
			relPosTime = GetTime(true);
		}

		// TODO: Need to ensure that sound is not played before context is
		// legal
		if(context != sNoContext)
		{
			// Set the position for this context and sound
			OSPManager.SetPositionRel (context, relPos.x, relPos.y, relPos.z);
			//Spatialize (local override of InvSq is passed in)
			OSPManager.Spatialize (context, data, useInverseSquare, falloffNear, falloffFar);
		}
	}

	/// <summary>
	/// Gets the time.
	/// We can bounce between Time and AudioSettings.dspTime
	/// </summary>
	/// <returns>The time.</returns>
	/// <param name="dspTime">If set to <c>true</c> dsp time.</param>
	float GetTime(bool dspTime)
	{
		if(dspTime == true)
			return (float)AudioSettings.dspTime;

		return Time.time;
	}	

	/// <summary>
	/// Updates the local inv sq.
	/// </summary>
	void UpdateLocalInvSq()
	{
		float near = 0.0f;
		float far  = 0.0f;
		
		// We will set the current sound to the local inverse square value
		if(useInverseSquare == true)
		{
			near = falloffNear;
			far  = falloffFar;
		}
		else 
		{
			if(OSPManager.sInstance != null)
			{
				OSPManager.sInstance.GetNearFarFalloffValues(ref near, ref far);
			}
		}

		OSPManager.SetFalloffRangeLocal(context, near, far);
	}	
}
