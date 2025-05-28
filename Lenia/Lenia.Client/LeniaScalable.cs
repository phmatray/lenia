using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lenia.Client;

public class LeniaScalable
{
    private float[] grid;
    private float[] nextGrid;
    private int width;
    private int height;
    private int size;
    
    // Performance monitoring
    private double lastUpdateTime = 0;
    private int targetFps = 60;
    private double targetFrameTime;
    
    // Adaptive processing
    private int processingQuality = 100; // Percentage of cells to process each frame
    private int cellSkip = 1; // Process every N cells
    private bool useAdaptiveQuality = true;
    
    // Chunked processing for large grids
    private int chunksPerFrame = 1;
    private int currentChunk = 0;
    
    // Minimal kernel for maximum speed
    private static readonly (int x, int y, float weight)[] KERNEL = new[]
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
    
    public int Width => width;
    public int Height => height;
    public int ProcessingQuality => processingQuality;
    public bool UseAdaptiveQuality 
    { 
        get => useAdaptiveQuality; 
        set => useAdaptiveQuality = value; 
    }
    
    public LeniaScalable(int width, int height, int targetFps = 60)
    {
        this.targetFps = targetFps;
        this.targetFrameTime = 1000.0 / targetFps; // ms per frame
        ResizeGrid(width, height);
    }
    
    public void ResizeGrid(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
        size = width * height;
        
        var oldGrid = grid;
        grid = new float[size];
        nextGrid = new float[size];
        
        // Copy old data if resizing
        if (oldGrid != null)
        {
            int copyWidth = Math.Min(width, (int)Math.Sqrt(oldGrid.Length));
            int copyHeight = Math.Min(height, oldGrid.Length / copyWidth);
            
            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    if (y * copyWidth + x < oldGrid.Length)
                    {
                        grid[y * width + x] = oldGrid[y * copyWidth + x];
                    }
                }
            }
        }
        
        // Adjust processing parameters based on grid size
        AdaptProcessingToGridSize();
    }
    
    private void AdaptProcessingToGridSize()
    {
        int totalCells = size;
        
        if (totalCells <= 576) // 24x24
        {
            processingQuality = 100;
            cellSkip = 1;
            chunksPerFrame = 1;
        }
        else if (totalCells <= 1600) // 40x40
        {
            processingQuality = 80;
            cellSkip = 1;
            chunksPerFrame = 2;
        }
        else if (totalCells <= 4096) // 64x64
        {
            processingQuality = 60;
            cellSkip = 2;
            chunksPerFrame = 4;
        }
        else if (totalCells <= 10000) // 100x100
        {
            processingQuality = 40;
            cellSkip = 3;
            chunksPerFrame = 8;
        }
        else // Larger grids
        {
            processingQuality = 25;
            cellSkip = 4;
            chunksPerFrame = 16;
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
        int radius = Math.Max(4, Math.Min(width, height) / 4);
        InitializeCircle(width / 2, height / 2, radius);
    }
    
    public void InitializeGeminium()
    {
        int outerRadius = Math.Max(6, Math.Min(width, height) / 3);
        int innerRadius = outerRadius * 2 / 3;
        InitializeRing(width / 2, height / 2, innerRadius, outerRadius);
    }
    
    public void Clear()
    {
        Array.Clear(grid, 0, size);
    }
    
    public void InitializeRandom(float density)
    {
        var rand = new Random();
        for (int i = 0; i < size; i++)
        {
            grid[i] = rand.NextSingle() < density ? rand.NextSingle() : 0f;
        }
    }
    
    
    public void InitializeCross(int cx, int cy, int size, int thickness)
    {
        // Horizontal bar
        for (int x = cx - size / 2; x <= cx + size / 2; x++)
        {
            for (int y = cy - thickness / 2; y <= cy + thickness / 2; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    float distFromCenter = Math.Abs(y - cy) / (float)(thickness / 2);
                    float value = 1.0f - distFromCenter * distFromCenter;
                    grid[y * width + x] = value;
                }
            }
        }
        
        // Vertical bar
        for (int y = cy - size / 2; y <= cy + size / 2; y++)
        {
            for (int x = cx - thickness / 2; x <= cx + thickness / 2; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    float distFromCenter = Math.Abs(x - cx) / (float)(thickness / 2);
                    float value = 1.0f - distFromCenter * distFromCenter;
                    grid[y * width + x] = Math.Max(grid[y * width + x], value);
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        if (chunksPerFrame > 1)
        {
            UpdateChunked();
        }
        else
        {
            UpdateComplete();
        }
        
        (grid, nextGrid) = (nextGrid, grid);
        
        sw.Stop();
        lastUpdateTime = sw.Elapsed.TotalMilliseconds;
        
        // Adaptive quality adjustment
        if (useAdaptiveQuality)
        {
            AdaptQualityBasedOnPerformance();
        }
    }
    
    private void UpdateComplete()
    {
        if (cellSkip == 1)
        {
            Parallel.For(0, height, y => UpdateRow(y));
        }
        else
        {
            Parallel.For(0, height / cellSkip, y => UpdateRowSkipped(y * cellSkip));
        }
    }
    
    private void UpdateChunked()
    {
        int chunkSize = height / chunksPerFrame;
        int startY = currentChunk * chunkSize;
        int endY = Math.Min(height, (currentChunk + 1) * chunkSize);
        
        Parallel.For(startY, endY, y => UpdateRow(y));
        
        currentChunk = (currentChunk + 1) % chunksPerFrame;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateRow(int y)
    {
        int rowStart = y * width;
        
        for (int x = 0; x < width; x++)
        {
            int index = rowStart + x;
            float potential = CalculatePotential(x, y);
            float growth = GrowthFunction(potential);
            nextGrid[index] = Math.Max(0.0f, Math.Min(1.0f, grid[index] + DeltaT * growth));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateRowSkipped(int y)
    {
        int rowStart = y * width;
        
        for (int x = 0; x < width; x += cellSkip)
        {
            int index = rowStart + x;
            float potential = CalculatePotential(x, y);
            float growth = GrowthFunction(potential);
            nextGrid[index] = Math.Max(0.0f, Math.Min(1.0f, grid[index] + DeltaT * growth));
            
            // Fill skipped cells with interpolated values
            if (cellSkip > 1 && x + cellSkip < width)
            {
                float nextValue = nextGrid[index];
                for (int skip = 1; skip < cellSkip && x + skip < width; skip++)
                {
                    nextGrid[rowStart + x + skip] = nextValue;
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculatePotential(int centerX, int centerY)
    {
        float sum = 0;
        
        for (int i = 0; i < KERNEL.Length; i++)
        {
            var (dx, dy, weight) = KERNEL[i];
            int targetX = centerX + dx;
            int targetY = centerY + dy;
            
            // Fast wrapping
            if (targetX < 0) targetX += width;
            else if (targetX >= width) targetX -= width;
            
            if (targetY < 0) targetY += height;
            else if (targetY >= height) targetY -= height;
            
            sum += grid[targetY * width + targetX] * weight;
        }
        
        return sum;
    }
    
    private void AdaptQualityBasedOnPerformance()
    {
        double targetTime = targetFrameTime * 0.7; // Use 70% of frame time for update
        
        if (lastUpdateTime > targetTime)
        {
            // Too slow, reduce quality
            if (processingQuality > 25)
            {
                processingQuality = Math.Max(25, processingQuality - 10);
                cellSkip = Math.Min(4, cellSkip + 1);
                chunksPerFrame = Math.Min(16, chunksPerFrame * 2);
            }
        }
        else if (lastUpdateTime < targetTime * 0.5 && processingQuality < 100)
        {
            // Fast enough, increase quality
            processingQuality = Math.Min(100, processingQuality + 5);
            if (processingQuality > 75)
            {
                cellSkip = Math.Max(1, cellSkip - 1);
                chunksPerFrame = Math.Max(1, chunksPerFrame / 2);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GrowthFunction(float u)
    {
        float z = (u - Mu) / Sigma;
        return 2.0f * MathF.Exp(-0.5f * z * z) - 1.0f;
    }
    
    public void SetTargetFPS(int fps)
    {
        targetFps = fps;
        targetFrameTime = 1000.0 / fps;
    }
}