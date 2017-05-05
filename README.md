# VRGIN
VR injection framework for Unity -- helps you integrate VR into games that don't natively support it.

## What it does
- It takes care of the cameras and the GUI
- It gives you a frame to work with (Controllers, Modes, etc.)
- It comes with presets for standing and seated mode, plus a set of tools for your controllers
- There is a bunch of helper classes, e.g. for shortcuts and for rumble

## What it doesn't do
- It does not take care of the injection into the DLL itself -- use a DLL injection library for this, e.g. the [Illusion Plugin Architecture](https://github.com/Eusth/IPA) or Rei Patcher. Refer to [the template](https://github.com/Eusth/VRGIN.Template) for a project that's ready to build and use.

## Current state

| Unity Version | Compatibility |
| --------------|---------------|
| < 4.6         | Unsupported (VRGIN makes use of `UnityEngine.UI.dll`. |
| 4.6, 4.7      | Somewhat supported, but does not work out of the box with the current master branch. See [PlayClubVR](https://github.com/Eusth/PlayClubVR) for a functional example that uses an older version of this framework. |
| 5.0 - 5.6     | Supported. However, note that this does not currently leverage Unity's native OpenVR implementation (although you could relatively easily switch to it.) |


## How to use it

Use [the template](https://github.com/Eusth/VRGIN.Template) to get started. For some (somewhat outdated) insight behind the scenes,  see [Hacking VR into a Unity game](https://github.com/Eusth/VRGIN/wiki/Hacking-VR-into-a-Unity-game). For examples, take a look at my VR mods.
