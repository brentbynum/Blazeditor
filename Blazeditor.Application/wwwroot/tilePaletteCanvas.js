(function () {
    let paletteCanvas;
    let paletteContext;
    let paletteTiles = [];
    let cellSize = { width: 64, height: 64 }; // Default cell size, can be overridden
    let running = false;
    let isMouseOver = false;

    function renderLoop() {
        if (!running) return;
        window.tilePaletteCanvas.drawTiles();
        window.requestAnimationFrame(renderLoop);
    }
    function getScaledMousePosition(e) {
        if (!paletteCanvas) return { x: 0, y: 0 };
        const rect = paletteCanvas.getBoundingClientRect();
        const scaleX = paletteCanvas.width / rect.width;
        const scaleY = paletteCanvas.height / rect.height;
        const mouseX = (e.clientX - rect.left) * scaleX;
        const mouseY = (e.clientY - rect.top) * scaleY;
        return { x: mouseX, y: mouseY };
    }
    window.tilePaletteCanvas = {
        init: function (canvas, tiles, cellSizeArg) {
            paletteCanvas = canvas;
            paletteCanvas.onmousemove = this.mousemove;
            paletteCanvas.onmouseleave = this.onmouseleave;
            paletteCanvas.onclick = this.click;
            paletteContext = paletteCanvas.getContext('2d');
            paletteTiles = Object.values(tiles).slice().sort((a, b) => (b.size.width * b.size.height) - (a.size.width * a.size.height));
            if (cellSizeArg) {
                cellSize = cellSizeArg;
            }
            // Now we need to iterate all of the tiles and set their initial state
            for (let tile of paletteTiles) {
                if (!(tile.image && tile.image.startsWith('data:image'))) {
                    tile.image = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR42mP8//8/AwAI/wP+vQAAAABJRU5ErkJggg=='; // Placeholder image
                }
                tileImage = new window.Image();
                tileImage.src = tile.image;
                tile.paletteState = {
                    isMouseOver: false,
                    layout: null,
                    image: tileImage,
                    isLoaded: false
                };
                tileImage.onload = function () {
                    tile.paletteState.isLoaded = true;
                };
            }
            this.startRenderLoop();
        },
        mousemove: function (e) {
            if (!paletteTiles || paletteTiles.length === 0) return;
            const rect = paletteCanvas.getBoundingClientRect();
            const pos = getScaledMousePosition(e);
            for (let tile of paletteTiles) {
                if (tile.paletteState && tile.paletteState.layout) {
                    const l = tile.paletteState.layout;
                    tile.paletteState.isMouseOver = pos.x >= l.x && pos.x <= l.x + l.width && pos.y >= l.y && pos.y <= l.y + l.height;
                } else if (tile.paletteState) {
                    tile.paletteState.isMouseOver = false;
                }
            }
            isMouseOver = true;
        },
        onmouseleave: function () {
            isMouseOver = false;
        },
        click: function (e) {
            if (!paletteTiles || paletteTiles.length === 0) return;
            const pos = getScaledMousePosition(e);
            let selectedTileId = null;
            for (let tile of paletteTiles) {
                if (tile.paletteState && tile.paletteState.layout) {
                    const l = tile.paletteState.layout;
                    const isClicked = pos.x >= l.x && pos.x <= l.x + l.width && pos.y >= l.y && pos.y <= l.y + l.height;
                    tile.paletteState.isSelected = isClicked;
                    if (isClicked) {
                        selectedTileId = tile.id;
                    }
                } else if (tile.paletteState) {
                    tile.paletteState.isSelected = false;
                }
            }
            window.tilePaletteCanvas.drawTiles();
            if (selectedTileId !== null && window.DotNet && window.tilePaletteCanvas.dotNetRef) {
                window.tilePaletteCanvas.dotNetRef.invokeMethodAsync('OnJsTileSelected', selectedTileId);
            }
        },
        drawTiles: function () {
            paletteContext.clearRect(0, 0, paletteCanvas.width, paletteCanvas.height);
            if (!paletteTiles) return;
            let bin = new MaxRectsBin(paletteCanvas.width, paletteCanvas.height);
            for (let i = 0; i < paletteTiles.length; i++) {
                const tile = paletteTiles[i];
                let w = tile.size.width * cellSize;
                let h = tile.size.height * cellSize;
                let pos = bin.insert(w, h);
                if (!pos) continue; // Skip if can't fit
                // Store layout for hit testing
                if (!tile.paletteState) tile.paletteState = {};
                tile.paletteState.layout = { x: pos.x, y: pos.y, width: w, height: h };
                // Draw tile image if present
                if (tile.image && tile.image.startsWith('data:image')) {
                    paletteContext.drawImage(tile.paletteState.image, pos.x, pos.y, w, h);
                    paletteContext.strokeStyle = isMouseOver && tile.paletteState.isMouseOver ? '#ff0' : '#999';
                    paletteContext.strokeRect(pos.x, pos.y, w, h);
                } else {
                    paletteContext.fillStyle = '#6c757d';
                    paletteContext.fillRect(pos.x, pos.y, w, h);
                    paletteContext.strokeStyle = '#343a40';
                    paletteContext.strokeRect(pos.x, pos.y, w, h);
                }
                paletteContext.fillStyle = isMouseOver && tile.paletteState.isMouseOver ? '#ff0' : '#999';
                paletteContext.font = '16px sans-serif';
                paletteContext.fillText(tile.name || 'Tile', pos.x + 8, pos.y + 32);
            }
        },
        calculateRequiredHeight: function (tiles, cellSizeArg, canvasWidth) {
            let cellSize = cellSizeArg || { width: 64, height: 64 };
            // Use a bin packer to simulate placement and get the max Y
            let bin = new MaxRectsBin(canvasWidth, 100000); // Large height
            let maxY = 0;
            Object.keys(tiles).forEach(key => {
                let w = tiles[key].size.width * cellSize;
                let h = tiles[key].size.height * cellSize;
                let pos = bin.insert(w, h);
                if (pos) {
                    maxY = Math.max(maxY, pos.y + h);
                }
            });
            // Add a little padding
            return Math.ceil(maxY + 8);
        },
        setCanvasHeight: function (canvas, height) {
            canvas.height = height;
        },
        startRenderLoop: function () {
            running = true;
            renderLoop();
        },
        stopRenderLoop: function () {
            running = false;
        },
        setDotNetRef: function (dotNetRef) {
            window.tilePaletteCanvas.dotNetRef = dotNetRef;
        }
    };

    // MaxRects bin packer (simple version)
    function MaxRectsBin(width, height) {
        this.width = width;
        this.height = height;
        this.freeRects = [{ x: 0, y: 0, width: width, height: height }];
        this.placements = [];
    }

    MaxRectsBin.prototype.insert = function (w, h) {
        let bestNode = null;
        let bestShortSide = Infinity;
        let bestIndex = -1;

        for (let i = 0; i < this.freeRects.length; i++) {
            let r = this.freeRects[i];
            if (w <= r.width && h <= r.height) {
                let leftoverHoriz = Math.abs(r.width - w);
                let leftoverVert = Math.abs(r.height - h);
                let shortSide = Math.min(leftoverHoriz, leftoverVert);
                if (shortSide < bestShortSide) {
                    bestNode = { x: r.x, y: r.y, width: w, height: h };
                    bestShortSide = shortSide;
                    bestIndex = i;
                }
            }
        }
        if (!bestNode) return null;

        // Split the free rect
        let r = this.freeRects[bestIndex];
        this.freeRects.splice(bestIndex, 1);
        // Add new free rects (right and below)
        if (r.width - w > 0) {
            this.freeRects.push({ x: r.x + w, y: r.y, width: r.width - w, height: h });
        }
        if (r.height - h > 0) {
            this.freeRects.push({ x: r.x, y: r.y + h, width: r.width, height: r.height - h });
        }
        this.placements.push(bestNode);
        return bestNode;
    };
})();
