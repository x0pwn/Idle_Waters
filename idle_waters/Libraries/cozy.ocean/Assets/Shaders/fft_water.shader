FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    VrForward();
    Depth();
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"

    float3 worldPos : TEXCOORD8;
};

VS
{
    #include "common/vertex.hlsl"

    Texture2DArray HeightMap < Attribute("HeightMap"); >;
    bool EnableLengthScale0 < Attribute("EnableLengthScale0"); >;
    bool EnableLengthScale1 < Attribute("EnableLengthScale1"); >;
    bool EnableLengthScale2 < Attribute("EnableLengthScale2"); >;
    int LengthScale0 < Attribute("LengthScale0"); >;
    int LengthScale1 < Attribute("LengthScale1"); >;
    int LengthScale2 < Attribute("LengthScale2"); >;

    PixelInput MainVs( VertexInput i )
    {
        PixelInput o = ProcessVertex( i );
        float3x4 matObjectToWorld = CalculateInstancingObjectToWorldMatrix( i );
        float3 worldPos = mul(matObjectToWorld, float4(i.vPositionOs, 1.0));
        
        float3 displacement = 0;
        
        if (EnableLengthScale0) {
            displacement += HeightMap.SampleLevel( g_sPointWrap, float3(worldPos.xy / LengthScale0, 0), 0).xyz;
        }
        
        if (EnableLengthScale1) {
            displacement += HeightMap.SampleLevel( g_sPointWrap, float3(worldPos.xy / LengthScale1, 1), 0).xyz;
        }
        
        if (EnableLengthScale2) {
            displacement += HeightMap.SampleLevel( g_sPointWrap, float3(worldPos.xy / LengthScale2, 2), 0).xyz;
        }

        o.vPositionWs = worldPos + displacement;
        o.vPositionPs.xyzw = Position3WsToPs( o.vPositionWs.xyz );
        o.worldPos = worldPos;

        return FinalizeVertex( o );
    }
}

PS
{
    #include "common/pixel.hlsl"

    static const float PI = 3.1415926;

    Texture2DArray HeightMap < Attribute("HeightMap"); >;
    Texture2DArray HeightMapNormals < Attribute("HeightMapNormals"); >;
    bool EnableLengthScale0 < Attribute("EnableLengthScale0"); >;
    bool EnableLengthScale1 < Attribute("EnableLengthScale1"); >;
    bool EnableLengthScale2 < Attribute("EnableLengthScale2"); >;
    int LengthScale0 < Attribute("LengthScale0"); >;
    int LengthScale1 < Attribute("LengthScale1"); >;
    int LengthScale2 < Attribute("LengthScale2"); >;

    float3 AmbientColor < Attribute("AmbientColor"); >;
    float3 SunColor < Attribute("SunColor"); >;
    float3 LightColor < Attribute("LightColor"); >;
    float3 LightDirection < Attribute("LightDirection"); >;

    float3 DiffuseReflectance < Attribute("DiffuseReflectance"); >;
    float FresnelShininess < Attribute("FresnelShininess"); >;
    float FresnelBias < Attribute("FresnelBias"); >;
    float FresnelStrength < Attribute("FresnelStrength"); >;
    float3 SpecularReflectance < Attribute("SpecularReflectance"); >;

    // Based on Acerola's simpler water shader: https://github.com/GarrettGunnell/Water/blob/main/Assets/Shaders/FFTWater.shader#L313
    float4 MainPs( PixelInput i ) : SV_Target0
    {
        // float foam = 0;
        float4 derivatives = 0;

        if (EnableLengthScale0) {
            derivatives += HeightMapNormals.SampleLevel(g_sPointWrap, float3(i.worldPos.xy / LengthScale0, 0), 0);
            // foam += HeightMap.SampleLevel( g_sTrilinearWrap, float3(i.worldPos.xy / LengthScale0, 0), 0).a;
        }

        if (EnableLengthScale1) {
            derivatives += HeightMapNormals.SampleLevel(g_sPointWrap, float3(i.worldPos.xy / LengthScale1, 1), 0);
            // foam += HeightMap.SampleLevel( g_sTrilinearWrap, float3(i.worldPos.xy / LengthScale1, 1), 0).a;
        }

        if (EnableLengthScale2) {
            derivatives += HeightMapNormals.SampleLevel(g_sPointWrap, float3(i.worldPos.xy / LengthScale2, 2), 0);
            // foam += HeightMap.SampleLevel( g_sTrilinearWrap, float3(i.worldPos.xy / LengthScale2, 2), 0).a;
        }
        
        float3 viewDir = normalize(g_vCameraPositionWs - i.worldPos);
        float3 lightDir = normalize(LightDirection.xyz);

        float2 slope = float2(derivatives.x / (1 + derivatives.z), derivatives.y / (1 + derivatives.w));
        float3 worldNormal = normalize(float3(-slope.x, -slope.y, 1));

        float3 halfwayDir = normalize(lightDir + viewDir);
        float ndotl = max(0.0, dot(lightDir, worldNormal));

        // Diffuse calculation
        float3 diffuseReflectance = DiffuseReflectance / PI;
        float3 diffuse = LightColor * ndotl * diffuseReflectance;

        // Fresnel calculation
        float base = 1.0 - dot(viewDir, worldNormal);
        float exponential = pow(base, FresnelShininess);
        float R = exponential + FresnelBias * (1.0 - exponential);
        R *= FresnelStrength;
        
        // Environment reflection
        float3 reflectedDir = reflect(-viewDir, worldNormal);
        float3 skyCol = AmbientLight::From(i.worldPos, i.vTextureCoords.xy, reflectedDir);
        
        // Sun specular highlight
        float3 sun = SunColor * pow(max(0.0, dot(reflectedDir, lightDir)), 1.0);

        float3 fresnel = skyCol.rgb * R;
        fresnel += sun * R;

        // Specular calculation
        float spec = max(0.0, dot(worldNormal, halfwayDir)) * ndotl;
        float3 specular = LightColor * SpecularReflectance * spec;

        // Specular Fresnel
        base = 1.0 - max(0.0, dot(viewDir, halfwayDir));
        exponential = pow(base, 5.0);
        R = exponential + FresnelBias * (1.0 - exponential);
        specular *= R;

        // Final combination
        float3 output = AmbientColor + diffuse + specular + fresnel;
        
        return float4(output, 1.0);
    }
}