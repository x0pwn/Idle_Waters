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

	float Time < Attribute("Time"); >;
	RWTexture2DArray<float4> InitialSpectrum < Attribute( "InitialSpectrum" ); >;
	RWTexture2DArray<float4> Constants < Attribute( "Constants" ); >;
	RWTexture2DArray<float4> TimeBasedSpectrumDzDxDyDzx < Attribute( "TimeBasedSpectrumDzDxDyDzx" ); >;
	RWTexture2DArray<float4> TimeBasedSpectrumDxxDyxDzyDyy < Attribute( "TimeBasedSpectrumDxxDyxDzyDyy" ); >;

	float2 complexMultiply(float2 a, float2 b) {
		return float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
	}

	[numthreads( 8, 8, 1 )]
	void MainCs( uint3 id : SV_DispatchThreadID )
	{
		float2 k = float2(Constants[id.xyz].x, Constants[id.xyz].y);
		float kMagnitude = Constants[id.xyz].z;
		float kMagnitudeReciprocal = rcp(kMagnitude);
		float dispersionRelation = Constants[id.xyz].w;

		// representing complex numbers as float2
		float2 initialSpectrum = InitialSpectrum[id.xyz].rg;
		float2 initialSpectrumConjugate = InitialSpectrum[id.xyz].ba;

		// using eulers formula:
		// exp(i * w(k) * t) = cos(w(k) * t) + i sin(w(k) * t)
		float2 exp = float2(cos(dispersionRelation * Time), sin(dispersionRelation * Time));
		float2 expInverted = float2(exp.x, -exp.y);
		float2 h = complexMultiply(initialSpectrum, exp) + complexMultiply(initialSpectrumConjugate, expInverted);
		float2 ih = float2(-h.y, h.x);

		// using z for height since s&box uses z for up/down
		float2 dz = h;
		// fourier coefficients calculated from the "choppy waves" section of tessendorf's paper
		float2 dx = ih * k.x * kMagnitudeReciprocal;
		float2 dy = ih * k.y * kMagnitudeReciprocal;

		float2 dzx = ih * k.x; // i * kx * h
		float2 dxx = -h * k.x * k.x * kMagnitudeReciprocal; // -i * i * kx * kx * (1 / k) * h
		float2 dyx = -h * k.x * k.y * kMagnitudeReciprocal; // -i * i * kx * ky * (1 / k) * h
		float2 dzy = ih * k.y; // i * ky * h
		float2 dyy = -h * k.y * k.y * kMagnitudeReciprocal; // -i * i * ky * ky * (1 / k) * h

		// complex packing to reduce the number of textures needed to two
		// can be done because all the of the imaginary terms will add to 0 after IFFT
		float2 Dz_Dx = float2(dz.x - dx.y, dz.y + dx.x);
		float2 Dy_Dzx = float2(dy.x - dzx.y, dy.y + dzx.x);
		float2 Dxx_Dyx = float2(dxx.x - dyx.y, dxx.y + dyx.x);
		float2 Dzy_Dyy = float2(dzy.x - dyy.y, dzy.y + dyy.x);

		TimeBasedSpectrumDzDxDyDzx[id.xyz] = float4(Dz_Dx, Dy_Dzx);
		TimeBasedSpectrumDxxDyxDzyDyy[id.xyz] = float4(Dxx_Dyx, Dzy_Dyy);

		// TimeBasedSpectrumDzDxDyDzx[id.xy] = float4(initialSpectrum.x, initialSpectrum.y, 0, 1);
		// TimeBasedSpectrumDxxDyxDzyDyy[id.xy] = float4(0, 0, 0, 0);
	}
}