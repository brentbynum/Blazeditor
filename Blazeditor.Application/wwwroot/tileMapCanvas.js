(function () {
    let mapCanvas;
    let ctx;
    let tileMaps = {};
    let cellSize = 64;
    let running = false;
    let hoveredCell = { x: -1, y: -1, level: 0 };
    let selectedTileId = null;
    let dotNetRef = null;
    var showGrid = true;
    let tools = {
        paint: {
            action: (x, y, level) => {
                if (dotNetRef && selectedTileId !== null) {
                    dotNetRef.invokeMethodAsync('OnJsPlaceTile', selectedTileId, x, y, level);
                }
            }
        },
        select: {},
        fill: {},
        eraser: {
            action: (x, y, level) => {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnJsRemoveTile', x, y, level);
                }
            }
        }
    }
    // default tool is paint
    let selectedTool = tools.paint;
    function renderLoop() {
        if (!running) return;
        window.tileMapCanvas.drawTiles();
        window.requestAnimationFrame(renderLoop);
    }

    window.tileMapCanvas = {
        init: function (canvas, tileMapsArg, cellSizeArg) {
            mapCanvas = canvas;
            ctx = mapCanvas.getContext('2d');
            mapCanvas.onmousemove = this.mousemove;
            mapCanvas.onclick = this.click;
            if (tileMapsArg) {
                tileMaps = tileMapsArg;
            }
            if (cellSizeArg) {
                cellSize = cellSizeArg;
            }
            
            running = false;
            this.startRenderLoop();
        },
        updateTileMaps: function (tileMapsArg) {
            if (tileMapsArg) {
                tileMaps = tileMapsArg;
            }
        },
        toggleGrid: function (val) {
            showGrid = val;
        },
        selectTool(toolId) {
            selectedTool = tools[toolId] || tools.paint;
        },
        setSelectedTileId: function (tileId) {
            selectedTileId = tileId;
        },
        setDotNetRef: function (ref) {
            dotNetRef = ref;
        },
        mousemove: function (e) {
            if (!mapCanvas) return;
            const rect = mapCanvas.getBoundingClientRect();
            const scaleX = mapCanvas.width / rect.width;
            const scaleY = mapCanvas.height / rect.height;
            const mouseX = (e.clientX - rect.left) * scaleX;
            const mouseY = (e.clientY - rect.top) * scaleY;
            hoveredCell.x = Math.floor(mouseX / cellSize);
            hoveredCell.y = Math.floor(mouseY / cellSize);
            hoveredCell.level = 0; // For now, always use level 0
            window.tileMapCanvas.drawTiles();
        },
        click: function (e) {
            if (!mapCanvas || selectedTileId === null) return;
            const rect = mapCanvas.getBoundingClientRect();
            const scaleX = mapCanvas.width / rect.width;
            const scaleY = mapCanvas.height / rect.height;
            const mouseX = (e.clientX - rect.left) * scaleX;
            const mouseY = (e.clientY - rect.top) * scaleY;
            const x = Math.floor(mouseX / cellSize);
            const y = Math.floor(mouseY / cellSize);
            const level = 0; // For now, always use level 0
            selectedTool.action(x, y, level);
        },
        drawTiles: function () {
            if (!mapCanvas || !ctx) return;
            ctx.clearRect(0, 0, mapCanvas.width, mapCanvas.height);
            for (let levelKey of Object.keys(tileMaps)) {
                const tileMap = tileMaps[levelKey];
                for (let y = 0; y < tileMap.size.height; y++) {
                    for (let x = 0; x < tileMap.size.width; x++) {
                        const idx = y * tileMap.size.width + x;
                        const tile = tileMap.tiles ? tileMap.tiles[idx] : null;
                        if (tile && !tile.layout) {
                            let img = new window.Image();
                            img.src = tile.image;
                            tile.layout = {
                                image: img
                            }
                            img.onload = () => { tile.layout.isLoaded = true;  }
                        }
                        if (tile && tile.image && tile.image.startsWith('data:image') && tile.layout.isLoaded) {                       
                            ctx.drawImage(tile.layout.image, x * cellSize, y * cellSize, tile.size.width * cellSize, tile.size.height * cellSize);
                        }
                        if (showGrid) {
                            // Draw grid
                            ctx.strokeStyle = '#ccc';
                            ctx.strokeRect(x * cellSize, y * cellSize, cellSize, cellSize);
                            // Highlight hovered cell
                            if (x === hoveredCell.x && y === hoveredCell.y && parseInt(levelKey) === hoveredCell.level) {
                                ctx.save();
                                ctx.strokeStyle = '#ff0';
                                ctx.lineWidth = 3;
                                ctx.strokeRect(x * cellSize, y * cellSize, cellSize, cellSize);
                                ctx.restore();
                            }
                        }
                    }
                }
            }
        },
        startRenderLoop: function () {
            running = true;
            renderLoop();
        },
        stopRenderLoop: function () {
            running = false;
        }
    };
})();