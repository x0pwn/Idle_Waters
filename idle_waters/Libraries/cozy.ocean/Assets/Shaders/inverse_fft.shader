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

    bool IsColumnPass < Attribute( "IsColumnPass" ); >;
    RWTexture2DArray<float4> InputOutputTexture < Attribute( "InputOutputTexture" ); >;

    // Constants
    #define TWO_PI 6.28318530718
    #define SIZE 256
    #define LOG2_SIZE 8

    // used to store intermediate results of stages
    groupshared float4 intermediateBuffer[SIZE];

    float4 ComplexMultiply(float4 a, float4 b)
    {
        // we're packing two complex multiplications to do 2 FFTs at once, 
        // ex:  a = (1 + 2i), (2 - 3i)
        //      b = (2 + 4i), (1 + 4i)
        // result = (1 + 2i) * (2 + 4i), (2 - 3i) * (1 + 4i)
        return float4(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x, a.z * b.z - a.w * b.w, a.z * b.w + a.w * b.z);
    }

    // Compute twiddle factor for inverse FFT
    // For inverse FFT, we use positive exponent: e^(2πik/N)
    float4 GetTwiddleFactor(uint k, uint N) {
        float angle = (TWO_PI * k) / N;
        float real = cos(angle);
        float imaginary = sin(angle);

        // packing the twiddle factor in twice since we are doing 2 FFTs at once
        return float4(real, imaginary, real, imaginary);
    }

    [numthreads( SIZE, 1, 1 )]
    void MainCs( uint3 id : SV_DispatchThreadID )
    {
        uint threadIndex = id.x;
        uint2 inputIndex;
        if (IsColumnPass) {
            inputIndex = id.yx;
        } else {
            inputIndex = id.xy;
        }
        
        // Load data into the shared intermediate buffer
        intermediateBuffer[threadIndex] = InputOutputTexture[uint3(inputIndex, id.z)];
        GroupMemoryBarrierWithGroupSync();

        /*
            Bit-reverse the indices.
            The threadIndex < reversedIndex conditional ensures that we don't double swap, for example:

            Thread 1 sees: threadIndex = 1, reversedIndex = 4
            - Swaps elements 1 and 4

            Thread 4 sees: threadIndex = 4, reversedIndex = 1
            - Would swap elements 4 and 1, which would undo our first swap
        */
        uint reversedIndex = reversebits(threadIndex) >> (32 - LOG2_SIZE) & (SIZE - 1);
        if (threadIndex < reversedIndex) {
            float4 temp = intermediateBuffer[threadIndex];
            intermediateBuffer[threadIndex] = intermediateBuffer[reversedIndex];
            intermediateBuffer[reversedIndex] = temp;
        }
        GroupMemoryBarrierWithGroupSync();

        for (uint stage = 0; stage < LOG2_SIZE; stage++) {
            uint butterflySize = (uint) 2 << stage;      // Size of each butterfly
            uint halfButterflySize = butterflySize / 2;  // Distance between paired elements
            
            // Position within current butterfly
            uint butterflyId = threadIndex / butterflySize;
            uint butterflyOffset = threadIndex % butterflySize;
            
            // Only threads in first half of butterfly do computation, since they calculate both the top and bottom
            if (butterflyOffset < halfButterflySize) {
                uint i = butterflyId * butterflySize + butterflyOffset;
                uint j = i + halfButterflySize;
                float4 twiddle = GetTwiddleFactor(butterflyOffset * (SIZE / butterflySize), SIZE);
                
                float4 a = intermediateBuffer[i];
                float4 b = ComplexMultiply(intermediateBuffer[j], twiddle);

                intermediateBuffer[i] = a + b;
                intermediateBuffer[j] = a - b;
            }
            GroupMemoryBarrierWithGroupSync();
        }

        InputOutputTexture[uint3(inputIndex, id.z)] = intermediateBuffer[threadIndex];
    }    
}