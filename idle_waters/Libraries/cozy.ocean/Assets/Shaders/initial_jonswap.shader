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

	static const float PI = 3.1415926;
	static const float g = 9.81;

	struct SpectrumParameters
	{
		float scale;
		float angle;
		float spreadBlend;
		float swell;
		float alpha;
		float peakOmega;
		float gamma;
		float shortWavesFade;
	};

	struct WaterLayer
	{
		int lengthScale;
		float cutoffLow;
		float cutoffHigh;
	};

	uint Size < Attribute("Size"); >;
	float Depth < Attribute("Depth"); >;
	RWTexture2D<float4> Noise < Attribute( "GaussianNoise" ); >;
	RWTexture2DArray<float4> Spectrum < Attribute( "Spectrum" ); >;
	// k.x, k.y, k magnitude, dispersion relation
	RWTexture2DArray<float4> Constants < Attribute( "Constants" ); >;
	StructuredBuffer<SpectrumParameters> Spectrums < Attribute( "SpectrumParameters" ); >;
	StructuredBuffer<WaterLayer> WaterLayers < Attribute("WaterLayers"); >;

	
	float Frequency(float k, float depth)
	{
		return sqrt(g * k * tanh(min(k * depth, 20)));
	}

	float FrequencyDerivative(float k, float depth)
	{
		float th = tanh(min(k * depth, 20));
		float ch = cosh(k * depth);
		return g * (depth * k / ch / ch + th) / Frequency(k, depth) / 2;
	}

	float NormalisationFactor(float s)
	{
		float s2 = s * s;
		float s3 = s2 * s;
		float s4 = s3 * s;
		if (s < 5)
			return -0.000564 * s4 + 0.00776 * s3 - 0.044 * s2 + 0.192 * s + 0.163;
		else
			return -4.80e-08 * s4 + 1.07e-05 * s3 - 9.53e-04 * s2 + 5.90e-02 * s + 3.93e-01;
	}

	float DonelanBannerBeta(float x)
	{
		if (x < 0.95)
			return 2.61 * pow(abs(x), 1.3);
		if (x < 1.6)
			return 2.28 * pow(abs(x), -1.3);
		float p = -0.4 + 0.8393 * exp(-0.567 * log(x * x));
		return pow(10, p);
	}

	float DonelanBanner(float theta, float omega, float peakOmega)
	{
		float beta = DonelanBannerBeta(omega / peakOmega);
		float sech = 1 / cosh(beta * theta);
		return beta / 2 / tanh(beta * 3.1416) * sech * sech;
	}

	float Cosine2s(float theta, float s)
	{
		return NormalisationFactor(s) * pow(abs(cos(0.5 * theta)), 2 * s);
	}

	float SpreadPower(float omega, float peakOmega)
	{
		if (omega > peakOmega)
		{
			return 9.77 * pow(abs(omega / peakOmega), -2.5);
		}
		else
		{
			return 6.97 * pow(abs(omega / peakOmega), 5);
		}
	}

	float DirectionSpectrum(float theta, float omega, SpectrumParameters pars)
	{
		float s = SpreadPower(omega, pars.peakOmega)
			+ 16 * tanh(min(omega / pars.peakOmega, 20)) * pars.swell * pars.swell;
		return lerp(2 / 3.1415 * cos(theta) * cos(theta), Cosine2s(theta - pars.angle, s), pars.spreadBlend);
	}


	float TMACorrection(float omega, float depth)
	{
		float omegaH = omega * sqrt(depth / g);
		if (omegaH <= 1) {
			return 0.5 * omegaH * omegaH;
		}
			
		if (omegaH < 2) {
			return 1.0 - 0.5 * (2.0 - omegaH) * (2.0 - omegaH);
		}
			
		return 1;
	}

	float JONSWAP(float omega, float depth, SpectrumParameters pars)
	{
		float sigma;
		if (omega <= pars.peakOmega) {
			sigma = 0.07;
		} else {
			sigma = 0.09;
		}
			
		float r = exp(-(omega - pars.peakOmega) * (omega - pars.peakOmega)
			/ 2 / sigma / sigma / pars.peakOmega / pars.peakOmega);
		
		float oneOverOmega = 1 / omega;
		float peakOmegaOverOmega = pars.peakOmega / omega;
		return pars.scale * TMACorrection(omega, depth) * pars.alpha * g * g
			* oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega
			* exp(-1.25 * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega)
			* pow(abs(pars.gamma), r);
	}

	float ShortWavesFade(float kLength, SpectrumParameters pars)
	{
		return exp(-pars.shortWavesFade * pars.shortWavesFade * kLength * kLength);
	}

	// referenced from https://github.com/gasgiant/FFT-Ocean/blob/main/Assets/ComputeShaders/InitialSpectrum.compute
	// computes the jonswap spectrum with swell
	[numthreads( 8, 8, 1 )]
	void MainCs( uint3 id : SV_DispatchThreadID )
	{
		WaterLayer layer = WaterLayers[id.z];
		float deltaK = 2 * PI / layer.lengthScale;
		int nx = id.x - Size / 2;
		int nz = id.y - Size / 2;
		float2 k = float2(nx, nz) * deltaK;
		float kMagnitude = length(k);
		
		if (kMagnitude <= layer.cutoffHigh && kMagnitude >= layer.cutoffLow) {
			float kAngle = atan2(k.y, k.x);
			float dispersionRelation = Frequency(kMagnitude, Depth);
			float dispersionRelationDerivative = FrequencyDerivative(kMagnitude, Depth);
			float spectrum = JONSWAP(dispersionRelation, Depth, Spectrums[0])
			* DirectionSpectrum(kAngle, dispersionRelation, Spectrums[0]) * ShortWavesFade(kMagnitude, Spectrums[0]);

			// adds in swell
			if (Spectrums[1].scale > 0) {
				spectrum += JONSWAP(dispersionRelation, Depth, Spectrums[1])
				* DirectionSpectrum(kAngle, dispersionRelation, Spectrums[1]) * ShortWavesFade(kMagnitude, Spectrums[1]);
			}

			float2 h0 = float2(Noise[id.xy].x, Noise[id.xy].y) * sqrt(2 * spectrum * abs(dispersionRelationDerivative) / kMagnitude * deltaK * deltaK);

			Spectrum[id.xyz] = float4(h0.x, h0.y, 0, 0);
			Constants[id.xyz] = float4(k.x, k.y, kMagnitude, dispersionRelation);
		} else {
			Spectrum[id.xyz] = 0;
			Constants[id.xyz] = float4(k.x, k.y, 1, 0);
		}

		// Constants[id.xyz] = float4(layer.lengthScale, layer.cutoffHigh, layer.cutoffLow, kMagnitude);
	}	
}