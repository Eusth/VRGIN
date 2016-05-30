# Upgrading SteamVR

When upgrading SteamVR, you have to follow these steps:

1. When the shaders have been changed, make a new Asset Bundle and replace the file `steamvr` with it.
2. Make a Find-and-Replace over all scripts in SteamVR, replacing *Shader.Find* with *VRGIN.Helpers.UnityHelper.GetShader*