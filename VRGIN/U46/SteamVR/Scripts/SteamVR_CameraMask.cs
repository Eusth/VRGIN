//========= Copyright 2014, Valve Corporation, All rights reserved. ===========
//
// Purpose: Masks out pixels that cannot be seen through the connected hmd.
//
//=============================================================================

using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System;
using VRGIN.Core;
using VRGIN.Helpers;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SteamVR_CameraMask : ProtectedBehaviour
{
	static Material material;
	static Mesh[] hiddenAreaMeshes = new Mesh[] { null, null };

	MeshFilter meshFilter;

	protected override void OnAwake()
	{
		meshFilter = GetComponent<MeshFilter>();

		if (material == null)
			material = new Material(UnityHelper.GetShader("SteamVR_HiddenArea"));

		var mr = GetComponent<MeshRenderer>();
		mr.material = material;
        mr.castShadows = false;
		//mr.shadowCastingMode = ShadowCastingMode.Off;
		mr.receiveShadows = false;
#if !(UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0 || UNITY_4_5 || UNITY_4_5)
		mr.lightProbeUsage = LightProbeUsage.Off;
#else
		mr.useLightProbes = false;
#endif
		//mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
	}

	public void Set(SteamVR vr, Valve.VR.EVREye eye)
	{
        try
        {
            int i = (int)eye;
            if (hiddenAreaMeshes[i] == null)
                hiddenAreaMeshes[i] = SteamVR_Utils.CreateHiddenAreaMesh(vr.hmd.GetHiddenAreaMesh(eye), vr.textureBounds[i]);
            meshFilter.mesh = hiddenAreaMeshes[i];
        } catch(Exception e)
        {
            Console.WriteLine(e);
        }
	}

	public void Clear()
	{
		meshFilter.mesh = null;
	}
}

