using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core.Visuals
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
        }
        
        private Material CreateUnlitTransparentCombined()
        {
            return new Material(@"Shader ""Unlit/AlphaSelfIllum"" {
                                Properties {
                                    _MainTex (""Base (RGB)"", 2D) = ""white"" {}
                                    _SubTex (""Base (RGB)"", 2D) = ""white"" {}
                                }
                                Category {
                                   Lighting Off
                                   ZWrite Off
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
            return new Material(@"Shader ""Unlit Single Color"" {
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
        }
        public Material Unlit
        {
            get; set;
        }


        private Material CreateUnlitTransparent()
        {
            return new Material(@"Shader ""Unlit/AlphaSelfIllum"" {
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
        }
        public Material UnlitTransparent
        {
            get; set;
        }
    }
}
