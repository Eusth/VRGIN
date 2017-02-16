using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Visuals
{
    public class DefaultMaterialPalette : IMaterialPalette
    { 
        public DefaultMaterialPalette()
        {
            Unlit = CreateUnlit();
            UnlitTransparent = CreateUnlitTransparent();
            UnlitTransparentCombined = CreateUnlitTransparentCombined();
            StandardShader = CreateStandardShader();
            Sprite = CreateSprite();

            if (!Unlit || !Unlit.shader) VRLog.Error("Could not load Unlit material!");
            if (!UnlitTransparent || !UnlitTransparent.shader) VRLog.Error("Could not load UnlitTransparent material!");
            if (!UnlitTransparentCombined || !UnlitTransparentCombined.shader) VRLog.Error("Could not load UnlitTransparentCombined material!");
            if (!StandardShader) VRLog.Error("Could not load StandardShader material!");

            if (!Sprite || !Sprite.shader)
            {
                VRLog.Error("Could not load Sprite material!");

                // Fall back to alternative material
                Sprite = UnlitTransparent;
            }
        }
        
        private Material CreateUnlitTransparentCombined()
        {
#if UNITY_4_5

            return new Material(@"Shader ""UnlitTransparentCombined"" {
                                Properties {
                                    _MainTex (""Base (RGB)"", 2D) = ""white"" {}
                                    _SubTex (""Base (RGB)"", 2D) = ""white"" {}
                                }
                                Category {
                                   Lighting Off
                                   Cull Back
                                   Blend SrcAlpha OneMinusSrcAlpha
                                   
                                   SubShader {
                                        Tags {""Queue""=""Transparent+1000""  ""IgnoreProjector""=""True""}
                                        Pass {
                                           SetTexture [_MainTex] {
                                                  Combine Texture, Texture
                                            }
                                            SetTexture [_SubTex] { combine Texture lerp(Texture) Previous }
                                        }
                                    } 
                                }
                            }");
#else
            return new Material(UnityHelper.GetShader("UnlitTransparentCombined"));
#endif
        }

        public Material UnlitTransparentCombined
        {
            get; set;
        }


        private Material CreateSprite()
        {
            return Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
        }
        public Material Sprite
        {
            get; set;
        }


        private Shader CreateStandardShader()
        {
            return Shader.Find("Standard");
        }
        public Shader StandardShader
        {
            get; set;
        }


        private Material CreateUnlit()
        {
#if UNITY_4_5
            return new Material(@"Shader ""Unlit"" {
                Properties {
                    _Color (""Main Color"", Color) = (1,1,1,1)
                    _MainTex (""Base (RGB)"", 2D) = ""white"" {}
                }
                Category {
                    Lighting Off
                    ZWrite On
                    Cull Back
                    SubShader {
                        Pass {
                            SetTexture [_MainTex] {
                                constantColor [_Color]
                                Combine texture * constant, texture * constant
                                }
                        }
                    }
                }
            }");
#else
            return new Material(UnityHelper.GetShader("Unlit"));
#endif
        }
        public Material Unlit
        {
            get; set;
        }


        private Material CreateUnlitTransparent()
        {
#if UNITY_4_5
            return new Material(@"Shader ""UnlitTransparent"" {
                                Properties {
                                    _MainTex (""Base (RGB)"", 2D) = ""white"" {}
                                }
                                Category {
                                   Lighting Off
                                   ZWrite Off
                                   Cull Back
                                   Blend SrcAlpha OneMinusSrcAlpha
                                   
                                   SubShader {
                                        Tags {""Queue""=""Transparent+1000""}
                                        Pass {
                                           SetTexture [_MainTex] {
                                                  Combine Texture, Texture + Texture
                                            }
                                        }
                                    } 
                                }
                            }");
#else
            return new Material(UnityHelper.GetShader("UnlitTransparent"));
#endif
        }
        public Material UnlitTransparent
        {
            get; set;
        }
    }
}
