(function () {
    let mapCanvas;
    let ctx;
    let tileMaps = {};
    window.tileMapState = {
        cellSize: 32, // 32x32 grid
        mapSize: { width: 1, height: 1 },
        selectedTileId: null,
        dotNetRef: null,
        selectedCells: [],
        tilePalette: [],
        activeLayer: 0,
        origin: { x: 0, y: 0 },
        scale: 1,
        minScale: 0.2,
        maxScale: 4,
        isMouseOver: false
    };
    let running = false;
    let hoveredCell = { x: -1, y: -1, layer: 0 };
    let showGrid = true;

    let shimPositions = {
        leadingEdge: null,
        leadingEdgeCapped: null,
        center: null,
        centerCapped: null,
        centerBothEdge: null,
        centerBothEdgeCapped: null,
        trailingEdge: null,
        trailingEdgeCapped: null,
    }
    let paletteRoles = {
        floors: [],
        shimRuns: [],
        shimCapMasks: [],
        shimOverhangMasks: [],
        complete: false
    };

    // default tool is paint
    let selectedTool = window.tools.paint;

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
        e.preventDefault(); // Prevent page scroll when zooming on canvas
        var pos = getScaledMousePosition(e);
        // Mouse position in world coords before zoom
        const worldX = window.tileMapState.origin.x + pos.x / window.tileMapState.scale;
        const worldY = window.tileMapState.origin.y + pos.y / window.tileMapState.scale;
        // Adjust scale
        const zoomFactor = 1.1;
        let newScale = window.tileMapState.scale;
        if (e.deltaY < 0) {
            newScale = Math.min(window.tileMapState.scale * zoomFactor, window.tileMapState.maxScale);
        } else {
            newScale = Math.max(window.tileMapState.scale / zoomFactor, window.tileMapState.minScale);
        }
        // Adjust origin so the world point under the mouse stays under the mouse
        window.tileMapState.origin.x = worldX - pos.x / newScale;
        window.tileMapState.origin.y = worldY - pos.y / newScale;
        window.tileMapState.scale = newScale;
    }

    function blendBaseOverShim(baseImg, shimImg, maskImg) {
        const width = shimImg.width;
        const height = shimImg.height;
        // Step 1: Draw shim (dirt) as background
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        ctx.drawImage(shimImg, 0, 0, width, height);
        // Step 2: Prepare grass with mask
        const grassCanvas = document.createElement('canvas');
        grassCanvas.width = baseImg.width;
        grassCanvas.height = baseImg.height;
        const grassCtx = grassCanvas.getContext('2d');
        grassCtx.drawImage(baseImg, 0, 0, shimImg.width, baseImg.height);
        grassCtx.globalCompositeOperation = 'destination-in';
        grassCtx.drawImage(maskImg, 0, 0, shimImg.width, maskImg.height);
        grassCtx.globalCompositeOperation = 'source-over';
        // Step 3: Draw masked grass over dirt
        ctx.drawImage(grassCanvas, 0, 0, shimImg.width, baseImg.height);
        return canvas;
    }

    function mirrorMask(maskImg) {
        const width = maskImg.width;
        const height = maskImg.height;
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        ctx.save();
        ctx.translate(width, height);
        ctx.scale(-1, -1);
        ctx.drawImage(maskImg, 0, 0, width, height);
        ctx.restore();
        return canvas;
    }

    // Applies a cap mask to a shim image, making only the left edge transparent
    function applyCapMask(shimImg, capMaskImg) {
        const width = shimImg.width;
        const height = shimImg.height;
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        // Draw shim image
        ctx.drawImage(shimImg, 0, 0, width, height);

        // Create a mask canvas the same size as shimImg
        const maskCanvas = document.createElement('canvas');
        maskCanvas.width = width;
        maskCanvas.height = height;
        const maskCtx = maskCanvas.getContext('2d');
        // Fill mask with opaque white (full alpha)
        maskCtx.fillStyle = 'white';
        maskCtx.fillRect(32, 0, width, height);
        // Draw cap mask at the left edge (0,0)
        maskCtx.drawImage(capMaskImg, 0, 0, capMaskImg.width, capMaskImg.height);
        // Apply mask to shim image
        ctx.globalCompositeOperation = 'destination-in';
        ctx.drawImage(maskCanvas, 0, 0, width, height);
        ctx.globalCompositeOperation = 'source-over';
        return canvas;
    }

    function blendBaseOverShimBothEdges(baseImg, shimImg, maskImg) {
        // Top edge
        let canvas = blendBaseOverShim(baseImg, shimImg, maskImg);
        // Bottom edge
        let flippedMask = mirrorMask(maskImg);
        canvas = blendBaseOverShim(baseImg, canvas, flippedMask);
        return canvas;
    }

    function loadImageAsync(img) {
        return new Promise((resolve, reject) => {
            if (img.complete) {
                resolve(img);
            } else {
                img.onload = () => resolve(img);
                img.onerror = () => reject(new Error('Image load error'));
            }
        });
    }
    async function analyzePalette(tilePalette) {
        const roles = {
            floors: [],
            shimRuns: [],
            shimCapMasks: [],
            shimOverhangMasks: [],
            complete: false
        };

        for (let tileId in tilePalette) {
            const tile = tilePalette[tileId];
            if (!tile) continue;
            if (!tile.layout) {
                let img = new window.Image();
                img.src = tile.image;
                tile.layout = {
                    image: img
                }
                await loadImageAsync(img);
                tile.layout.isLoaded = true;
            }
            if (tile.role === blazeditor.tileRole.floor) {
                roles.floors.push(tile);
            } else if (tile.role === blazeditor.tileRole.shim) {
                if (tile.properties && tile.properties.ShimType) {
                    if (tile.properties.ShimType === blazeditor.shimType.run) {
                        roles.shimRuns.push(tile);
                    } else if (tile.properties.ShimType === blazeditor.shimType.capMask) {
                        roles.shimCapMasks.push(tile);
                    } else if (tile.properties.ShimType === blazeditor.shimType.overhangMask) {
                        roles.shimOverhangMasks.push(tile);
                    }
                }
            }
        }
        computeShims(roles);
        roles.complete = true;
        return roles;
    }

    function computeShims(roles) {
        // Precompute horizontal non-capped variants if images are loaded
        if (
            roles.floors.length > 0 &&
            roles.shimRuns.length > 0 &&
            roles.shimOverhangMasks.length > 0 &&
            roles.shimCapMasks.length > 0 &&
            roles.floors[0].layout.image.complete &&
            roles.shimRuns[0].layout.image.complete &&
            roles.shimOverhangMasks[0].layout.image.complete &&
            roles.shimCapMasks[0].layout.image.complete
        ) {
            const baseImg = roles.floors[0].layout.image;
            const shimImg = roles.shimRuns[0].layout.image;
            const maskImg = roles.shimOverhangMasks[0].layout.image;
            const capMaskImg = roles.shimCapMasks[0].layout.image;
            const size = { width: shimImg.width / 32, height: shimImg.height / 32 };
            // leadingEdge: mask at top
            shimPositions.leadingEdge = {
                layout: { image: blendBaseOverShim(baseImg, shimImg, maskImg), isLoaded: true },
                size: size
            };
            shimPositions.leadingEdgeCapped = {
                layout: { image: applyCapMask(shimPositions.leadingEdge.layout.image, capMaskImg), isLoaded: true },
                size: size
            };
            // center: no mask
            shimPositions.center = {
                layout: { image: shimImg, isLoaded: true },
                size: size
            };
            shimPositions.centerCapped = {
                layout: { image: applyCapMask(shimPositions.center.layout.image, capMaskImg), isLoaded: true },
                size: size
            };
            // trailingEdge: mask at bottom (flipped)
            const flippedMask = mirrorMask(maskImg);
            shimPositions.trailingEdge = {
                layout: { image: blendBaseOverShim(baseImg, shimImg, flippedMask), isLoaded: true },
                size: size
            };
            shimPositions.trailingEdgeCapped = {
                layout: { image: applyCapMask(shimPositions.trailingEdge.layout.image, capMaskImg), isLoaded: true },
                size: size
            };
            // centerBothEdge: mask at both top and bottom
            shimPositions.centerBothEdge = {
                layout: { image: blendBaseOverShimBothEdges(baseImg, shimImg, maskImg), isLoaded: true },
                size: size
            };
            shimPositions.centerBothEdgeCapped = {
                layout: { image: applyCapMask(shimPositions.centerBothEdge.layout.image, capMaskImg), isLoaded: true },
                size: size
            };
        }
    }
    function drawHorizontalRun(baseTile, x, y, offset, diff, isCapped) {
        let runTile = null;
        // need to use capped runs for the left edge
        for (let z = 0; z < diff; z++) {

            // get the shim tile for this step
            runTile = getShimTileFor(z, diff, isCapped);

            ctx.drawImage(
                runTile.layout.image,
                x * window.tileMapState.cellSize + offset + (z * 32),
                y * window.tileMapState.cellSize + offset + (z * 32) + (baseTile.size.height * 64),
                runTile.size.width * window.tileMapState.cellSize,
                runTile.size.height * window.tileMapState.cellSize);
        }
    }

    function getShimTileFor(index, diff, isCapped) {
        let runTile = null;
        if (index == 0 && index == diff - 1) {
            // there's only one level of elevation change, so there should be an overhang & underhang
            runTile = isCapped ? shimPositions.centerBothEdgeCapped : shimPositions.centerBothEdge;
        } else if (index == 0) {
            // first step, so only the overhang
            runTile = isCapped ? shimPositions.leadingEdgeCapped : shimPositions.leadingEdge;
        } else if (index == diff - 1) {
            // last step, so only the underhang
            runTile = isCapped ? shimPositions.trailingEdgeCapped : shimPositions.trailingEdge;
        } else {
            // middle steps, so just the center
            runTile = isCapped ? shimPositions.centerCapped : shimPositions.center;
        }
        return runTile;
    }

    function drawVerticalRun(baseTile, x, y, offset, diff, isCapped) {
        let runTile = null;

        // draw a vertical run tile for every elevation step, offsetting by 32px for each step
        for (let z = 0; z < diff; z++) {

            // get the shim tile for this step
            runTile = getShimTileFor(z, diff, isCapped);

            // Calculate the center of the gap to the right of the tile
            const drawX = x * window.tileMapState.cellSize + offset + (baseTile.size.width * 64) + (runTile.size.height * 32 / 2) + (z * 32);
            const drawY = y * window.tileMapState.cellSize + offset + (runTile.size.width * 32 / 2) + (z * 32);
            ctx.save();
            ctx.translate(drawX, drawY); // Move origin to center of where we want the image
            ctx.rotate(Math.PI / 2);     // Rotate 90 degrees clockwise
            ctx.scale(1, -1);            // Mirror horizontally
            // Draw image centered at the origin
            ctx.drawImage(
                runTile.layout.image,
                -runTile.size.width * 32 / 2, // Offset by half width (after rotation)
                -runTile.size.height * 32 / 2,  // Offset by half height (after rotation)
                runTile.size.width * 32,
                runTile.size.height * 32
            );
            ctx.restore();
        }
    }
    function getFloorTileAt(x, y) {
        let placement = tileMaps[0].tilePlacements.find(p => p && p.x === x && p.y === y && p.tileId !== null);
        if (placement) {
            let tile = placement ? window.tileMapState.tilePalette[placement.tileId] : null;
            if (tile && tile.role === blazeditor.tileRole.floor) {
                return placement;
            }
        }
        return null;
    }
    function updateFloorTileLayout() {

        for (let y = 0; y < window.tileMapState.mapSize.height; y++) {
            for (let x = 0; x < window.tileMapState.mapSize.width; x++) {
                let placement = tileMaps[0].tilePlacements[y * window.tileMapState.mapSize.width + x]; // Get the placement for the first layer
                if (!placement || placement.tileId == null) continue;
                if (!placement.layout) {
                    placement.layout = {};
                }
                if (!placement.layout.nextFloorLeft) {
                    for (let x1 = x - 1; x1 >= 0; x1--) {
                        let nextFloorLeft = getFloorTileAt(x1, y);
                        if (nextFloorLeft) {
                            placement.layout.nextFloorLeft = nextFloorLeft;
                            break;
                        }
                    }
                }
                if (!placement.layout.nextFloorRight) {
                    for (let x1 = x + 1; x1 < window.tileMapState.mapSize.width; x1++) {
                        let nextFloorRight = getFloorTileAt(x1, y);
                        if (nextFloorRight) {
                            placement.layout.nextFloorRight = nextFloorRight;
                            break;
                        }
                    }
                }

                if (!placement.layout.nextFloorAbove) {
                    for (let y1 = y - 1; y1 >= 0; y1--) {
                        let nextFloorAbove = getFloorTileAt(x, y1);
                        if (nextFloorAbove) {
                            placement.layout.nextFloorAbove = nextFloorAbove;
                            break;
                        }
                    }
                }

                if (!placement.layout.nextFloorBelow) {
                    for (let y1 = y + 1; y1 < window.tileMapState.mapSize.height; y1++) {
                        let nextFloorBelow = getFloorTileAt(x, y1);
                        if (nextFloorBelow) {
                            placement.layout.nextFloorBelow = nextFloorBelow;
                            break;
                        }
                    }
                }
            }
        }
    }

    window.tileMapCanvas = {
        init: async function (canvas, tileMapsArg, cellSizeArg, mapSizeArg, tilePaletteArg) {
            mapCanvas = canvas;
            ctx = mapCanvas.getContext('2d');
            mapCanvas.onmousemove = this.mousemove;
            mapCanvas.onclick = this.click;
            mapCanvas.addEventListener('wheel', handleWheel, { passive: false });
            mapCanvas.addEventListener('mousedown', this.mousedown);
            mapCanvas.addEventListener('mouseup', this.mouseup);
            mapCanvas.addEventListener('mouseleave', this.mouseleave);
            mapCanvas.addEventListener('keydown', this.keydown);
            if (mapSizeArg) {
                window.tileMapState.mapSize = mapSizeArg;
            }
            // Set the default cursor for the canvas
            if (mapCanvas) {
                mapCanvas.style.cursor = selectedTool.cursor;
            }
            if (tilePaletteArg) {
                window.tileMapState.tilePalette = tilePaletteArg;
                paletteRoles = await analyzePalette(window.tileMapState.tilePalette);
            }
            if (tileMapsArg) {
                tileMaps = tileMapsArg;
                updateFloorTileLayout();
            }
            if (cellSizeArg) {
                window.tileMapState.cellSize = 32; // Always use 32 for placement
            }



            running = false;
            this.startRenderLoop();
        },
        updateTileMaps: function (val) {
            if (val) {
                tileMaps = val;
                updateFloorTileLayout();
            }
        },
        updateTilePalette: async function (val) {
            if (val) {
                window.tileMapState.tilePalette = val;
                paletteRoles = await analyzePalette(window.tileMapState.tilePalette);
                updateFloorTileLayout();
            }
        },
        updateTilePositions: function (positions) {
            if (!positions) return;
            let floorTileUpdate = false;
            let tilePalette = window.tileMapState.tilePalette;
            if (positions.length) {
                positions.forEach(pos => {
                    let map = tileMaps[pos.layer];
                    if (Array.isArray(map.tilePlacements)) {
                        map.tilePlacements[pos.y * window.tileMapState.mapSize.width + pos.x] = pos;
                    }
                    floorTileUpdate = floorTileUpdate || pos.elevation > 0 && pos.layer === 0 && pos.tileId != null && tilePalette[pos.tileId].role === blazeditor.tileRole.floor;
                });
            } else {
                let pos = positions;
                let map = tileMaps[pos.layer];
                if (Array.isArray(map.tilePlacements)) {
                    map.tilePlacements[pos.y * window.tileMapState.mapSize.width + pos.x] = pos;
                }
                floorTileUpdate = pos.elevation > 0 && pos.layer === 0 && pos.tileId != null && tilePalette[pos.tileId].role === blazeditor.tileRole.floor;
            }
            if (floorTileUpdate) {
                updateFloorTileLayout();
            }
        },
        setShowGrid: function (val) {
            showGrid = val;
        },
        setActiveLayer: function (layer) {
            window.tileMapState.activeLayer = layer;
            hoveredCell.layer = layer; // Update hovered cell layer
        },
        selectTool(toolId) {
            selectedTool = tools[toolId] || tools.paint;
            // Set the canvas cursor style
            if (mapCanvas && selectedTool.cursor) {
                mapCanvas.style.cursor = selectedTool.cursor;
            }
        },
        setSelectedTileId: function (val) {
            window.tileMapState.selectedTileId = val;
        },
        setDotNetRef: function (ref) {
            window.tileMapState.dotNetRef = ref;
        },
        keydown: function (e) {
            if (selectedTool.keydown) {
                selectedTool.keydown(e);
            }
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
            window.tileMapState.isMouseOver = false;
        },
        mousemove: function (e) {
            var pos = getScaledMousePosition(e);
            // Convert to world coordinates
            const worldX = window.tileMapState.origin.x + pos.x / window.tileMapState.scale;
            const worldY = window.tileMapState.origin.y + pos.y / window.tileMapState.scale;
            hoveredCell.x = Math.floor(worldX / window.tileMapState.cellSize);
            hoveredCell.y = Math.floor(worldY / window.tileMapState.cellSize);
            hoveredCell.layer = window.tileMapState.activeLayer;

            if (selectedTool.mousemove) {
                selectedTool.mousemove(pos);
            }
            window.tileMapState.isMouseOver = true;

        },
        click: function (e) {
            var pos = getScaledMousePosition(e);
            // Convert to world coordinates
            const worldX = window.tileMapState.origin.x + pos.x / window.tileMapState.scale;
            const worldY = window.tileMapState.origin.y + pos.y / window.tileMapState.scale;
            const x = Math.floor(worldX / window.tileMapState.cellSize);
            const y = Math.floor(worldY / window.tileMapState.cellSize);
            if (selectedTool.click) {
                selectedTool.click(x, y, window.tileMapState.activeLayer, e);
            }
        },
        drawTiles: function () {
            if (!mapCanvas || !ctx) return;
            ctx.clearRect(0, 0, mapCanvas.width, mapCanvas.height);

            ctx.save();
            ctx.scale(window.tileMapState.scale, window.tileMapState.scale);
            ctx.translate(-window.tileMapState.origin.x, -window.tileMapState.origin.y);
            // Draw selection highlight (fill only, no border)
            if (window.tileMapState.selectedCells.length > 0) {
                ctx.save();
                ctx.globalAlpha = 0.3;
                ctx.fillStyle = '#007bff';
                window.tileMapState.selectedCells.forEach(cell => {
                    ctx.fillRect(cell.x * window.tileMapState.cellSize, cell.y * window.tileMapState.cellSize, window.tileMapState.cellSize, window.tileMapState.cellSize);
                });
                ctx.restore();
            }
            // Draw tiles (from TilePlacements)
            for (let map of Object.values(tileMaps)) {
                const tilePlacements = map.tilePlacements;
                if (!tilePlacements || !tilePlacements.length) continue;
                for (let i = 0; i < tilePlacements.length; i++) {
                    const placement = tilePlacements[i];
                    if (!placement || placement.tileId == null) continue;
                    placement.layout = placement.layout || {};
                    const x = placement.x;
                    const y = placement.y;
                    const tile = window.tileMapState.tilePalette[placement.tileId];
                    if (tile && tile.image && tile.image.startsWith('data:image') && tile.layout.isLoaded) {
                        const offset = placement.elevation * -32; // Elevation offset, assuming 32px per elevation step
                        // Draw at 32x32 grid
                        ctx.filter = 'contrast(0.8) brightness(0.8)';
                        if (offset != 0) {
                            const el = -(offset / 32);
                            const bright = Math.max(0.2, 0.8 + (el * 0.05)); // Increase brightness based on elevation
                            const contrast = Math.max(0.2, 0.8 + (el * 0.1)); // Increase contrast based on elevation
                            ctx.filter = `brightness(${bright}) contrast(${contrast})`; // Dim the tile if it has elevation
                        }

                        ctx.drawImage(tile.layout.image, x * window.tileMapState.cellSize + offset, y * window.tileMapState.cellSize + offset, tile.size.width * 64, tile.size.height * 64); // TODO: The cell size should be read from the palette instead of hard-coded to 64
                        ctx.filter = 'none'; // Reset filter for next tiles
                        if (offset != 0) {

                            const isCappedLeft = (placement.layout.nextFloorLeft ? placement.elevation - placement.layout.nextFloorLeft.elevation : 0) > 0;
                            const isCappedTop = (placement.layout.nextFloorAbove ? placement.elevation - placement.layout.nextFloorAbove.elevation : 0) > 0;

                            const verticalDifference = Math.max(0, placement.layout.nextFloorBelow ? placement.elevation - placement.layout.nextFloorBelow.elevation : 0);
                            const horizontalDifference = Math.max(0, placement.layout.nextFloorRight ? placement.elevation - placement.layout.nextFloorRight.elevation : 0);

                            // Draw horizontal run below the tile
                            if (verticalDifference) {
                                // Draw horizontal run to the left of the tile
                                drawHorizontalRun(tile, x, y, offset, verticalDifference, isCappedLeft);
                            }
                            if (horizontalDifference) {
                                // Draw vertical run to the right of the tile
                                drawVerticalRun(tile, x, y, offset, horizontalDifference, isCappedTop);
                            }
                        }
                    }
                }
            }

            for (let y = 0; y < window.tileMapState.mapSize.height; y++) {
                for (let x = 0; x < window.tileMapState.mapSize.width; x++) {
                    // Draw a border around the whole grid (once, not per cell)
                    if (x === 0 && y === 0) {
                        ctx.save();
                        ctx.strokeStyle = '#000';
                        ctx.lineWidth = 2;
                        ctx.strokeRect(0, 0, window.tileMapState.mapSize.width * window.tileMapState.cellSize, window.tileMapState.mapSize.height * window.tileMapState.cellSize);
                        ctx.restore();
                    }
                    if (showGrid) {
                        // Draw grid
                        ctx.strokeStyle = '#88888888';
                        ctx.lineWidth = 1;
                        ctx.strokeRect(x * window.tileMapState.cellSize, y * window.tileMapState.cellSize, window.tileMapState.cellSize, window.tileMapState.cellSize);
                        // Highlight hovered cell
                        if (window.tileMapState.isMouseOver && x === hoveredCell.x && y === hoveredCell.y) {
                            ctx.save();
                            ctx.strokeStyle = '#ff0';
                            ctx.strokeRect(x * window.tileMapState.cellSize, y * window.tileMapState.cellSize, window.tileMapState.cellSize, window.tileMapState.cellSize);
                            ctx.restore();
                        }
                    }
                    // Draw tile overlay for the hovered cell
                    if (selectedTool.drawOverlay && x === hoveredCell.x && y === hoveredCell.y) {
                        selectedTool.drawOverlay(ctx, x, y, hoveredCell.layer);
                    }
                }
            }
            ctx.restore();
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
