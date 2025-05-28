using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lenia.Client;

public class LeniaMinimal
{
    private float[] grid;
    private float[] nextGrid;
    private readonly int width;
    private readonly int height;
    private readonly int size;
    
    // Minimal kernel - only nearest neighbors
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
    
    public LeniaMinimal(int width, int height)
    {
        this.width = width;
        this.height = height;
        this.size = width * height;
        grid = new float[size];
        nextGrid = new float[size];
        InitializeRandom();
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
        InitializeCircle(width / 2, height / 2, 6);
    }
    
    public void InitializeGeminium()
    {
        InitializeRing(width / 2, height / 2, 3, 5);
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GrowthFunction(float u)
    {
        float z = (u - Mu) / Sigma;
        return 2.0f * MathF.Exp(-0.5f * z * z) - 1.0f;
    }
}