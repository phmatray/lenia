let canvas;
let ctx;
let dotNetHelper;
let imageData;
let data;
let currentColorScheme = null;

export function initializeCanvas(canvasElement, dotNetRef) {
    canvas = canvasElement;
    ctx = canvas.getContext('2d');
    dotNetHelper = dotNetRef;
}

export function setColorScheme(colors) {
    currentColorScheme = colors;
    console.log('Color scheme set:', colors);
}

export function renderGrid(flatGrid, gridWidth, gridHeight) {
    if (!ctx) return;
    
    const canvasWidth = canvas.width;
    const canvasHeight = canvas.height;
    
    // Debug log for first render
    if (!window._hasLoggedColorScheme) {
        console.log('Rendering with color scheme:', currentColorScheme);
        window._hasLoggedColorScheme = true;
    }
    
    // Use direct grid-to-canvas mapping for better performance
    if (!imageData || imageData.width !== gridWidth || imageData.height !== gridHeight) {
        imageData = ctx.createImageData(gridWidth, gridHeight);
        data = imageData.data;
    }
    
    // Direct 1:1 mapping from grid to imageData
    for (let i = 0; i < flatGrid.length; i++) {
        const value = Math.max(0, Math.min(1, flatGrid[i])); // Clamp to 0-1
        const pixelIndex = i * 4;
        
        if (currentColorScheme && currentColorScheme.length > 0) {
            // Use color scheme
            const colorIndex = Math.min(
                Math.floor(value * (currentColorScheme.length - 1)), 
                currentColorScheme.length - 1
            );
            const fraction = (value * (currentColorScheme.length - 1)) - colorIndex;
            
            let r, g, b;
            if (colorIndex < currentColorScheme.length - 1) {
                // Interpolate between colors
                const color1 = currentColorScheme[colorIndex];
                const color2 = currentColorScheme[colorIndex + 1];
                r = Math.floor(color1.r + (color2.r - color1.r) * fraction);
                g = Math.floor(color1.g + (color2.g - color1.g) * fraction);
                b = Math.floor(color1.b + (color2.b - color1.b) * fraction);
            } else {
                // Use last color
                const color = currentColorScheme[colorIndex];
                r = color.r;
                g = color.g;
                b = color.b;
            }
            
            data[pixelIndex] = r;     // R
            data[pixelIndex + 1] = g; // G
            data[pixelIndex + 2] = b; // B
        } else {
            // Fallback to grayscale
            const intensity = Math.floor(value * 255);
            data[pixelIndex] = intensity;     // Red
            data[pixelIndex + 1] = intensity; // Green
            data[pixelIndex + 2] = intensity; // Blue
        }
        data[pixelIndex + 3] = 255;       // Alpha
    }
    
    // Scale up the small grid to fill the canvas
    ctx.putImageData(imageData, 0, 0);
    ctx.imageSmoothingEnabled = false; // Pixelated scaling for performance
    ctx.drawImage(canvas, 0, 0, gridWidth, gridHeight, 0, 0, canvasWidth, canvasHeight);
}

// Add function to download canvas as image
export function downloadCanvas(filename) {
    if (!canvas) return;
    
    const link = document.createElement('a');
    link.download = filename;
    link.href = canvas.toDataURL();
    link.click();
}

// Add function to get canvas data as base64
export function getCanvasData() {
    if (!canvas) return null;
    return canvas.toDataURL();
}