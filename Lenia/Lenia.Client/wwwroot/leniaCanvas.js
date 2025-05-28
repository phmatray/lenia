let canvas;
let ctx;
let dotNetHelper;

export function initializeCanvas(canvasElement, dotNetRef) {
    canvas = canvasElement;
    ctx = canvas.getContext('2d');
    dotNetHelper = dotNetRef;
}

export function renderGrid(flatGrid, gridWidth, gridHeight) {
    if (!ctx) return;
    
    const canvasWidth = canvas.width;
    const canvasHeight = canvas.height;
    const cellWidth = canvasWidth / gridWidth;
    const cellHeight = canvasHeight / gridHeight;
    
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);
    
    let index = 0;
    for (let y = 0; y < gridHeight; y++) {
        for (let x = 0; x < gridWidth; x++) {
            const value = flatGrid[index++];
            const intensity = Math.floor(value * 255);
            
            ctx.fillStyle = `rgb(${intensity}, ${intensity}, ${intensity})`;
            ctx.fillRect(x * cellWidth, y * cellHeight, cellWidth, cellHeight);
        }
    }
}