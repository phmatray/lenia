using System.Runtime.CompilerServices;

namespace Lenia.Client;

public class LeniaUltraFast
{
    private float[] _grid;
    private float[] _nextGrid;
    private readonly int _width;
    private readonly int _height;
    private readonly int _size;
    
    // Ultra sparse kernel - only store significant weights
    private readonly int[] _kernelOffsetsX;
    private readonly int[] _kernelOffsetsY;
    private readonly float[] _kernelWeights;
    private readonly int _kernelCount;
    
    // Precomputed growth function lookup table
    private readonly float[] _growthLookup;
    private const int LookupSize = 1024;
    
    public float R { get; set; } = 6.0f;
    public float DeltaT { get; set; } = 0.1f;
    public float Mu { get; set; } = 0.15f;
    public float Sigma { get; set; } = 0.016f;
    public float KernelAlpha { get; set; } = 4.0f;
    
    public int Width => _width;
    public int Height => _height;
    
    public LeniaUltraFast(int width, int height)
    {
        this._width = width;
        this._height = height;
        this._size = width * height;
        _grid = new float[_size];
        _nextGrid = new float[_size];
        
        // Build ultra-sparse kernel
        var kernelList = new List<(int x, int y, float weight)>();
        BuildSparseKernel(kernelList);
        
        _kernelCount = kernelList.Count;
        _kernelOffsetsX = new int[_kernelCount];
        _kernelOffsetsY = new int[_kernelCount];
        _kernelWeights = new float[_kernelCount];
        
        for (int i = 0; i < _kernelCount; i++)
        {
            _kernelOffsetsX[i] = kernelList[i].x;
            _kernelOffsetsY[i] = kernelList[i].y;
            _kernelWeights[i] = kernelList[i].weight;
        }
        
        // Precompute growth function lookup table
        _growthLookup = new float[LookupSize];
        for (int i = 0; i < LookupSize; i++)
        {
            float u = (float)i / (LookupSize - 1);
            _growthLookup[i] = ComputeGrowth(u);
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
        var result = new double[_size];
        for (int i = 0; i < _size; i++)
        {
            result[i] = _grid[i];
        }
        return result;
    }
    
    private void InitializeRandom()
    {
        var random = new Random();
        for (int i = 0; i < _size; i++)
        {
            _grid[i] = (float)random.NextDouble();
        }
    }
    
    public void InitializeCircle(int centerX, int centerY, int radius)
    {
        Array.Clear(_grid, 0, _size);
        int radiusSq = radius * radius;
        
        for (int y = Math.Max(0, centerY - radius); y <= Math.Min(_height - 1, centerY + radius); y++)
        {
            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(_width - 1, centerX + radius); x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSq)
                {
                    _grid[y * _width + x] = 1.0f;
                }
            }
        }
    }
    
    public void InitializeRing(int centerX, int centerY, int innerRadius, int outerRadius)
    {
        Array.Clear(_grid, 0, _size);
        int innerRadiusSq = innerRadius * innerRadius;
        int outerRadiusSq = outerRadius * outerRadius;
        
        for (int y = Math.Max(0, centerY - outerRadius); y <= Math.Min(_height - 1, centerY + outerRadius); y++)
        {
            for (int x = Math.Max(0, centerX - outerRadius); x <= Math.Min(_width - 1, centerX + outerRadius); x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                int distSq = dx * dx + dy * dy;
                if (distSq >= innerRadiusSq && distSq <= outerRadiusSq)
                {
                    _grid[y * _width + x] = 1.0f;
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
        InitializeCircle(_width / 2, _height / 2, 8);
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
        InitializeRing(_width / 2, _height / 2, 4, 7);
    }
    
    public void Clear()
    {
        Array.Clear(_grid, 0, _size);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        // Process in chunks for better cache locality
        int chunkSize = _height / Environment.ProcessorCount;
        
        Parallel.For(0, Environment.ProcessorCount, chunk =>
        {
            int startY = chunk * chunkSize;
            int endY = (chunk == Environment.ProcessorCount - 1) ? _height : (chunk + 1) * chunkSize;
            
            for (int y = startY; y < endY; y++)
            {
                UpdateRow(y);
            }
        });
        
        (_grid, _nextGrid) = (_nextGrid, _grid);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateRow(int y)
    {
        int rowStart = y * _width;
        
        for (int x = 0; x < _width; x++)
        {
            int index = rowStart + x;
            float potential = CalculatePotential(x, y);
            float growth = GetGrowthFromLookup(potential);
            _nextGrid[index] = Math.Max(0.0f, Math.Min(1.0f, _grid[index] + DeltaT * growth));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculatePotential(int centerX, int centerY)
    {
        float sum = 0;
        
        for (int i = 0; i < _kernelCount; i++)
        {
            int targetX = centerX + _kernelOffsetsX[i];
            int targetY = centerY + _kernelOffsetsY[i];
            
            // Fast wrapping using bitwise operations (works for power-of-2 sizes)
            if (targetX < 0) targetX += _width;
            else if (targetX >= _width) targetX -= _width;
            
            if (targetY < 0) targetY += _height;
            else if (targetY >= _height) targetY -= _height;
            
            sum += _grid[targetY * _width + targetX] * _kernelWeights[i];
        }
        
        return sum;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetGrowthFromLookup(float u)
    {
        int index = (int)(u * (LookupSize - 1));
        index = Math.Max(0, Math.Min(LookupSize - 1, index));
        return _growthLookup[index];
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