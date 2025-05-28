using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lenia.Client;

public class LeniaUltraFast
{
    private float[] grid;
    private float[] nextGrid;
    private readonly int width;
    private readonly int height;
    private readonly int size;
    
    // Ultra sparse kernel - only store significant weights
    private readonly int[] kernelOffsetsX;
    private readonly int[] kernelOffsetsY;
    private readonly float[] kernelWeights;
    private readonly int kernelCount;
    
    // Precomputed growth function lookup table
    private readonly float[] growthLookup;
    private const int LOOKUP_SIZE = 1024;
    
    public float R { get; set; } = 6.0f;
    public float DeltaT { get; set; } = 0.1f;
    public float Mu { get; set; } = 0.15f;
    public float Sigma { get; set; } = 0.016f;
    public float KernelAlpha { get; set; } = 4.0f;
    
    public int Width => width;
    public int Height => height;
    
    public LeniaUltraFast(int width, int height)
    {
        this.width = width;
        this.height = height;
        this.size = width * height;
        grid = new float[size];
        nextGrid = new float[size];
        
        // Build ultra-sparse kernel
        var kernelList = new List<(int x, int y, float weight)>();
        BuildSparseKernel(kernelList);
        
        kernelCount = kernelList.Count;
        kernelOffsetsX = new int[kernelCount];
        kernelOffsetsY = new int[kernelCount];
        kernelWeights = new float[kernelCount];
        
        for (int i = 0; i < kernelCount; i++)
        {
            kernelOffsetsX[i] = kernelList[i].x;
            kernelOffsetsY[i] = kernelList[i].y;
            kernelWeights[i] = kernelList[i].weight;
        }
        
        // Precompute growth function lookup table
        growthLookup = new float[LOOKUP_SIZE];
        for (int i = 0; i < LOOKUP_SIZE; i++)
        {
            float u = (float)i / (LOOKUP_SIZE - 1);
            growthLookup[i] = ComputeGrowth(u);
        }
        
        InitializeRandom();
    }
    
    private void BuildSparseKernel(List<(int x, int y, float weight)> kernelList)
    {
        var kernelRadius = (int)MathF.Ceiling(R);
        float kernelSum = 0;
        
        // Only include kernel points with significant weight
        for (int dy = -kernelRadius; dy <= kernelRadius; dy++)
        {
            for (int dx = -kernelRadius; dx <= kernelRadius; dx++)
            {
                float distance = MathF.Sqrt(dx * dx + dy * dy);
                if (distance > R || distance < 0.5f) continue;
                
                float weight = KernelFunction(distance / R);
                if (weight > 0.01f) // Only significant weights
                {
                    kernelList.Add((dx, dy, weight));
                    kernelSum += weight;
                }
            }
        }
        
        // Normalize
        for (int i = 0; i < kernelList.Count; i++)
        {
            var item = kernelList[i];
            kernelList[i] = (item.x, item.y, item.weight / kernelSum);
        }
    }
    
    public double[] GetGrid()
    {
        var result = new double[size];
        for (int i = 0; i < size; i++)
        {
            result[i] = grid[i];
        }
        return result;
    }
    
    private void InitializeRandom()
    {
        var random = new Random();
        for (int i = 0; i < size; i++)
        {
            grid[i] = (float)random.NextDouble();
        }
    }
    
    public void InitializeCircle(int centerX, int centerY, int radius)
    {
        Array.Clear(grid, 0, size);
        int radiusSq = radius * radius;
        
        for (int y = Math.Max(0, centerY - radius); y <= Math.Min(height - 1, centerY + radius); y++)
        {
            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(width - 1, centerX + radius); x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSq)
                {
                    grid[y * width + x] = 1.0f;
                }
            }
        }
    }
    
    public void InitializeRing(int centerX, int centerY, int innerRadius, int outerRadius)
    {
        Array.Clear(grid, 0, size);
        int innerRadiusSq = innerRadius * innerRadius;
        int outerRadiusSq = outerRadius * outerRadius;
        
        for (int y = Math.Max(0, centerY - outerRadius); y <= Math.Min(height - 1, centerY + outerRadius); y++)
        {
            for (int x = Math.Max(0, centerX - outerRadius); x <= Math.Min(width - 1, centerX + outerRadius); x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                int distSq = dx * dx + dy * dy;
                if (distSq >= innerRadiusSq && distSq <= outerRadiusSq)
                {
                    grid[y * width + x] = 1.0f;
                }
            }
        }
    }
    
    public void InitializeOrbium()
    {
        R = 6.0f;
        DeltaT = 0.1f;
        Mu = 0.15f;
        Sigma = 0.016f;
        KernelAlpha = 4.0f;
        
        var kernelList = new List<(int x, int y, float weight)>();
        BuildSparseKernel(kernelList);
        InitializeCircle(width / 2, height / 2, 8);
    }
    
    public void InitializeGeminium()
    {
        R = 5.0f;
        DeltaT = 0.1f;
        Mu = 0.14f;
        Sigma = 0.014f;
        KernelAlpha = 4.0f;
        
        var kernelList = new List<(int x, int y, float weight)>();
        BuildSparseKernel(kernelList);
        InitializeRing(width / 2, height / 2, 4, 7);
    }
    
    public void Clear()
    {
        Array.Clear(grid, 0, size);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        // Process in chunks for better cache locality
        int chunkSize = height / Environment.ProcessorCount;
        
        Parallel.For(0, Environment.ProcessorCount, chunk =>
        {
            int startY = chunk * chunkSize;
            int endY = (chunk == Environment.ProcessorCount - 1) ? height : (chunk + 1) * chunkSize;
            
            for (int y = startY; y < endY; y++)
            {
                UpdateRow(y);
            }
        });
        
        (grid, nextGrid) = (nextGrid, grid);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateRow(int y)
    {
        int rowStart = y * width;
        
        for (int x = 0; x < width; x++)
        {
            int index = rowStart + x;
            float potential = CalculatePotential(x, y);
            float growth = GetGrowthFromLookup(potential);
            nextGrid[index] = Math.Max(0.0f, Math.Min(1.0f, grid[index] + DeltaT * growth));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculatePotential(int centerX, int centerY)
    {
        float sum = 0;
        
        for (int i = 0; i < kernelCount; i++)
        {
            int targetX = centerX + kernelOffsetsX[i];
            int targetY = centerY + kernelOffsetsY[i];
            
            // Fast wrapping using bitwise operations (works for power-of-2 sizes)
            if (targetX < 0) targetX += width;
            else if (targetX >= width) targetX -= width;
            
            if (targetY < 0) targetY += height;
            else if (targetY >= height) targetY -= height;
            
            sum += grid[targetY * width + targetX] * kernelWeights[i];
        }
        
        return sum;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetGrowthFromLookup(float u)
    {
        int index = (int)(u * (LOOKUP_SIZE - 1));
        index = Math.Max(0, Math.Min(LOOKUP_SIZE - 1, index));
        return growthLookup[index];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float KernelFunction(float r)
    {
        if (r >= 1.0f || r <= 0.0f) return 0.0f;
        return MathF.Exp(KernelAlpha - KernelAlpha / (4.0f * r * (1.0f - r)));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ComputeGrowth(float u)
    {
        float z = (u - Mu) / Sigma;
        return 2.0f * MathF.Exp(-0.5f * z * z) - 1.0f;
    }
}