//========= Copyright 2015, Valve Corporation, All rights reserved. ===========
//
// Purpose: Flips the camera output back to normal for D3D.
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System;
using VRGIN.Core;
using VRGIN.Helpers;

public class SteamVR_CameraFlip : MonoBehaviour
{
	static Material blitMaterial;

	void OnEnable()
	{
        try
        {
            if (blitMaterial == null)
                blitMaterial = new Material(UnityHelper.GetShader("SteamVR_BlitFlip"));
        } catch(Exception e)
        {
            Console.WriteLine(e);
        }
	}

	void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
        try
        {
            Graphics.Blit(src, dest, blitMaterial);
        } catch(Exception e)
        {
            Console.WriteLine(e);
        }
	}
}

