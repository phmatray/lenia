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
    
    if (!imageData || imageData.width !== canvasWidth || imageData.height !== canvasHeight) {
        imageData = ctx.createImageData(canvasWidth, canvasHeight);
        data = imageData.data;
    }
    
    const scaleX = gridWidth / canvasWidth;
    const scaleY = gridHeight / canvasHeight;
    
    for (let canvasY = 0; canvasY < canvasHeight; canvasY++) {
        for (let canvasX = 0; canvasX < canvasWidth; canvasX++) {
            const gridX = Math.floor(canvasX * scaleX);
            const gridY = Math.floor(canvasY * scaleY);
            
            const gridIndex = gridY * gridWidth + gridX;
            const value = flatGrid[gridIndex];
            const intensity = Math.floor(value * 255);
            
            const pixelIndex = (canvasY * canvasWidth + canvasX) * 4;
            data[pixelIndex] = intensity;     // Red
            data[pixelIndex + 1] = intensity; // Green
            data[pixelIndex + 2] = intensity; // Blue
            data[pixelIndex + 3] = 255;       // Alpha
        }
    }
    
    ctx.putImageData(imageData, 0, 0);
}