using System.Runtime.CompilerServices;

namespace Lenia.Client;

public class LeniaCoreOptimized
{
    private double[] _grid;
    private double[] _nextGrid;
    private readonly int _width;
    private readonly int _height;
    private readonly int _size;
    
    private readonly int[] _kernelX;
    private readonly int[] _kernelY;
    private readonly double[] _kernelWeights;
    private readonly int _kernelCount;
    
    public double R { get; set; } = 10.0;
    public double DeltaT { get; set; } = 0.1;
    public double Mu { get; set; } = 0.15;
    public double Sigma { get; set; } = 0.016;
    public double KernelAlpha { get; set; } = 4.0;
    
    public int Width => _width;
    public int Height => _height;
    
    public LeniaCoreOptimized(int width, int height)
    {
        this._width = width;
        this._height = height;
        this._size = width * height;
        _grid = new double[_size];
        _nextGrid = new double[_size];
        
        var kernelList = new List<(int x, int y, double weight)>();
        BuildKernel(kernelList);
        
        _kernelCount = kernelList.Count;
        _kernelX = new int[_kernelCount];
        _kernelY = new int[_kernelCount];
        _kernelWeights = new double[_kernelCount];
        
        for (int i = 0; i < _kernelCount; i++)
        {
            _kernelX[i] = kernelList[i].x;
            _kernelY[i] = kernelList[i].y;
            _kernelWeights[i] = kernelList[i].weight;
        }
        
        InitializeRandom();
    }
    
    private void BuildKernel(List<(int x, int y, double weight)> kernelList)
    {
        var kernelRadius = (int)Math.Ceiling(R);
        double kernelSum = 0;
        
        for (int dy = -kernelRadius; dy <= kernelRadius; dy++)
        {
            for (int dx = -kernelRadius; dx <= kernelRadius; dx++)
            {
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > R || distance <= 0.1) continue;
                
                var weight = KernelFunction(distance / R);
                if (weight > 1e-8)
                {
                    kernelList.Add((dx, dy, weight));
                    kernelSum += weight;
                }
            }
        }
        
        for (int i = 0; i < kernelList.Count; i++)
        {
            var item = kernelList[i];
            kernelList[i] = (item.x, item.y, item.weight / kernelSum);
        }
    }
    
    public double[] GetGrid() => _grid;
    
    private void InitializeRandom()
    {
        var random = new Random();
        for (int i = 0; i < _size; i++)
        {
            _grid[i] = random.NextDouble();
        }
    }
    
    public void InitializeCircle(int centerX, int centerY, int radius)
    {
        Array.Clear(_grid, 0, _size);
        for (int y = Math.Max(0, centerY - radius); y <= Math.Min(_height - 1, centerY + radius); y++)
        {
            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(_width - 1, centerX + radius); x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    _grid[y * _width + x] = 1.0;
                }
            }
        }
    }
    
    public void InitializeRing(int centerX, int centerY, int innerRadius, int outerRadius)
    {
        Array.Clear(_grid, 0, _size);
        for (int y = Math.Max(0, centerY - outerRadius); y <= Math.Min(_height - 1, centerY + outerRadius); y++)
        {
            for (int x = Math.Max(0, centerX - outerRadius); x <= Math.Min(_width - 1, centerX + outerRadius); x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distSq = dx * dx + dy * dy;
                if (distSq >= innerRadius * innerRadius && distSq <= outerRadius * outerRadius)
                {
                    _grid[y * _width + x] = 1.0;
                }
            }
        }
    }
    
    public void InitializeOrbium()
    {
        R = 10.0;
        DeltaT = 0.1;
        Mu = 0.15;
        Sigma = 0.016;
        KernelAlpha = 4.0;
        
        var kernelList = new List<(int x, int y, double weight)>();
        BuildKernel(kernelList);
        InitializeCircle(_width / 2, _height / 2, 12);
    }
    
    public void InitializeGeminium()
    {
        R = 8.0;
        DeltaT = 0.1;
        Mu = 0.14;
        Sigma = 0.014;
        KernelAlpha = 4.0;
        
        var kernelList = new List<(int x, int y, double weight)>();
        BuildKernel(kernelList);
        InitializeRing(_width / 2, _height / 2, 6, 10);
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
        var rowStart = y * _width;
        
        for (int x = 0; x < _width; x++)
        {
            var index = rowStart + x;
            var potential = CalculatePotentialFast(x, y);
            var growth = GrowthFunction(potential);
            _nextGrid[index] = Math.Max(0.0, Math.Min(1.0, _grid[index] + DeltaT * growth));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculatePotentialFast(int centerX, int centerY)
    {
        double sum = 0;
        
        for (int i = 0; i < _kernelCount; i++)
        {
            var targetX = centerX + _kernelX[i];
            var targetY = centerY + _kernelY[i];
            
            if (targetX < 0) targetX += _width;
            else if (targetX >= _width) targetX -= _width;
            
            if (targetY < 0) targetY += _height;
            else if (targetY >= _height) targetY -= _height;
            
            var targetIndex = targetY * _width + targetX;
            sum += _grid[targetIndex] * _kernelWeights[i];
        }
        
        return sum;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double KernelFunction(double r)
    {
        if (r >= 1.0 || r <= 0.0) return 0.0;
        return Math.Exp(KernelAlpha - KernelAlpha / (4.0 * r * (1.0 - r)));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GrowthFunction(double u)
    {
        var z = (u - Mu) / Sigma;
        return 2.0 * Math.Exp(-0.5 * z * z) - 1.0;
    }
}