Shader "TextMeshPro/Distance Field Soft Masked (Full)" 
{
    Properties 
    {
        // Face Properties
        _FaceTex ("Face Texture", 2D) = "white" {}
        _FaceUVSpeedX ("Face UV Speed X", Range(-5, 5)) = 0.0
        _FaceUVSpeedY ("Face UV Speed Y", Range(-5, 5)) = 0.0
        [HDR]_FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0

        // Outline Properties
        [HDR]_OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineTex ("Outline Texture", 2D) = "white" {}
        _OutlineUVSpeedX ("Outline UV Speed X", Range(-5, 5)) = 0.0
        _OutlineUVSpeedY ("Outline UV Speed Y", Range(-5, 5)) = 0.0
        _OutlineWidth ("Outline Thickness", Range(0, 1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0
        
        // Font Weights
    _WeightNormal ("Weight Normal", Float) = 0
    _WeightBold ("Weight Bold", Float) = 0.5
    _ShaderFlags ("Flags", Float) = 0
    _ScaleRatioA ("Scale RatioA", Float) = 1
    _ScaleRatioB ("Scale RatioB", Float) = 1
    _ScaleRatioC ("Scale RatioC", Float) = 1

        // Underlay Properties
        [HDR]_UnderlayColor ("Border Color", Color) = (0,0,0, 0.5)
        _UnderlayOffsetX ("Border OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY ("Border OffsetY", Range(-1,1)) = 0
        _UnderlayDilate ("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness ("Border Softness", Range(0,1)) = 0

        // Glow Properties
        [HDR]_GlowColor ("Color", Color) = (0, 1, 0, 0.5)
        _GlowOffset ("Offset", Range(-1,1)) = 0
        _GlowInner ("Inner", Range(0,1)) = 0.05
        _GlowOuter ("Outer", Range(0,1)) = 0.05
        _GlowPower ("Falloff", Range(1, 0)) = 0.75

        // Bevel Properties
        _Bevel ("Bevel", Range(0,1)) = 0.5
        _BevelOffset ("Bevel Offset", Range(-0.5,0.5)) = 0
        _BevelWidth ("Bevel Width", Range(-.5,0.5)) = 0
        _BevelClamp ("Bevel Clamp", Range(0,1)) = 0
        _BevelRoundness ("Bevel Roundness", Range(0,1)) = 0
        _LightAngle ("Light Angle", Range(0.0, 6.2831853)) = 3.1416
        [HDR]_SpecularColor ("Specular", Color) = (1,1,1,1)
        _SpecularPower ("Specular", Range(0,4)) = 2.0
        _Reflectivity ("Reflectivity", Range(5.0,15.0)) = 10
        _Diffuse ("Diffuse", Range(0,1)) = 0.5
        _Ambient ("Ambient", Range(1,0)) = 0.5

        // Bump Map
        _BumpMap ("Normal map", 2D) = "bump" {}
        _BumpOutline ("Bump Outline", Range(0,1)) = 0
        _BumpFace ("Bump Face", Range(0,1)) = 0

        // Reflection
        _ReflectFaceColor ("Reflection Color", Color) = (0,0,0,1)
        _ReflectOutlineColor ("Reflection Color", Color) = (0,0,0,1)
        _Cube ("Reflection Cubemap", Cube) = "black" {}
        _EnvMatrixRotation ("Texture Rotation", vector) = (0, 0, 0, 0)

        // Font Atlas
        _MainTex ("Font Atlas", 2D) = "white" {}
        _TextureWidth ("Texture Width", float) = 512
        _TextureHeight ("Texture Height", float) = 512
        _GradientScale ("Gradient Scale", float) = 5.0
        _ScaleX ("Scale X", float) = 1.0
        _ScaleY ("Scale Y", float) = 1.0
        _PerspectiveFilter ("Perspective Correction", Range(0, 1)) = 0.875
        _Sharpness ("Sharpness", Range(-1,1)) = 0

        // Font Weights
        _WeightNormal ("Weight Normal", float) = 0
        _WeightBold ("Weight Bold", float) = 0.5
        _ShaderFlags ("Flags", float) = 0
        _ScaleRatioA ("Scale RatioA", float) = 1
        _ScaleRatioB ("Scale RatioB", float) = 1
        _ScaleRatioC ("Scale RatioC", float) = 1

        // Vertex Offset
        _VertexOffsetX ("Vertex OffsetX", float) = 0
        _VertexOffsetY ("Vertex OffsetY", float) = 0

        // Masking
        _MaskCoord ("Mask Coordinates", vector) = (0, 0, 32767, 32767)
        _ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
        _MaskSoftnessX ("Mask SoftnessX", float) = 0
        _MaskSoftnessY ("Mask SoftnessY", float) = 0

        // Soft Mask
        _SoftMaskTex ("Soft Mask Texture", 2D) = "white" {}
        _SoftMaskFade ("Soft Mask Fade", Range(0, 1)) = 1
        _SoftMaskScale ("Soft Mask Scale", Vector) = (1,1,0,0)
        _SoftMaskOffset ("Soft Mask Offset", Vector) = (0,0,0,0)

        // Stencil
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        // Render States
        _CullMode ("Cull Mode", Float) = 0
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader 
    {
        Tags 
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Stencil 
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite Off
        Lighting Off
        Fog { Mode Off }
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass 
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex VertShader
            #pragma fragment PixShader
            #pragma shader_feature __ BEVEL_ON
            #pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER
            #pragma shader_feature __ GLOW_ON
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/TextMesh Pro/Resources/TMP_Properties.cginc"
            #include "Assets/TextMesh Pro/Shaders/TMPro.cginc"

            // Soft Mask Uniforms
            sampler2D _SoftMaskTex;
            float _SoftMaskFade;
            float4 _SoftMaskScale;
            float4 _SoftMaskOffset;

            struct vertex_t 
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 position : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct pixel_t 
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 atlas : TEXCOORD0;
                float4 param : TEXCOORD1;
                float4 mask : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float2 softMaskUV : TEXCOORD4;

                #if (UNDERLAY_ON || UNDERLAY_INNER)
                    float4 texcoord2 : TEXCOORD5;
                    fixed4 underlayColor : COLOR1;
                #endif
                float4 textures : TEXCOORD6;
            };
            float3 ApplyBevelEffects(float3 n, float sd, float outline, float faceAlpha, float3 viewDir) {
            float3 light = normalize(float3(sin(_LightAngle), cos(_LightAngle), -1.0));
            float3 col = _SpecularColor.rgb * faceAlpha;
            col *= (1 - dot(n, light) * _Diffuse);
            col *= lerp(_Ambient, 1, n.z * n.z);
            float3 refl = texCUBE(_Cube, reflect(viewDir, -n)).rgb;
            col += refl * lerp(_ReflectFaceColor.rgb, _ReflectOutlineColor.rgb, saturate(sd + outline * 0.5)) * faceAlpha;
            return col;
        }

            float4 _FaceTex_ST;
            float4 _OutlineTex_ST;

            pixel_t VertShader(vertex_t input)
            {
                pixel_t output;

                UNITY_INITIALIZE_OUTPUT(pixel_t, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float bold = step(input.texcoord1.y, 0);

                float4 vert = input.position;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;

                float4 vPosition = UnityObjectToClipPos(vert);

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float scale = rsqrt(dot(pixelSize, pixelSize));
                scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);
                if (UNITY_MATRIX_P[3][3] == 0) 
                    scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

                float bias = (.5 - weight) + (.5 / scale);

                float alphaClip = (1.0 - _OutlineWidth * _ScaleRatioA - _OutlineSoftness * _ScaleRatioA);

                #if GLOW_ON
                    alphaClip = min(alphaClip, 1.0 - _GlowOffset * _ScaleRatioB - _GlowOuter * _ScaleRatioB);
                #endif

                alphaClip = alphaClip / 2.0 - ( .5 / scale) - weight;

                #if (UNDERLAY_ON || UNDERLAY_INNER)
                    float4 underlayColor = _UnderlayColor;
                    underlayColor.rgb *= underlayColor.a;

                    float bScale = scale;
                    bScale /= 1 + ((_UnderlaySoftness*_ScaleRatioC) * bScale);
                    float bBias = (0.5 - weight) * bScale - 0.5 - ((_UnderlayDilate * _ScaleRatioC) * 0.5 * bScale);

                    float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
                    float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
                    float2 bOffset = float2(x, y);
                #endif

                // Generate UV for the Masking Texture
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

                // Support for texture tiling and offset
                float2 textureUV = UnpackUV(input.texcoord1.x);
                float2 faceUV = TRANSFORM_TEX(textureUV, _FaceTex);
                float2 outlineUV = TRANSFORM_TEX(textureUV, _OutlineTex);

                // Calculate soft mask UVs
                output.softMaskUV = (input.position.xy * 0.5 + 0.5) * _SoftMaskScale.xy + _SoftMaskOffset.xy;

                output.position = vPosition;
                output.color = input.color;
                output.atlas = input.texcoord0;
                output.param = float4(alphaClip, scale, bias, weight);
                output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));
                output.viewDir = mul((float3x3)_EnvMatrix, _WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, vert).xyz);

                #if (UNDERLAY_ON || UNDERLAY_INNER)
                    output.texcoord2 = float4(input.texcoord0 + bOffset, bScale, bBias);
                    output.underlayColor = underlayColor;
                #endif
                output.textures = float4(faceUV, outlineUV);

                return output;
            }

            fixed4 PixShader(pixel_t input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    // Sample mask FIRST
    float maskAlpha = tex2D(_SoftMaskTex, input.softMaskUV).a;
    float maskValue = saturate(maskAlpha * _SoftMaskFade);

    // Calculate all components INDEPENDENTLY
    float c = tex2D(_MainTex, input.atlas).a;
    #ifndef UNDERLAY_ON
        clip(c - input.param.x);
    #endif

    float scale = input.param.y;
    float bias = input.param.z;
    float weight = input.param.w;
    float sd = (bias - c) * scale;

    // 1. Calculate FACE with outline
    half4 faceColor = _FaceColor;
    half4 outlineColor = _OutlineColor;
    faceColor.rgb *= input.color.rgb;
    faceColor *= tex2D(_FaceTex, input.textures.xy);
    outlineColor *= tex2D(_OutlineTex, input.textures.zw);
    faceColor = GetColor(sd, faceColor, outlineColor, (_OutlineWidth * _ScaleRatioA) * scale, (_OutlineSoftness * _ScaleRatioA) * scale);
    
    // 2. Calculate UNDERLAY
    #if UNDERLAY_ON || UNDERLAY_INNER
        float d = tex2D(_MainTex, input.texcoord2.xy).a * input.texcoord2.z;
        half4 underlay = input.underlayColor;
        #if UNDERLAY_ON
            underlay *= saturate(d - input.texcoord2.w);
        #else
            underlay *= (1 - saturate(d - input.texcoord2.w)) * saturate(1 - sd);
        #endif
        underlay *= (1 - faceColor.a);
    #else
        half4 underlay = half4(0,0,0,0);
    #endif

    // 3. Calculate GLOW
    #if GLOW_ON
        half4 glow = GetGlowColor(sd, scale);
    #else
        half4 glow = half4(0,0,0,0);
    #endif

    // 4. Calculate BEVEL/REFLECTION
    #if BEVEL_ON
    float3 dxy = float3(0.5 / _TextureWidth, 0.5 / _TextureHeight, 0);
    float3 n = GetSurfaceNormal(input.atlas, weight, dxy);

    float3 bump = UnpackNormal(tex2D(_BumpMap, input.textures.xy + float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.y)).xyz;
    float outline = (_OutlineWidth * _ScaleRatioA) * scale;
    n = normalize(n - bump);

    float3 light = normalize(float3(sin(_LightAngle), cos(_LightAngle), -1.0));

    float3 spec = GetSpecular(n, light);
    float3 diffuseTerm = 1 - (dot(n, light) * _Diffuse);
    float3 ambientTerm = lerp(_Ambient, 1, n.z * n.z);

    fixed4 reflcol = texCUBE(_Cube, reflect(input.viewDir, -n));
    float3 reflection = reflcol.rgb * lerp(_ReflectFaceColor.rgb, _ReflectOutlineColor.rgb, saturate(sd + outline * 0.5));

    float3 bevelEffect = (spec + reflection) * faceColor.a;
    bevelEffect *= diffuseTerm * ambientTerm;

    faceColor.rgb += bevelEffect;
#endif
    

    // COMBINE ALL ELEMENTS WITH MASK
    half4 finalColor = half4(0,0,0,0);
    finalColor.rgb = underlay.rgb + glow.rgb + faceColor.rgb;
    finalColor.a = max(underlay.a, max(glow.a, faceColor.a));
    
    // APPLY MASK TO FINAL COMPOSITE
    finalColor.a *= maskValue;
    finalColor.rgb *= maskValue; // Important: Also mask RGB channels

    // Apply clipping
    #if UNITY_UI_CLIP_RECT
        half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
        finalColor *= m.x * m.y;
    #endif

    #if UNITY_UI_ALPHACLIP
        clip(finalColor.a - 0.001);
    #endif

    return finalColor;
}
            ENDCG
        }
    }
    Fallback "TextMeshPro/Mobile/Distance Field"
    CustomEditor "TMP_SoftMaskShaderGUI"
}