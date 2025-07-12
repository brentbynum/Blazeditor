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
    let origin = { x: 0, y: 0 };
    let scale = 1;
    const minScale = 0.2;
    const maxScale = 4;
    let selectedCells = [];
    let tilePalette = [];

    function cellsEqual(a, b) {
        return a.x === b.x && a.y === b.y && a.level === b.level;
    }
    function cellInCollection(cell, collection) {
        return collection.some(c => cellsEqual(c, cell));
    }
    function getRectCells(a, b) {
        const cells = [];
        const minX = Math.min(a.x, b.x);
        const maxX = Math.max(a.x, b.x);
        const minY = Math.min(a.y, b.y);
        const maxY = Math.max(a.y, b.y);
        for (let x = minX; x <= maxX; x++) {
            for (let y = minY; y <= maxY; y++) {
                cells.push({ x, y, level: a.level });
            }
        }
        return cells;
    }

    // Add a global object for profiling
    window.tileMapPaintProfile = {};

    let tools = {
        paint: (function () {
            return {
                cursor: "url('/cursors/paint.png'), auto",
                click: (x, y, level) => {
                    if (dotNetRef && selectedTileId !== null) {
                        window.tileMapPaintProfile.start = performance.now();
                        dotNetRef.invokeMethodAsync('OnJsPlaceTile', selectedTileId, x, y, level);
                    }
                },
                drawOverlay: (ctx, x, y, level) => {
                    if (selectedTileId !== null && tilePalette && tilePalette.length > 0) {
                        const tile = tilePalette.find(t => t.id === selectedTileId);
                        if (tile) {
                            if (!tile._overlayImage) {
                                let img = new window.Image();
                                img.src = tile.image;
                                img.onload = () => { tile._overlayImageLoaded = true; };
                                tile._overlayImage = img;
                            }
                            if (tile._overlayImage && tile._overlayImageLoaded) {
                                ctx.save();
                                ctx.globalAlpha = 0.5;
                                ctx.drawImage(tile._overlayImage, x * cellSize, y * cellSize, tile.size.width * cellSize, tile.size.height * cellSize);
                                ctx.restore();
                            }
                        }
                    }
                }
            }
        })(),
        select: {
            cursor: "url('/cursors/select.png'), auto",
            click: (x, y, level, e) => {
                const cell = { x, y, level };
                if (e && e.shiftKey) {
                    if (selectedCells.length > 0) {
                        const last = selectedCells[selectedCells.length - 1];
                        const rectCells = getRectCells(last, cell);
                        // Add all cells in the rectangle, avoiding duplicates
                        rectCells.forEach(rc => {
                            if (!cellInCollection(rc, selectedCells)) {
                                selectedCells.push(rc);
                            }
                        });
                    } else {
                        selectedCells.push(cell);
                    }
                } else if (e && e.ctrlKey) {
                    const idx = selectedCells.findIndex(c => cellsEqual(c, cell));
                    if (idx === -1) {
                        selectedCells.push(cell);
                    } else {
                        selectedCells.splice(idx, 1);
                    }
                } else {
                    selectedCells = [cell];
                }
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnJsSelectionChanged', selectedCells);
                }
            }
        },
        fill: {
            cursor: "url('/cursors/fill.png'), auto",
            click: (x, y, level, e) => {
                if (dotNetRef) {
                    // Pass ctrlKey to C# for alternate fill mode
                    const ctrlKey = e && e.ctrlKey;
                    dotNetRef.invokeMethodAsync('OnJsFill', selectedTileId, x, y, level, ctrlKey);
                }
            }
        },
        erase: {
            cursor: "url('/cursors/erase.png'), auto",
            click: (x, y, level) => {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnJsRemoveTile', x, y, level);
                }
            }
        },
        pan: (function () {
            let panState = {
                active: false,
                mousePos: { x: 0, y: 0 }
            };
            return {
                cursor: "url('/cursors/pan.png'), auto",
                mousedown: (pos) => {
                    panState.active = true;
                    panState.mousePos.x = pos.x;
                    panState.mousePos.y = pos.y;
                },
                mousemove: (pos) => {
                    if (panState.active) {
                        origin.x += (panState.mousePos.x - pos.x) / scale;
                        origin.y += (panState.mousePos.y - pos.y) / scale;
                        panState.mousePos.x = pos.x;
                        panState.mousePos.y = pos.y;
                    }
                },
                mouseup: () => { panState.active = false; },
                mouseleave: () => { panState.active = false; }
            };
        })()
    };

    // default tool is paint
    let selectedTool = tools.paint;
    function renderLoop() {
        if (!running) return;
        window.tileMapCanvas.drawTiles();
        window.requestAnimationFrame(renderLoop);
    }
    function getScaledMousePosition(e) {
        if (!mapCanvas) return { x: 0, y: 0 };
        const rect = mapCanvas.getBoundingClientRect();
        const scaleX = mapCanvas.width / rect.width;
        const scaleY = mapCanvas.height / rect.height;
        const mouseX = (e.clientX - rect.left) * scaleX;
        const mouseY = (e.clientY - rect.top) * scaleY;
        return { x: mouseX, y: mouseY };
    }
    function handleWheel(e) {
        var pos = getScaledMousePosition(e);
        // Mouse position in world coords before zoom
        const worldX = origin.x + pos.x / scale;
        const worldY = origin.y + pos.y / scale;
        // Adjust scale
        const zoomFactor = 1.1;
        let newScale = scale;
        if (e.deltaY < 0) {
            newScale = Math.min(scale * zoomFactor, maxScale);
        } else {
            newScale = Math.max(scale / zoomFactor, minScale);
        }
        // Adjust origin so the world point under the mouse stays under the mouse
        origin.x = worldX - pos.x / newScale;
        origin.y = worldY - pos.y / newScale;
        scale = newScale;
    }

    window.tileMapCanvas = {
        init: function (canvas, tileMapsArg, cellSizeArg, tilePaletteArg) {
            mapCanvas = canvas;
            ctx = mapCanvas.getContext('2d');
            mapCanvas.onmousemove = this.mousemove;
            mapCanvas.onclick = this.click;
            mapCanvas.addEventListener('wheel', handleWheel, { passive: false });
            mapCanvas.addEventListener('mousedown', this.mousedown);
            mapCanvas.addEventListener('mouseup', this.mouseup);
            mapCanvas.addEventListener('mouseleave', this.mouseleave);
            if (tileMapsArg) {
                tileMaps = tileMapsArg;
            }
            if (cellSizeArg) {
                cellSize = cellSizeArg;
            }
            if (tilePaletteArg) {
                tilePalette = tilePaletteArg;
            }
            running = false;
            this.startRenderLoop();
        },
        updateTileMaps: function (val) {
            if (val) {
                tileMaps = val;
            }
        },
        updateTilePositions: function (positions) {
            if (!positions) return;
            if (positions.length) {
                positions.forEach(pos => {
                    let map = tileMaps[pos.level]
                    map.tiles[pos.x + (pos.y * map.size.width)] = pos.tile;
                });
            } else {
                let pos = positions;
                let map = tileMaps[pos.level]
                map.tiles[pos.x + (pos.y * map.size.width)] = pos.tile;
            }
        },
        setShowGrid: function (val) {
            showGrid = val;
        },
        selectTool(toolId) {
            selectedTool = tools[toolId] || tools.paint;
            // Set the canvas cursor style
            if (mapCanvas && selectedTool.cursor) {
                mapCanvas.style.cursor = selectedTool.cursor;
            }
        },
        setSelectedTileId: function (val) {
            selectedTileId = val;
        },
        setDotNetRef: function (ref) {
            dotNetRef = ref;
        },
        mouseup: function (e) {
            if (selectedTool.mouseup) {
                selectedTool.mouseup(getScaledMousePosition(e));
            }
        },
        mousedown: function (e) {
            if (selectedTool.mousedown) {
                selectedTool.mousedown(getScaledMousePosition(e));
            }
        },
        mouseleave: function (e) {
            if (selectedTool.mouseleave) {
                selectedTool.mouseleave(getScaledMousePosition(e));
            }
        },
        mousemove: function (e) {
            var pos = getScaledMousePosition(e);
            // Convert to world coordinates
            const worldX = origin.x + pos.x / scale;
            const worldY = origin.y + pos.y / scale;
            hoveredCell.x = Math.floor(worldX / cellSize);
            hoveredCell.y = Math.floor(worldY / cellSize);
            hoveredCell.level = 0; // For now, always use level 0

            if (selectedTool.mousemove) {
                selectedTool.mousemove(pos);
            }
        },
        click: function (e) {
            var pos = getScaledMousePosition(e);
            // Convert to world coordinates
            const worldX = origin.x + pos.x / scale;
            const worldY = origin.y + pos.y / scale;
            const x = Math.floor(worldX / cellSize);
            const y = Math.floor(worldY / cellSize);
            const level = 0; // For now, always use level 0
            if (selectedTool.click) {
                selectedTool.click(x, y, level, e);
            }
        },
        drawTiles: function () {
            if (!mapCanvas || !ctx) return;
            ctx.clearRect(0, 0, mapCanvas.width, mapCanvas.height);

            ctx.save();
            ctx.scale(scale, scale);
            ctx.translate(-origin.x, -origin.y);
            // Draw selection highlight (fill only, no border)
            if (selectedCells.length > 0) {
                ctx.save();
                ctx.globalAlpha = 0.3;
                ctx.fillStyle = '#007bff';
                selectedCells.forEach(cell => {
                    ctx.fillRect(cell.x * cellSize, cell.y * cellSize, cellSize, cellSize);
                });
                ctx.restore();
            }
            // Draw tiles
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
                            img.onload = () => { tile.layout.isLoaded = true; }
                        }
                        if (tile && tile.image && tile.image.startsWith('data:image') && tile.layout.isLoaded) {
                            ctx.drawImage(tile.layout.image, x * cellSize, y * cellSize, tile.size.width * cellSize, tile.size.height * cellSize);
                        }
                    }
                }
            }
            // Draw grid and hovered cell
            for (let levelKey of Object.keys(tileMaps)) {
                const tileMap = tileMaps[levelKey];
                for (let y = 0; y < tileMap.size.height; y++) {
                    for (let x = 0; x < tileMap.size.width; x++) {
                        // Draw a border around the whole grid
                        ctx.save();
                        ctx.strokeStyle = '#000';
                        ctx.lineWidth = 3;
                        ctx.strokeRect(0, 0, tileMap.size.width * cellSize, tileMap.size.height * cellSize);
                        ctx.restore();
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
                        // Draw tile overlay for the hovered cell
                        if (selectedTool.drawOverlay && x === hoveredCell.x && y === hoveredCell.y && parseInt(levelKey) === hoveredCell.level) {
                            selectedTool.drawOverlay(ctx, x, y, hoveredCell.level);
                        }
                    }
                }
            }
            ctx.restore();
        },
        profilePaintEnd: function () {
            window.tileMapPaintProfile.end = performance.now();
            const duration = window.tileMapPaintProfile.end - window.tileMapPaintProfile.start;
            console.log(`[Profile] Tile paint roundtrip: ${duration.toFixed(2)} ms`);
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