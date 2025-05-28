using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lenia.Client;

public class LeniaCoreOptimized
{
    private double[] grid;
    private double[] nextGrid;
    private readonly int width;
    private readonly int height;
    private readonly int size;
    
    private readonly int[] kernelX;
    private readonly int[] kernelY;
    private readonly double[] kernelWeights;
    private readonly int kernelCount;
    
    public double R { get; set; } = 10.0;
    public double DeltaT { get; set; } = 0.1;
    public double Mu { get; set; } = 0.15;
    public double Sigma { get; set; } = 0.016;
    public double KernelAlpha { get; set; } = 4.0;
    
    public int Width => width;
    public int Height => height;
    
    public LeniaCoreOptimized(int width, int height)
    {
        this.width = width;
        this.height = height;
        this.size = width * height;
        grid = new double[size];
        nextGrid = new double[size];
        
        var kernelList = new List<(int x, int y, double weight)>();
        BuildKernel(kernelList);
        
        kernelCount = kernelList.Count;
        kernelX = new int[kernelCount];
        kernelY = new int[kernelCount];
        kernelWeights = new double[kernelCount];
        
        for (int i = 0; i < kernelCount; i++)
        {
            kernelX[i] = kernelList[i].x;
            kernelY[i] = kernelList[i].y;
            kernelWeights[i] = kernelList[i].weight;
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
    
    public double[] GetGrid() => grid;
    
    private void InitializeRandom()
    {
        var random = new Random();
        for (int i = 0; i < size; i++)
        {
            grid[i] = random.NextDouble();
        }
    }
    
    public void InitializeCircle(int centerX, int centerY, int radius)
    {
        Array.Clear(grid, 0, size);
        for (int y = Math.Max(0, centerY - radius); y <= Math.Min(height - 1, centerY + radius); y++)
        {
            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(width - 1, centerX + radius); x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    grid[y * width + x] = 1.0;
                }
            }
        }
    }
    
    public void InitializeRing(int centerX, int centerY, int innerRadius, int outerRadius)
    {
        Array.Clear(grid, 0, size);
        for (int y = Math.Max(0, centerY - outerRadius); y <= Math.Min(height - 1, centerY + outerRadius); y++)
        {
            for (int x = Math.Max(0, centerX - outerRadius); x <= Math.Min(width - 1, centerX + outerRadius); x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distSq = dx * dx + dy * dy;
                if (distSq >= innerRadius * innerRadius && distSq <= outerRadius * outerRadius)
                {
                    grid[y * width + x] = 1.0;
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
        InitializeCircle(width / 2, height / 2, 12);
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
        InitializeRing(width / 2, height / 2, 6, 10);
    }
    
    public void Clear()
    {
        Array.Clear(grid, 0, size);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        Parallel.For(0, height, y =>
        {
            UpdateRow(y);
        });
        
        (grid, nextGrid) = (nextGrid, grid);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateRow(int y)
    {
        var rowStart = y * width;
        
        for (int x = 0; x < width; x++)
        {
            var index = rowStart + x;
            var potential = CalculatePotentialFast(x, y);
            var growth = GrowthFunction(potential);
            nextGrid[index] = Math.Max(0.0, Math.Min(1.0, grid[index] + DeltaT * growth));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculatePotentialFast(int centerX, int centerY)
    {
        double sum = 0;
        
        for (int i = 0; i < kernelCount; i++)
        {
            var targetX = centerX + kernelX[i];
            var targetY = centerY + kernelY[i];
            
            if (targetX < 0) targetX += width;
            else if (targetX >= width) targetX -= width;
            
            if (targetY < 0) targetY += height;
            else if (targetY >= height) targetY -= height;
            
            var targetIndex = targetY * width + targetX;
            sum += grid[targetIndex] * kernelWeights[i];
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