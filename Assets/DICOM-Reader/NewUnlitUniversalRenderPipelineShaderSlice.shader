Shader "VolumeRendering/Basic_URP_Slice"
{
    Properties
    {
        [Header(Rendering)]
        _Volume("Volume", 3D) = "" {}
        _Transfer("Transfer", 2D) = "" {}
        _Iteration("Iteration", Int) = 10
        _Intensity("Intensity", Range(0.0, 1.0)) = 0.1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend Dst", Float) = 10

        [Header(Ranges)]
        _SliceNum("SliceNum", Range(0, 0.995)) = 0.0
        
        [HideInInspector] _MinX("MinX", Range(0, 1)) = 0.0
        [HideInInspector] _MaxX("MaxX", Range(0, 1)) = 1.0
        [HideInInspector] _MinY("MinY", Range(0, 1)) = 0.0
        [HideInInspector] _MaxY("MaxY", Range(0, 1)) = 1.0
        [HideInInspector] _MinZ("MinZ", Range(0, 1)) = 0
        [HideInInspector] _MaxZ("MaxZ", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
       
        Pass
        {
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend [_BlendSrc] [_BlendDst]
            Lighting Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Unity 6 (URP) のコアライブラリをインクルード
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 localPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            // URPでのテクスチャ定義
            TEXTURE3D(_Volume); SAMPLER(sampler_Volume);
            TEXTURE2D(_Transfer); SAMPLER(sampler_Transfer);

           
            // SRP Batcher対応のための定数バッファ
            CBUFFER_START(UnityPerMaterial)
                int _Iteration;
                float _Intensity;
                float _MinX, _MaxX, _MinY, _MaxY, _MinZ, _MaxZ;
                float _SliceNum;
            CBUFFER_END

            struct Ray
            {
                float3 from;
                float3 dir;
                float tmax;
            };

            void intersection(inout Ray ray)
            {
                float3 invDir = 1.0 / ray.dir;
                float3 t1 = (-0.5 - ray.from) * invDir;
                float3 t2 = (+0.5 - ray.from) * invDir;
                float3 tmax3 = max(t1, t2);
                float2 tmax2 = min(tmax3.xx, tmax3.yz);
                ray.tmax = min(tmax2.x, tmax2.y);
            }

            inline float sampleVolume(float3 pos)
            {
                float x = step(pos.x, _MaxX) * step(_MinX, pos.x);
                float y = step(pos.y, _MaxY) * step(_MinY, pos.y);
                float z = step(pos.z, _SliceNum+0.02) * step(_SliceNum, pos.z);
                return SAMPLE_TEXTURE3D(_Volume, sampler_Volume, pos).r * (x * y * z);
            }

            inline float4 transferFunction(float t)
            {
                return SAMPLE_TEXTURE2D(_Transfer, sampler_Transfer, float2(t, 0));
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.localPos = v.positionOS;
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 worldDir = IN.worldPos - GetCameraPositionWS();
                // ワールド空間の向きをローカル空間に変換
                float3 localDir = normalize(mul(GetWorldToObjectMatrix(), float4(worldDir, 0.0)).xyz);

                Ray ray;
                ray.from = IN.localPos.xyz;
                ray.dir = localDir;
                intersection(ray);

                int n = _Iteration * ray.tmax / sqrt(3.0);
                n = max(1, n); // ゼロ除算防止
               
                float3 localStep = localDir * ray.tmax / n;
                float3 localPos = IN.localPos.xyz;

                float4 output = 0;

                UNITY_LOOP
                for (int i = 0; i < n; ++i)
                {
                    float volume = sampleVolume(localPos + 0.5);
                    float4 color = transferFunction(volume) * volume * _Intensity;
                    output += (1.0 - output.a) * color;
                    localPos += localStep;
                }

                return output;
            }
            ENDHLSL
        }
    }
}