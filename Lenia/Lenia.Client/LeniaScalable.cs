using System.Runtime.CompilerServices;

namespace Lenia.Client;

public class LeniaScalable
{
    private float[] _grid;
    private float[] _nextGrid;
    private int _width;
    private int _height;
    private int _size;
    
    // Performance monitoring
    private double _lastUpdateTime;
    private int _targetFps = 60;
    private double _targetFrameTime;
    
    // Adaptive processing
    private int _processingQuality = 100; // Percentage of cells to process each frame
    private int _cellSkip = 1; // Process every N cells
    private bool _useAdaptiveQuality = true;
    
    // Chunked processing for large grids
    private int _chunksPerFrame = 1;
    private int _currentChunk;
    
    // Minimal kernel for maximum speed
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
    public int ProcessingQuality => _processingQuality;
    public bool UseAdaptiveQuality 
    { 
        get => _useAdaptiveQuality; 
        set => _useAdaptiveQuality = value; 
    }
    
    public LeniaScalable(int width, int height, int targetFps = 60)
    {
        this._targetFps = targetFps;
        this._targetFrameTime = 1000.0 / targetFps; // ms per frame
        ResizeGrid(width, height);
    }
    
    public void ResizeGrid(int newWidth, int newHeight)
    {
        _width = newWidth;
        _height = newHeight;
        _size = _width * _height;
        
        var oldGrid = _grid;
        _grid = new float[_size];
        _nextGrid = new float[_size];
        
        // Copy old data if resizing
        if (oldGrid != null)
        {
            int copyWidth = Math.Min(_width, (int)Math.Sqrt(oldGrid.Length));
            int copyHeight = Math.Min(_height, oldGrid.Length / copyWidth);
            
            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    if (y * copyWidth + x < oldGrid.Length)
                    {
                        _grid[y * _width + x] = oldGrid[y * copyWidth + x];
                    }
                }
            }
        }
        
        // Adjust processing parameters based on grid size
        AdaptProcessingToGridSize();
    }
    
    private void AdaptProcessingToGridSize()
    {
        int totalCells = _size;
        
        if (totalCells <= 576) // 24x24
        {
            _processingQuality = 100;
            _cellSkip = 1;
            _chunksPerFrame = 1;
        }
        else if (totalCells <= 1600) // 40x40
        {
            _processingQuality = 80;
            _cellSkip = 1;
            _chunksPerFrame = 2;
        }
        else if (totalCells <= 4096) // 64x64
        {
            _processingQuality = 60;
            _cellSkip = 2;
            _chunksPerFrame = 4;
        }
        else if (totalCells <= 10000) // 100x100
        {
            _processingQuality = 40;
            _cellSkip = 3;
            _chunksPerFrame = 8;
        }
        else // Larger grids
        {
            _processingQuality = 25;
            _cellSkip = 4;
            _chunksPerFrame = 16;
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
        int radius = Math.Max(4, Math.Min(_width, _height) / 4);
        InitializeCircle(_width / 2, _height / 2, radius);
    }
    
    public void InitializeGeminium()
    {
        int outerRadius = Math.Max(6, Math.Min(_width, _height) / 3);
        int innerRadius = outerRadius * 2 / 3;
        InitializeRing(_width / 2, _height / 2, innerRadius, outerRadius);
    }
    
    public void Clear()
    {
        Array.Clear(_grid, 0, _size);
    }
    
    public void InitializeRandom(float density)
    {
        var rand = new Random();
        for (int i = 0; i < _size; i++)
        {
            _grid[i] = rand.NextSingle() < density ? rand.NextSingle() : 0f;
        }
    }
    
    
    public void InitializeCross(int cx, int cy, int size, int thickness)
    {
        // Horizontal bar
        for (int x = cx - size / 2; x <= cx + size / 2; x++)
        {
            for (int y = cy - thickness / 2; y <= cy + thickness / 2; y++)
            {
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    float distFromCenter = Math.Abs(y - cy) / (float)(thickness / 2);
                    float value = 1.0f - distFromCenter * distFromCenter;
                    _grid[y * _width + x] = value;
                }
            }
        }
        
        // Vertical bar
        for (int y = cy - size / 2; y <= cy + size / 2; y++)
        {
            for (int x = cx - thickness / 2; x <= cx + thickness / 2; x++)
            {
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    float distFromCenter = Math.Abs(x - cx) / (float)(thickness / 2);
                    float value = 1.0f - distFromCenter * distFromCenter;
                    _grid[y * _width + x] = Math.Max(_grid[y * _width + x], value);
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        if (_chunksPerFrame > 1)
        {
            UpdateChunked();
        }
        else
        {
            UpdateComplete();
        }
        
        (_grid, _nextGrid) = (_nextGrid, _grid);
        
        sw.Stop();
        _lastUpdateTime = sw.Elapsed.TotalMilliseconds;
        
        // Adaptive quality adjustment
        if (_useAdaptiveQuality)
        {
            AdaptQualityBasedOnPerformance();
        }
    }
    
    private void UpdateComplete()
    {
        if (_cellSkip == 1)
        {
            Parallel.For(0, _height, y => UpdateRow(y));
        }
        else
        {
            Parallel.For(0, _height / _cellSkip, y => UpdateRowSkipped(y * _cellSkip));
        }
    }
    
    private void UpdateChunked()
    {
        int chunkSize = _height / _chunksPerFrame;
        int startY = _currentChunk * chunkSize;
        int endY = Math.Min(_height, (_currentChunk + 1) * chunkSize);
        
        Parallel.For(startY, endY, y => UpdateRow(y));
        
        _currentChunk = (_currentChunk + 1) % _chunksPerFrame;
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
    private void UpdateRowSkipped(int y)
    {
        int rowStart = y * _width;
        
        for (int x = 0; x < _width; x += _cellSkip)
        {
            int index = rowStart + x;
            float potential = CalculatePotential(x, y);
            float growth = GrowthFunction(potential);
            _nextGrid[index] = Math.Max(0.0f, Math.Min(1.0f, _grid[index] + DeltaT * growth));
            
            // Fill skipped cells with interpolated values
            if (_cellSkip > 1 && x + _cellSkip < _width)
            {
                float nextValue = _nextGrid[index];
                for (int skip = 1; skip < _cellSkip && x + skip < _width; skip++)
                {
                    _nextGrid[rowStart + x + skip] = nextValue;
                }
            }
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
    
    private void AdaptQualityBasedOnPerformance()
    {
        double targetTime = _targetFrameTime * 0.7; // Use 70% of frame time for update
        
        if (_lastUpdateTime > targetTime)
        {
            // Too slow, reduce quality
            if (_processingQuality > 25)
            {
                _processingQuality = Math.Max(25, _processingQuality - 10);
                _cellSkip = Math.Min(4, _cellSkip + 1);
                _chunksPerFrame = Math.Min(16, _chunksPerFrame * 2);
            }
        }
        else if (_lastUpdateTime < targetTime * 0.5 && _processingQuality < 100)
        {
            // Fast enough, increase quality
            _processingQuality = Math.Min(100, _processingQuality + 5);
            if (_processingQuality > 75)
            {
                _cellSkip = Math.Max(1, _cellSkip - 1);
                _chunksPerFrame = Math.Max(1, _chunksPerFrame / 2);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GrowthFunction(float u)
    {
        float z = (u - Mu) / Sigma;
        return 2.0f * MathF.Exp(-0.5f * z * z) - 1.0f;
    }
    
    public void SetTargetFps(int fps)
    {
        _targetFps = fps;
        _targetFrameTime = 1000.0 / fps;
    }
}