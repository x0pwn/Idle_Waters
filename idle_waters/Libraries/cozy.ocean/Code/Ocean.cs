
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using Sandbox;

public struct WaterLayer
{
	public int lengthScale;
	public float cutoffLow;
	public float cutoffHigh;
}

public struct ShaderSpectrumSettings
{
	public float scale;
	public float angle;
	public float spreadBlend;
	public float swell;
	public float alpha;
	public float peakOmega;
	public float gamma;
	public float shortWavesFade;
}

public struct EditorSpectrumSettings
{
	[Range(0, 1)]
	public float scale { get; set; }
	public float windSpeed { get; set; }
	
	public float windDirection { get; set; }
	public float fetch { get; set; }
	[Range(0, 1)]
	public float spreadBlend { get; set; }
	[Range(0, 1)]
	public float swell;
	public float peakEnhancement { get; set; }
	public float shortWavesFade { get; set; }
}

public sealed class Ocean : Component
{
	[RequireComponent]
	public ModelRenderer waterRenderer { get; set; }
	
	[Property]
	public GameObject Sun { get; set; }
	
	[Property] [Range( 0, 100000 )] 
	public int seed = 12345;
	
	[Property] [Range( 0, 10 )] 
	public float timeScale = 1.0f;
	
	private int resolution = 256;

	[Property, InlineEditor]
	public EditorSpectrumSettings spectrumSettings { get; set; } = new EditorSpectrumSettings
	{
		scale = 0.6f,
		windSpeed = 20f,
		windDirection = 20f,
		fetch = 100000,
		spreadBlend = 0.75f,
		swell = 0.75f,
		peakEnhancement = 3f,
		shortWavesFade = 0.05f
	};

	[Property, InlineEditor]
	public EditorSpectrumSettings secondSpectrumSettings { get; set; } = new EditorSpectrumSettings
	{
		scale = 0.1f,
		windSpeed = 2f,
		windDirection = 30f,
		fetch = 1000,
		spreadBlend = 0,
		swell = 0,
		peakEnhancement = 1f,
		shortWavesFade = 0.01f
	};
	
	[Property]
	public float depth = 500f;
	
	[Property]
	public bool enableLengthScale0 = true;
	[Property]
	public int lengthScale0 = 250;
	
	
	[Property]
	public bool enableLengthScale1 = true;
	[Property]
	public int lengthScale1 = 100;
	
	
	[Property]
	public bool enableLengthScale2 = true;
	[Property]
	public int lengthScale2 = 10;

	[Property] 
	public bool recalculateInitials = true;

	[Property]
	public int planeLength = 1000;
	[Property]
	public int planeResolution = 1000;

	[Property] 
	public Color waterColor = new( 0, 0.1f, 0.16f, 1f );
	[Property] 
	public Color diffuseReflectance = new( 1, 1, 1, 0 );
	[Property] 
	public Color specularReflectance = new( 1, 1, 1, 0 );
	[Property] 
	public float fresnelShininess = 5;
	[Property] 
	public float fresnelBias = 0;
	[Property] 
	public float fresnelStrength = 0.3f;
	
	private ComputeShader initialSpectrumShader = new ComputeShader( "Shaders/initial_jonswap" );
	private ComputeShader packSpectrumConjugateShader = new ComputeShader( "Shaders/pack_spectrum_conjugate" );
	private ComputeShader timeBasedSpectrumShader = new ComputeShader( "Shaders/time_based_phillips_spectrum" );
	private ComputeShader inverseFFTShader = new ComputeShader( "Shaders/inverse_fft" );
	private ComputeShader inverseFFTPostProcessShader = new ComputeShader( "Shaders/post_fft" );
	private Material material = Material.Load( "Materials/fft_water.vmat" );
	
	public Texture NoiseTexture { get; set; }
	public Texture InitialSpectrumTexture { get; set; }
	
	public Texture InitialConstantsTexture { get; set; }
	public Texture TimeBasedSpectrumTextureDzDxDyDzx { get; set; }
	public Texture TimeBasedSpectrumTextureDxxDyxDzyDyy { get; set; }
	
	public Texture WaterAmplitudeTexture { get; set; }
	public Texture WaterAmplitudeNormalTexture { get; set; }
	public float LastUpdateTime { get; set; }
	
	protected override void OnAwake()
	{
		InitialSpectrumTexture = Texture.CreateArray( resolution, resolution, 3)
			.WithFormat( ImageFormat.RGBA32323232F )
			.WithUAVBinding()
			.Finish();
		
		InitialConstantsTexture = Texture.CreateArray( resolution, resolution, 3)
			.WithFormat( ImageFormat.RGBA32323232F )
			.WithUAVBinding()
			.Finish();
		
		TimeBasedSpectrumTextureDzDxDyDzx = Texture.CreateArray( resolution, resolution, 3)
			.WithFormat( ImageFormat.RGBA32323232F )
			.WithUAVBinding()
			.Finish();
		
		TimeBasedSpectrumTextureDxxDyxDzyDyy = Texture.CreateArray( resolution, resolution, 3)
			.WithFormat( ImageFormat.RGBA32323232F )
			.WithUAVBinding()
			.Finish();
		
		WaterAmplitudeTexture = Texture.CreateArray( resolution, resolution, 3)
			.WithFormat( ImageFormat.RGBA32323232F )
			.WithUAVBinding()
			.Finish();
		
		WaterAmplitudeNormalTexture = Texture.CreateArray( resolution, resolution, 3)
			.WithFormat( ImageFormat.RGBA32323232F )
			.WithUAVBinding()
			.Finish();

		NoiseTexture = new GaussianNoiseGenerator( seed ).CreateNoiseTexture( resolution );
		
		CreateWaterModel();
		CalculateInitialState();
	}

	protected override void OnEnabled()
	{
		waterRenderer.SceneObject.Attributes.Set( "HeightMap", WaterAmplitudeTexture );
		waterRenderer.SceneObject.Attributes.Set( "HeightMapNormals", WaterAmplitudeNormalTexture );
	}

	protected override void OnUpdate()
	{
		if ( recalculateInitials )
		{
			CalculateInitialState();
			
			waterRenderer.SceneObject.Attributes.Set( "EnableLengthScale0", enableLengthScale0 );
			waterRenderer.SceneObject.Attributes.Set( "LengthScale0", lengthScale0 );
			waterRenderer.SceneObject.Attributes.Set( "EnableLengthScale1", enableLengthScale1 );
			waterRenderer.SceneObject.Attributes.Set( "LengthScale1", lengthScale1 );
			waterRenderer.SceneObject.Attributes.Set( "EnableLengthScale2", enableLengthScale2 );
			waterRenderer.SceneObject.Attributes.Set( "LengthScale2", lengthScale2 );
		
			waterRenderer.SceneObject.Attributes.Set( "AmbientColor", waterColor );
			waterRenderer.SceneObject.Attributes.Set( "LightColor", Sun.GetComponent<DirectionalLight>().SkyColor );
			waterRenderer.SceneObject.Attributes.Set( "SunColor", Sun.GetComponent<DirectionalLight>().LightColor );
			waterRenderer.SceneObject.Attributes.Set( "LightDirection", Sun.LocalRotation.Forward );
		
			waterRenderer.SceneObject.Attributes.Set( "DiffuseReflectance", diffuseReflectance );
			waterRenderer.SceneObject.Attributes.Set( "FresnelShininess", fresnelShininess );
			waterRenderer.SceneObject.Attributes.Set( "FresnelBias", fresnelBias );
			waterRenderer.SceneObject.Attributes.Set( "FresnelStrength", fresnelStrength );
			waterRenderer.SceneObject.Attributes.Set( "SpecularReflectance", specularReflectance );
		}

		CalculateTimeBasedPhillipsSpectrum();
		CalculateAmplitudes();


		LastUpdateTime = Time.Now;
	}

	protected override void OnDestroy()
	{
		InitialSpectrumTexture.Dispose();
		InitialConstantsTexture.Dispose();
		TimeBasedSpectrumTextureDzDxDyDzx.Dispose();
		TimeBasedSpectrumTextureDxxDyxDzyDyy.Dispose();
		WaterAmplitudeTexture.Dispose();
		WaterAmplitudeNormalTexture.Dispose();
		NoiseTexture.Dispose();
		waterRenderer.Destroy();
	}

	private void CalculateAmplitudes()
	{
		// column pass
		inverseFFTShader.Attributes.Set( "IsColumnPass", true );
		inverseFFTShader.Attributes.Set( "InputOutputTexture", TimeBasedSpectrumTextureDzDxDyDzx );
		inverseFFTShader.Dispatch( resolution, resolution, 3);
		
		inverseFFTShader.Attributes.Set( "IsColumnPass", true );
		inverseFFTShader.Attributes.Set( "InputOutputTexture", TimeBasedSpectrumTextureDxxDyxDzyDyy );
		inverseFFTShader.Dispatch( resolution, resolution, 3);
		
		// row pass
		inverseFFTShader.Attributes.Set( "IsColumnPass", false );
		inverseFFTShader.Attributes.Set( "InputOutputTexture", TimeBasedSpectrumTextureDzDxDyDzx );
		inverseFFTShader.Dispatch( resolution, resolution, 3);
		
		inverseFFTShader.Attributes.Set( "IsColumnPass", false );
		inverseFFTShader.Attributes.Set( "InputOutputTexture", TimeBasedSpectrumTextureDxxDyxDzyDyy );
		inverseFFTShader.Dispatch( resolution, resolution, 3);
		
		// post process + pack
		inverseFFTPostProcessShader.Attributes.Set( "InputTextureDzDxDyDzx", TimeBasedSpectrumTextureDzDxDyDzx );
		inverseFFTPostProcessShader.Attributes.Set( "InputTextureDxxDyxDzyDyy", TimeBasedSpectrumTextureDxxDyxDzyDyy );
		inverseFFTPostProcessShader.Attributes.Set( "OutputTexture", WaterAmplitudeTexture );
		inverseFFTPostProcessShader.Attributes.Set( "OutputNormalTexture", WaterAmplitudeNormalTexture );
		inverseFFTPostProcessShader.Attributes.Set( "Lambda", 1.0f );
		inverseFFTPostProcessShader.Attributes.Set( "FoamBias", 0.85f );
		inverseFFTPostProcessShader.Attributes.Set( "FoamDecayRate", 0.0175f );
		inverseFFTPostProcessShader.Attributes.Set( "FoamAdd", 0.1f );
		inverseFFTPostProcessShader.Attributes.Set( "FoamThreshold", 0.0f );
		inverseFFTPostProcessShader.Dispatch( resolution, resolution, 3);
	}

	private void CalculateTimeBasedPhillipsSpectrum()
	{
		timeBasedSpectrumShader.Attributes.Set( "Time", (Time.Now * timeScale) );
		timeBasedSpectrumShader.Attributes.Set( "InitialSpectrum", InitialSpectrumTexture );
		timeBasedSpectrumShader.Attributes.Set( "Constants", InitialConstantsTexture );
		timeBasedSpectrumShader.Attributes.Set( "TimeBasedSpectrumDzDxDyDzx", TimeBasedSpectrumTextureDzDxDyDzx );
		timeBasedSpectrumShader.Attributes.Set( "TimeBasedSpectrumDxxDyxDzyDyy", TimeBasedSpectrumTextureDxxDyxDzyDyy );
		timeBasedSpectrumShader.Dispatch( resolution, resolution, 3);
	}
	
	private void CalculateInitialState()
	{
		GpuBuffer<ShaderSpectrumSettings> spectrumSettingsBuffer = new(2);
		spectrumSettingsBuffer.SetData( new[]
		{
			ConvertSpectrumSettings( spectrumSettings ),
			ConvertSpectrumSettings( secondSpectrumSettings )
		} );

		float cutoff1 = 2 * MathF.PI / lengthScale1 * 6f;
		float cutoff2 = 2 * MathF.PI / lengthScale2 * 6f;
		GpuBuffer<WaterLayer> waterLayerBuffer = new(3);
		waterLayerBuffer.SetData( new[]
		{
			new WaterLayer {
				lengthScale = lengthScale0,
				cutoffLow = 0.0001f,
				cutoffHigh = cutoff1
			},
			new WaterLayer {
				lengthScale = lengthScale1,
				cutoffLow = cutoff1,
				cutoffHigh = cutoff2
			},
			new WaterLayer {
				lengthScale = lengthScale2,
				cutoffLow = cutoff2,
				cutoffHigh = 9999
			}
		} );
		
		initialSpectrumShader.Attributes.Set( "GaussianNoise", NoiseTexture );
		initialSpectrumShader.Attributes.Set( "Spectrum", InitialSpectrumTexture );
		initialSpectrumShader.Attributes.Set( "Constants", InitialConstantsTexture );
		initialSpectrumShader.Attributes.Set( "WaterLayers", waterLayerBuffer );
		initialSpectrumShader.Attributes.Set( "Size", resolution );
		initialSpectrumShader.Attributes.Set( "Depth", depth );
		initialSpectrumShader.Attributes.Set( "SpectrumParameters", spectrumSettingsBuffer );
		initialSpectrumShader.Dispatch( resolution, resolution, 3 );
		
		packSpectrumConjugateShader.Attributes.Set( "Spectrum", InitialSpectrumTexture );
		packSpectrumConjugateShader.Attributes.Set( "Size", resolution );
		packSpectrumConjugateShader.Dispatch( resolution, resolution, 3 );
	}

	private void CreateWaterModel()
	{
		
		var mesh = new Mesh(material);
		var modelBuilder = new ModelBuilder();
    
		var halfLength = planeLength * 0.5f;
		var sideVertexCount = planeResolution;
    
		var vertices = new Vertex[(sideVertexCount + 1) * (sideVertexCount + 1)];
    
		for ( int i = 0, x = 0; x <= sideVertexCount; ++x )
		{
			for ( int y = 0; y <= sideVertexCount; ++y, ++i )
			{
				var vertex = new Vertex(
					new Vector3(((float)x / sideVertexCount * planeLength) - halfLength, 
						((float)y / sideVertexCount * planeLength) - halfLength, 
						0),
					Vector3.Up,
					Vector3.Forward,
					new Vector4(
						(float)x / sideVertexCount, 
						(float)y / sideVertexCount, 
						0, 
						0)
				);
            
				vertices[i] = vertex;
			}
		}
		
		mesh.CreateVertexBuffer<Vertex>( vertices.Length, Vertex.Layout, vertices );
		
		var triangles = new int[sideVertexCount * sideVertexCount * 6];
		for ( int ti = 0, vi = 0, x = 0; x < sideVertexCount; ++vi, ++x )
		{
			for ( int y = 0; y < sideVertexCount; ti += 6, ++vi, ++y )
			{
				triangles[ti] = vi;
				triangles[ti + 1] = vi + sideVertexCount + 2;
				triangles[ti + 2] = vi + 1;
				triangles[ti + 3] = vi;
				triangles[ti + 4] = vi + sideVertexCount + 1;
				triangles[ti + 5] = vi + sideVertexCount + 2;
			}
		}
		mesh.CreateIndexBuffer(triangles.Length, triangles);

		waterRenderer.Model = modelBuilder
			.AddMesh( mesh )
			.Create();
	}

	private float JonswapAlpha(float g, float fetch, float windSpeed)
	{
		return 0.076f * MathF.Pow(g * fetch / windSpeed / windSpeed, -0.22f);
	}

	private float JonswapPeakFrequency(float g, float fetch, float windSpeed)
	{
		return 22 * MathF.Pow(windSpeed * fetch / g / g, -0.33f);
	}
	
	private ShaderSpectrumSettings ConvertSpectrumSettings(EditorSpectrumSettings settings)
	{
		return new ShaderSpectrumSettings
		{
			scale = settings.scale,
			angle = settings.windDirection / 180 * MathF.PI,
			spreadBlend = settings.spreadBlend,
			swell = Math.Clamp(settings.swell, 0.01f, 1),
			alpha = JonswapAlpha(9.81f, settings.fetch, settings.windSpeed),
			peakOmega = JonswapPeakFrequency(9.81f, settings.fetch, settings.windSpeed),
			gamma = settings.peakEnhancement,
			shortWavesFade = settings.shortWavesFade
		};
	}
}
