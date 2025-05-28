let canvas;
let ctx;
let dotNetHelper;
let imageData;
let data;

export function initializeCanvas(canvasElement, dotNetRef) {
    canvas = canvasElement;
    ctx = canvas.getContext('2d');
    dotNetHelper = dotNetRef;
}

export function renderGrid(flatGrid, gridWidth, gridHeight) {
    if (!ctx) return;
    
    const canvasWidth = canvas.width;
    const canvasHeight = canvas.height;
    
    // Use direct grid-to-canvas mapping for better performance
    if (!imageData || imageData.width !== gridWidth || imageData.height !== gridHeight) {
        imageData = ctx.createImageData(gridWidth, gridHeight);
        data = imageData.data;
    }
    
    // Direct 1:1 mapping from grid to imageData
    for (let i = 0; i < flatGrid.length; i++) {
        const value = flatGrid[i];
        const intensity = Math.floor(value * 255);
        const pixelIndex = i * 4;
        
        data[pixelIndex] = intensity;     // Red
        data[pixelIndex + 1] = intensity; // Green
        data[pixelIndex + 2] = intensity; // Blue
        data[pixelIndex + 3] = 255;       // Alpha
    }
    
    // Scale up the small grid to fill the canvas
    ctx.putImageData(imageData, 0, 0);
    ctx.imageSmoothingEnabled = false; // Pixelated scaling for performance
    ctx.drawImage(canvas, 0, 0, gridWidth, gridHeight, 0, 0, canvasWidth, canvasHeight);
}