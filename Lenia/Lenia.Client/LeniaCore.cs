namespace Lenia.Client;

public class LeniaCore
{
    private double[] _grid;
    private double[] _nextGrid;
    private readonly int _width;
    private readonly int _height;
    private readonly int _size;
    
    private double[] _kernelWeights;
    private int[] _kernelOffsets;
    private int _kernelSize;
    private double _kernelSum;
    
    public double R { get; set; } = 13.0;
    public double DeltaT { get; set; } = 0.1;
    public double Mu { get; set; } = 0.15;
    public double Sigma { get; set; } = 0.017;
    public double KernelAlpha { get; set; } = 4.0;
    
    public int Width => _width;
    public int Height => _height;
    
    public LeniaCore(int width, int height)
    {
        this._width = width;
        this._height = height;
        this._size = width * height;
        _grid = new double[_size];
        _nextGrid = new double[_size];
        
        PrecomputeKernel();
        InitializeRandom();
    }
    
    public double[] GetGrid() => _grid;
    
    private void PrecomputeKernel()
    {
        var kernelRadius = (int)Math.Ceiling(R);
        var tempKernel = new List<(int offset, double weight)>();
        _kernelSum = 0;
        
        for (int dx = -kernelRadius; dx <= kernelRadius; dx++)
        {
            for (int dy = -kernelRadius; dy <= kernelRadius; dy++)
            {
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > R) continue;
                
                var weight = KernelFunction(distance / R);
                if (weight > 1e-10)
                {
                    var offset = dy * _width + dx;
                    tempKernel.Add((offset, weight));
                    _kernelSum += weight;
                }
            }
        }
        
        _kernelSize = tempKernel.Count;
        _kernelOffsets = new int[_kernelSize];
        _kernelWeights = new double[_kernelSize];
        
        for (int i = 0; i < _kernelSize; i++)
        {
            _kernelOffsets[i] = tempKernel[i].offset;
            _kernelWeights[i] = tempKernel[i].weight / _kernelSum;
        }
    }
    
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
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                _grid[y * _width + x] = distance <= radius ? 1.0 : 0.0;
            }
        }
    }
    
    public void InitializeRing(int centerX, int centerY, int innerRadius, int outerRadius)
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                _grid[y * _width + x] = (distance >= innerRadius && distance <= outerRadius) ? 1.0 : 0.0;
            }
        }
    }
    
    public void InitializeOrbium()
    {
        R = 13.0;
        DeltaT = 0.1;
        Mu = 0.15;
        Sigma = 0.016;
        KernelAlpha = 4.0;
        
        PrecomputeKernel();
        Clear();
        InitializeCircle(_width / 2, _height / 2, 16);
    }
    
    public void InitializeGeminium()
    {
        R = 10.0;
        DeltaT = 0.1;
        Mu = 0.14;
        Sigma = 0.014;
        KernelAlpha = 4.0;
        
        PrecomputeKernel();
        Clear();
        InitializeRing(_width / 2, _height / 2, 8, 12);
    }
    
    public void Clear()
    {
        Array.Clear(_grid, 0, _grid.Length);
    }
    
    public void Update()
    {
        Parallel.For(0, _height, y =>
        {
            for (int x = 0; x < _width; x++)
            {
                var index = y * _width + x;
                var potential = CalculatePotentialOptimized(x, y);
                var growth = GrowthFunction(potential);
                _nextGrid[index] = Math.Max(0, Math.Min(1, _grid[index] + DeltaT * growth));
            }
        });
        
        (_grid, _nextGrid) = (_nextGrid, _grid);
    }
    
    private double CalculatePotentialOptimized(int centerX, int centerY)
    {
        double sum = 0;
        var centerIndex = centerY * _width + centerX;
        
        for (int i = 0; i < _kernelSize; i++)
        {
            var offset = _kernelOffsets[i];
            var targetX = centerX + (offset % _width);
            var targetY = centerY + (offset / _width);
            
            if (targetX < 0) targetX += _width;
            if (targetX >= _width) targetX -= _width;
            if (targetY < 0) targetY += _height;
            if (targetY >= _height) targetY -= _height;
            
            var targetIndex = targetY * _width + targetX;
            sum += _grid[targetIndex] * _kernelWeights[i];
        }
        
        return sum;
    }
    
    private double KernelFunction(double r)
    {
        if (r >= 1 || r <= 0) return 0;
        return Math.Exp(KernelAlpha - KernelAlpha / (4 * r * (1 - r)));
    }
    
    private double GrowthFunction(double u)
    {
        var z = (u - Mu) / Sigma;
        return 2 * Math.Exp(-z * z / 2) - 1;
    }
}