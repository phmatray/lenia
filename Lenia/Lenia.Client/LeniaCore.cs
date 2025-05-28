using System;

namespace Lenia.Client;

public class LeniaCore
{
    private double[,] grid;
    private double[,] nextGrid;
    private readonly int width;
    private readonly int height;
    
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
        grid = new double[width, height];
        nextGrid = new double[width, height];
        InitializeRandom();
    }
    
    public double[,] GetGrid() => grid;
    
    private void InitializeRandom()
    {
        var random = new Random();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = random.NextDouble();
            }
        }
    }
    
    public void InitializeCircle(int centerX, int centerY, int radius)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                grid[x, y] = distance <= radius ? 1.0 : 0.0;
            }
        }
    }
    
    public void InitializeRing(int centerX, int centerY, int innerRadius, int outerRadius)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                grid[x, y] = (distance >= innerRadius && distance <= outerRadius) ? 1.0 : 0.0;
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
        
        Clear();
        InitializeRing(width / 2, height / 2, 8, 12);
    }
    
    public void Clear()
    {
        Array.Clear(grid, 0, grid.Length);
    }
    
    public void Update()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var potential = CalculatePotential(x, y);
                var growth = GrowthFunction(potential);
                nextGrid[x, y] = Math.Max(0, Math.Min(1, grid[x, y] + DeltaT * growth));
            }
        }
        
        (grid, nextGrid) = (nextGrid, grid);
    }
    
    private double CalculatePotential(int centerX, int centerY)
    {
        double sum = 0;
        double kernelSum = 0;
        
        int kernelRadius = (int)Math.Ceiling(R);
        
        for (int dx = -kernelRadius; dx <= kernelRadius; dx++)
        {
            for (int dy = -kernelRadius; dy <= kernelRadius; dy++)
            {
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > R) continue;
                
                var nx = (centerX + dx + width) % width;
                var ny = (centerY + dy + height) % height;
                
                var kernelValue = KernelFunction(distance / R);
                sum += grid[nx, ny] * kernelValue;
                kernelSum += kernelValue;
            }
        }
        
        return kernelSum > 0 ? sum / kernelSum : 0;
    }
    
    private double KernelFunction(double r)
    {
        if (r >= 1) return 0;
        return Math.Exp(KernelAlpha - KernelAlpha / (4 * r * (1 - r)));
    }
    
    private double GrowthFunction(double u)
    {
        var z = (u - Mu) / Sigma;
        return 2 * Math.Exp(-z * z / 2) - 1;
    }
}