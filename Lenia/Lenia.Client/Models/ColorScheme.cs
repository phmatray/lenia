namespace Lenia.Client.Models;

public class ColorScheme
{
    public string Name { get; init; }
    public string Description { get; init; }
    public (byte R, byte G, byte B)[] Colors { get; init; }
    
    public ColorScheme(string name, string description, (byte R, byte G, byte B)[] colors)
    {
        Name = name;
        Description = description;
        Colors = colors;
    }
    
    public (byte R, byte G, byte B) GetColor(float value)
    {
        // Clamp value between 0 and 1
        value = Math.Max(0, Math.Min(1, value));
        
        // Map value to color index
        int index = (int)(value * (Colors.Length - 1));
        float fraction = value * (Colors.Length - 1) - index;
        
        // Handle edge case
        if (index >= Colors.Length - 1)
        {
            return Colors[Colors.Length - 1];
        }
        
        // Interpolate between colors
        var color1 = Colors[index];
        var color2 = Colors[index + 1];
        
        return (
            R: (byte)(color1.R + (color2.R - color1.R) * fraction),
            G: (byte)(color1.G + (color2.G - color1.G) * fraction),
            B: (byte)(color1.B + (color2.B - color1.B) * fraction)
        );
    }
}

public static class ColorSchemes
{
    private static (byte, byte, byte)[] CreateColors(params (byte r, byte g, byte b)[] colors) => colors;
    
    public static readonly ColorScheme Plasma = new(
        "Plasma",
        "Vibrant purple to yellow gradient",
        CreateColors(
            (13, 8, 135),    // Dark purple
            (84, 2, 163),    // Purple
            (139, 10, 165),  // Violet
            (185, 50, 137),  // Pink
            (219, 92, 104),  // Rose
            (244, 136, 73),  // Orange
            (254, 188, 43),  // Yellow-orange
            (240, 249, 33)   // Yellow
        )
    );
    
    public static readonly ColorScheme Viridis = new(
        "Viridis",
        "Green to yellow scientific gradient",
        CreateColors(
            (68, 1, 84),     // Dark purple
            (59, 28, 140),   // Blue-purple
            (33, 65, 133),   // Blue
            (11, 100, 110),  // Teal
            (18, 130, 91),   // Green-teal
            (51, 160, 69),   // Green
            (111, 189, 55),  // Light green
            (180, 221, 61),  // Yellow-green
            (253, 231, 37)   // Yellow
        )
    );
    
    public static readonly ColorScheme Inferno = new(
        "Inferno",
        "Black to yellow fire gradient",
        CreateColors(
            (0, 0, 4),       // Black
            (40, 11, 84),    // Dark purple
            (101, 21, 110),  // Purple
            (159, 42, 99),   // Red-purple
            (212, 72, 66),   // Red
            (245, 125, 41),  // Orange
            (252, 194, 65),  // Yellow-orange
            (252, 255, 164)  // Light yellow
        )
    );
    
    public static readonly ColorScheme Ocean = new(
        "Ocean",
        "Deep blue ocean gradient",
        CreateColors(
            (3, 4, 94),      // Navy
            (2, 43, 140),    // Dark blue
            (0, 89, 177),    // Blue
            (0, 137, 205),   // Light blue
            (43, 184, 230),  // Sky blue
            (134, 224, 248), // Light cyan
            (194, 245, 255), // Very light cyan
            (255, 255, 255)  // White
        )
    );
    
    public static readonly ColorScheme Thermal = new(
        "Thermal",
        "Cool to hot thermal imaging",
        CreateColors(
            (0, 0, 0),       // Black
            (0, 0, 128),     // Dark blue
            (0, 0, 255),     // Blue
            (0, 255, 255),   // Cyan
            (0, 255, 0),     // Green
            (255, 255, 0),   // Yellow
            (255, 128, 0),   // Orange
            (255, 0, 0),     // Red
            (255, 255, 255)  // White
        )
    );
    
    public static readonly ColorScheme Grayscale = new(
        "Grayscale",
        "Classic black to white",
        CreateColors(
            (0, 0, 0),       // Black
            (255, 255, 255)  // White
        )
    );
    
    public static readonly ColorScheme[] All = new[]
    {
        Plasma,
        Viridis,
        Inferno,
        Ocean,
        Thermal,
        Grayscale
    };
}