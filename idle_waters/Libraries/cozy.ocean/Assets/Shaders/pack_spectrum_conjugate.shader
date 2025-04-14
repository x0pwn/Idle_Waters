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

	uint Size < Attribute("Size"); >;
	RWTexture2DArray<float4> Spectrum < Attribute( "Spectrum" ); >;

	[numthreads( 8, 8, 1 )]
	void MainCs( uint3 id : SV_DispatchThreadID )
	{
		float4 h0K = Spectrum[id.xyz];
		float4 h0MinusK = Spectrum[uint3((Size - id.x) % Size, (Size - id.y) % Size, id.z)];
		Spectrum[id.xyz] = float4(h0K.x, h0K.y, h0MinusK.x, -h0MinusK.y);
	}	
}