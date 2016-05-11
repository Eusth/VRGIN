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
            UnlitTransparentCombined = new Material(@"Shader ""Unlit/AlphaSelfIllum"" {
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
                                        Tags {""Queue""=""Transparent+100""}
                                        Pass {
                                           SetTexture [_MainTex] {
                                                  Combine Texture, Texture + Texture
                                            }
                                            SetTexture [_SubTex] { combine Texture lerp(Texture) Previous }
                                        }
                                    } 
                                }
                            }");

            UnlitTransparent = new Material(@"Shader ""Unlit/AlphaSelfIllum"" {
                                Properties {
                                    _MainTex (""Base (RGB)"", 2D) = ""white"" {}
                                }
                                Category {
                                   Lighting Off
                                   ZWrite Off
                                   Cull Back
                                   Blend SrcAlpha OneMinusSrcAlpha
                                   
                                   SubShader {
                                        Tags {""Queue""=""Transparent+100""}
                                        Pass {
                                           SetTexture [_MainTex] {
                                                  Combine Texture, Texture + Texture
                                            }
                                        }
                                    } 
                                }
                            }");

            Sprite = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
            StandardShader = Shader.Find("Standard");
            Unlit = new Material(@"Shader ""Unlit Single Color"" {
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


        public Material UnlitTransparentCombined
        {
            get; private set;
        }

        public Material Sprite
        {
            get; private set;
        }

        public Shader StandardShader
        {
            get; private set;
        }

        public Material Unlit
        {
            get; private set;
        }

        public Material UnlitTransparent
        {
            get; private set;
        }
    }
}
