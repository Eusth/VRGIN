/************************************************************************************
Filename    :   OSPReflectionZone.cs
Content     :   Add reflection zone volumes to set reflection parameters.
Created     :   August 10, 2015
Authors     :   Peter Giokaris
opyright   :   Copyright 2015 Oculus VR, Inc. All Rights reserved.

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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OSPReflectionZone : MonoBehaviour 
{
	[SerializeField]
	private Vector3 dimensions = new Vector3 (0.0f, 0.0f, 0.0f);
	public Vector3 Dimensions
	{
		get{return dimensions; }
		set{dimensions = value; 
			dimensions.x = Mathf.Clamp (dimensions.x, 1.0f, 200.0f);
			dimensions.y = Mathf.Clamp (dimensions.y, 1.0f, 200.0f);
			dimensions.z = Mathf.Clamp (dimensions.z, 1.0f, 200.0f);}
	}
	
	[SerializeField]
	private Vector2 rK01 = new Vector2(0.0f, 0.0f);
	public Vector2 RK01
	{
		get{return rK01; }
		set{rK01 = value; 
			rK01.x = Mathf.Clamp (rK01.x, 0.0f, 0.97f);
			rK01.y = Mathf.Clamp (rK01.y, 0.0f, 0.97f);}
	}
	
	[SerializeField]
	private Vector2 rK23 = new Vector2(0.0f, 0.0f);
	public Vector2 RK23
	{
		get{return rK23; }
		set{rK23 = value; 
			rK23.x = Mathf.Clamp (rK23.x, 0.0f, 0.95f);
			rK23.y = Mathf.Clamp (rK23.y, 0.0f, 0.95f);}
	}
	
	[SerializeField]
	private Vector2 rK45 = new Vector2(0.0f, 0.0f);
	public Vector2 RK45
	{
		get{return rK45; }
		set{rK45 = value; 
			rK45.x = Mathf.Clamp (rK45.x, 0.0f, 0.95f);
			rK45.y = Mathf.Clamp (rK45.y, 0.0f, 0.95f);}
	}

	// Push/pop list
	private static Stack<OSPManager.RoomModel> reflectionList = new Stack<OSPManager.RoomModel>();

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start () 
	{
	
	}

	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update () 
	{
	
	}

	/// <summary>
	/// Raises the trigger enter event.
	/// </summary>
	/// <param name="other">Other.</param>
	void OnTriggerEnter(Collider other) 
	{
		if(CheckForAudioListener(other.gameObject) == true)
		{
			PushCurrentReflectionValues();
		}
	}

	/// <summary>
	/// Raises the trigger exit event.
	/// </summary>
	/// <param name="other">Other.</param>
	void OnTriggerExit(Collider other)
	{
		if(CheckForAudioListener(other.gameObject) == true)
		{
			PopCurrentReflectionValues();			
		}
	}


	// * * * * * * * * * * * * *
	// Private functions

	/// <summary>
	/// Checks for audio listener.
	/// </summary>
	/// <returns><c>true</c>, if for audio listener was checked, <c>false</c> otherwise.</returns>
	/// <param name="gameObject">Game object.</param>
	bool CheckForAudioListener(GameObject gameObject)
	{
		AudioListener al = gameObject.GetComponentInChildren<AudioListener>();
		if(al != null)
			return true;

		return false;
	}
	
	/// <summary>
	/// Pushs the current reflection values onto reflectionsList stack.
	/// </summary>
	void PushCurrentReflectionValues()
	{
		if(OSPManager.sInstance == null)
		{
			Debug.LogWarning (System.String.Format ("OSPReflectionZone-PushCurrentReflectionValues: OSPManager does not exist in scene."));
			return;
		}

		OSPManager.RoomModel rm = new OSPManager.RoomModel();

		rm.DimensionX = OSPManager.sInstance.Dimensions.x;
		rm.DimensionY = OSPManager.sInstance.Dimensions.y;
		rm.DimensionZ = OSPManager.sInstance.Dimensions.z;

		rm.Reflection_K0 = OSPManager.sInstance.RK01.x;
		rm.Reflection_K1 = OSPManager.sInstance.RK01.y;
		rm.Reflection_K2 = OSPManager.sInstance.RK23.x;
		rm.Reflection_K3 = OSPManager.sInstance.RK23.y;
		rm.Reflection_K4 = OSPManager.sInstance.RK45.x;
		rm.Reflection_K5 = OSPManager.sInstance.RK45.y;

		reflectionList.Push(rm);	

		// Set the zone reflection values
		// NOTE: There will be conditions that might need resolution when dealing with volumes that 
		// overlap. Best practice is to never have volumes half-way inside other volumes; larger
		// volumes should completely contain smaller volumes
		SetReflectionValues();
	}

	/// <summary>
	/// Pops the current reflection values from reflectionsList stack.
	/// </summary>
	void PopCurrentReflectionValues()
	{
		if(OSPManager.sInstance == null)
		{
			Debug.LogWarning (System.String.Format ("OSPReflectionZone-PopCurrentReflectionValues: OSPManager does not exist in scene."));
			return;
		}

		if(reflectionList.Count == 0)
		{
			Debug.LogWarning (System.String.Format ("OSPReflectionZone-PopCurrentReflectionValues: reflectionList is empty."));
			return;
		}

		OSPManager.RoomModel rm = reflectionList.Pop();

		// Set the popped reflection values
		SetReflectionValues(ref rm);
	}


	/// <summary>
	/// Sets the reflection values. This is done when entering a zone (use zone values).
	/// </summary>
	void SetReflectionValues()
	{
		OSPManager.sInstance.Dimensions = Dimensions;
		OSPManager.sInstance.RK01       = RK01;
		OSPManager.sInstance.RK23       = RK23;
		OSPManager.sInstance.RK45       = RK45;
	}

	/// <summary>
	/// Sets the reflection values. This is done when exiting a zone (use popped values).
	/// </summary>
	/// <param name="rm">Rm.</param>
	void SetReflectionValues(ref OSPManager.RoomModel rm)
	{
		OSPManager.sInstance.Dimensions = new Vector3(rm.DimensionX, rm.DimensionY, rm.DimensionZ);
		OSPManager.sInstance.RK01       = new Vector3(rm.Reflection_K0, rm.Reflection_K1);
		OSPManager.sInstance.RK23       = new Vector3(rm.Reflection_K2, rm.Reflection_K3);
		OSPManager.sInstance.RK45       = new Vector3(rm.Reflection_K4, rm.Reflection_K5);
	}
}
