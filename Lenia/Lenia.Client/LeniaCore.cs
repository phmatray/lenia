using System;
using System.Threading.Tasks;

namespace Lenia.Client;

public class LeniaCore
{
    private double[] grid;
    private double[] nextGrid;
    private readonly int width;
    private readonly int height;
    private readonly int size;
    
    private double[] kernelWeights;
    private int[] kernelOffsets;
    private int kernelSize;
    private double kernelSum;
    
    public double R { get; set; } = 13.0;
    public double DeltaT { get; set; } = 0.1;
    public double Mu { get; set; } = 0.15;
    public double Sigma { get; set; } = 0.017;
    public double KernelAlpha { get; set; } = 4.0;
    
    public int Width => width;
    public int Height => height;
    
    public LeniaCore(int width, int height)
    {
        this.width = width;
        this.height = height;
        this.size = width * height;
        grid = new double[size];
        nextGrid = new double[size];
        
        PrecomputeKernel();
        InitializeRandom();
    }
    
    public double[] GetGrid() => grid;
    
    private void PrecomputeKernel()
    {
        var kernelRadius = (int)Math.Ceiling(R);
        var tempKernel = new List<(int offset, double weight)>();
        kernelSum = 0;
        
        for (int dx = -kernelRadius; dx <= kernelRadius; dx++)
        {
            for (int dy = -kernelRadius; dy <= kernelRadius; dy++)
            {
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > R) continue;
                
                var weight = KernelFunction(distance / R);
                if (weight > 1e-10)
                {
                    var offset = dy * width + dx;
                    tempKernel.Add((offset, weight));
                    kernelSum += weight;
                }
            }
        }
        
        kernelSize = tempKernel.Count;
        kernelOffsets = new int[kernelSize];
        kernelWeights = new double[kernelSize];
        
        for (int i = 0; i < kernelSize; i++)
        {
            kernelOffsets[i] = tempKernel[i].offset;
            kernelWeights[i] = tempKernel[i].weight / kernelSum;
        }
    }
    
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
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                grid[y * width + x] = distance <= radius ? 1.0 : 0.0;
            }
        }
    }
    
    public void InitializeRing(int centerX, int centerY, int innerRadius, int outerRadius)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                grid[y * width + x] = (distance >= innerRadius && distance <= outerRadius) ? 1.0 : 0.0;
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
        InitializeCircle(width / 2, height / 2, 16);
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
        InitializeRing(width / 2, height / 2, 8, 12);
    }
    
    public void Clear()
    {
        Array.Clear(grid, 0, grid.Length);
    }
    
    public void Update()
    {
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                var index = y * width + x;
                var potential = CalculatePotentialOptimized(x, y);
                var growth = GrowthFunction(potential);
                nextGrid[index] = Math.Max(0, Math.Min(1, grid[index] + DeltaT * growth));
            }
        });
        
        (grid, nextGrid) = (nextGrid, grid);
    }
    
    private double CalculatePotentialOptimized(int centerX, int centerY)
    {
        double sum = 0;
        var centerIndex = centerY * width + centerX;
        
        for (int i = 0; i < kernelSize; i++)
        {
            var offset = kernelOffsets[i];
            var targetX = centerX + (offset % width);
            var targetY = centerY + (offset / width);
            
            if (targetX < 0) targetX += width;
            if (targetX >= width) targetX -= width;
            if (targetY < 0) targetY += height;
            if (targetY >= height) targetY -= height;
            
            var targetIndex = targetY * width + targetX;
            sum += grid[targetIndex] * kernelWeights[i];
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