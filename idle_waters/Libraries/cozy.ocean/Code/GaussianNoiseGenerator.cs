using System;

namespace Sandbox;

public class GaussianNoiseGenerator
{
    private Random random;

    public GaussianNoiseGenerator(int? seed = null)
    {
        random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public Texture CreateNoiseTexture(int resolution)
    {
        // Calculate the total number of floats needed (4 per pixel for RGBA format)
        int totalFloats = resolution * resolution * 4;
        float[] noiseData = new float[totalFloats];

        // Generate noise for each pixel
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIndex = (y * resolution + x) * 4;

                // Generate two pairs of independent Gaussian values
                var (z0, z1) = GenerateGaussianPair();
                var (z2, z3) = GenerateGaussianPair();

                // Store the values directly without clamping
                noiseData[pixelIndex] = z0;     // R channel
                noiseData[pixelIndex + 1] = z1; // G channel
                noiseData[pixelIndex + 2] = z2; // B channel
                noiseData[pixelIndex + 3] = z3; // A channel
            }
        }

        // Convert float array to byte array for the texture builder
        byte[] byteData = new byte[totalFloats * sizeof(float)];
        Buffer.BlockCopy(noiseData, 0, byteData, 0, byteData.Length);

        return Texture.Create(resolution, resolution)
            .WithUAVBinding()
            .WithFormat(ImageFormat.RGBA32323232F)
            .WithData(byteData, byteData.Length)
            .Finish();
    }

    /// <summary>
    /// Generates a pair of independent Gaussian-distributed random values using the Box-Muller transform
    /// </summary>
    private (float z0, float z1) GenerateGaussianPair()
    {
        // Ensure u1 is never exactly 0 to avoid log(0)
        float u1 = Math.Max(float.Epsilon, (float)random.NextDouble());
        float u2 = (float)random.NextDouble();
        
        float magnitude = (float)Math.Sqrt(-2.0f * Math.Log(u1));
        float angle = (float)(2.0f * Math.PI * u2);
        
        // Generate two independent standard normal variables
        float z0 = magnitude * (float)Math.Cos(angle);
        float z1 = magnitude * (float)Math.Sin(angle);
        
        return (z0, z1);
    }
}
