Shader "BovineLabs/SelectionSkinned" {
    SubShader {
        Tags { "RenderType"="Transparent" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _SelectionColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            inline fixed4 ConvertToDestinationColorSpace(fixed4 color)
            {
                return fixed4(LinearToGammaSpace(color.rgb), color.w);
            }

            float4 vert (float4 vertex : POSITION) : SV_POSITION {
                return UnityObjectToClipPos(vertex);
            }

            fixed4 frag () : SV_Target {
                return ConvertToDestinationColorSpace(UNITY_ACCESS_INSTANCED_PROP(Props, _SelectionColor));
            }
            ENDCG
        }
    }
}
