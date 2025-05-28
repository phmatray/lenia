using System.Runtime.CompilerServices;

namespace Lenia.Client;

public class LeniaMinimal
{
    private float[] _grid;
    private float[] _nextGrid;
    private readonly int _width;
    private readonly int _height;
    private readonly int _size;
    
    // Minimal kernel - only nearest neighbors
    private static readonly (int x, int y, float weight)[] Kernel = new[]
    {
        (0, 0, 0.4f),
        (-1, 0, 0.1f), (1, 0, 0.1f), (0, -1, 0.1f), (0, 1, 0.1f),
        (-1, -1, 0.05f), (1, -1, 0.05f), (-1, 1, 0.05f), (1, 1, 0.05f),
        (-2, 0, 0.025f), (2, 0, 0.025f), (0, -2, 0.025f), (0, 2, 0.025f)
    };
    
    public float R { get; set; } = 4.0f;
    public float DeltaT { get; set; } = 0.1f;
    public float Mu { get; set; } = 0.15f;
    public float Sigma { get; set; } = 0.016f;
    public float KernelAlpha { get; set; } = 4.0f;
    
    public int Width => _width;
    public int Height => _height;
    
    public LeniaMinimal(int width, int height)
    {
        this._width = width;
        this._height = height;
        this._size = width * height;
        _grid = new float[_size];
        _nextGrid = new float[_size];
        InitializeRandom();
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
        InitializeCircle(_width / 2, _height / 2, 6);
    }
    
    public void InitializeGeminium()
    {
        InitializeRing(_width / 2, _height / 2, 3, 5);
    }
    
    public void Clear()
    {
        Array.Clear(_grid, 0, _size);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        Parallel.For(0, _height, y =>
        {
            UpdateRow(y);
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
            float growth = GrowthFunction(potential);
            _nextGrid[index] = Math.Max(0.0f, Math.Min(1.0f, _grid[index] + DeltaT * growth));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculatePotential(int centerX, int centerY)
    {
        float sum = 0;
        
        for (int i = 0; i < Kernel.Length; i++)
        {
            var (dx, dy, weight) = Kernel[i];
            int targetX = centerX + dx;
            int targetY = centerY + dy;
            
            // Fast wrapping
            if (targetX < 0) targetX += _width;
            else if (targetX >= _width) targetX -= _width;
            
            if (targetY < 0) targetY += _height;
            else if (targetY >= _height) targetY -= _height;
            
            sum += _grid[targetY * _width + targetX] * weight;
        }
        
        return sum;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GrowthFunction(float u)
    {
        float z = (u - Mu) / Sigma;
        return 2.0f * MathF.Exp(-0.5f * z * z) - 1.0f;
    }
}