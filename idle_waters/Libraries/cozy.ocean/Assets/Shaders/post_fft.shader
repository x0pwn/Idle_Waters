MODES
{
    Default();
}

COMMON
{
	#include "common/shared.hlsl"
}

CS
{
	#include "system.fxc"

	#define SIZE 256

	float Lambda < Attribute( "Lambda" ); >;
	float FoamBias < Attribute( "FoamBias" ); >;
	float FoamDecayRate < Attribute( "FoamDecayRate" ); >;
	float FoamAdd < Attribute( "FoamAdd" ); >;
	float FoamThreshold < Attribute( "FoamThreshold" ); >;

	RWTexture2DArray<float4> InputTextureDzDxDyDzx < Attribute( "InputTextureDzDxDyDzx" ); >;
	RWTexture2DArray<float4> InputTextureDxxDyxDzyDyy < Attribute( "InputTextureDxxDyxDzyDyy" ); >;
	RWTexture2DArray<float4> OutputTexture < Attribute( "OutputTexture" ); >;
	RWTexture2DArray<float4> OutputNormalTexture < Attribute( "OutputNormalTexture" ); >;

	[numthreads( 8, 8, 1 )]
	void MainCs( uint3 id : SV_DispatchThreadID )
	{
		float4 DzDxDyDzx = InputTextureDzDxDyDzx[id.xyz] * (1.0 - 2.0 * ((id.x + id.y) % 2));
		float4 DxxDyxDzyDyy = InputTextureDxxDyxDzyDyy[id.xyz] * (1.0 - 2.0 * ((id.x + id.y) % 2));

		float jacobian = (1 + Lambda * DxxDyxDzyDyy.x) * (1 + Lambda * DxxDyxDzyDyy.w) - (Lambda * Lambda * DxxDyxDzyDyy.y * DxxDyxDzyDyy.y);
		float foam = OutputTexture[id.xyz].w;
		foam *= exp(-FoamDecayRate);
		foam = saturate(foam);

		float biasedJacobian = max(0.0f, -(jacobian - FoamBias));
		if (biasedJacobian > FoamThreshold) {
			foam += FoamAdd * biasedJacobian;
		}

		// taking the real components, scaling them, and flipping every other number's sign
		// order is dx, dy, dz
		OutputTexture[id.xyz] = float4(Lambda * DzDxDyDzx.y, Lambda * DzDxDyDzx.z, DzDxDyDzx.x, foam);
		// dzx, dzy, dxx, dyy
		OutputNormalTexture[id.xyz] = float4(DzDxDyDzx.w, DxxDyxDzyDyy.z, DxxDyxDzyDyy.x, DxxDyxDzyDyy.w);
	}	
}