// Helper functions for tile selection
function cellsEqual(a, b) {
    return a.x === b.x && a.y === b.y && a.layer === b.layer;
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
            cells.push({ x, y, layer: a.layer });
        }
    }
    return cells;
}

// Tools object
const tools = {
    paint: (function () {
        let active = false;
        return {
            cursor: "url('/cursors/paint.png'), auto",
            click: (x, y, layer) => {
                if (window.tileMapState.dotNetRef && window.tileMapState.selectedTileId !== null) {
                    window.tileMapState.dotNetRef.invokeMethodAsync('OnJsPlaceTile', x, y, layer);
                }
            },
            drawOverlay: (ctx, x, y, layer) => {
                if (active && window.tileMapState.selectedTileId !== null && window.tileMapState.tilePalette) {
                    const tile = window.tileMapState.tilePalette[window.tileMapState.selectedTileId];
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
                            ctx.drawImage(tile._overlayImage, x * window.tileMapState.cellSize, y * window.tileMapState.cellSize, tile.size.width * 64, tile.size.height * 64);
                            ctx.restore();
                        }
                    }
                }
            },
            mousemove: () => {
                active = true;
            },
            mouseleave: () => {
                active = false;
            }
        }
    })(),
    select: {
        cursor: "url('/cursors/select.png'), auto",
        click: (x, y, layer, e) => {
            const cell = { x, y, layer };
            if (e && e.shiftKey) {
                if (window.tileMapState.selectedCells.length > 0) {
                    const last = window.tileMapState.selectedCells[window.tileMapState.selectedCells.length - 1];
                    const rectCells = getRectCells(last, cell);
                    rectCells.forEach(rc => {
                        if (!cellInCollection(rc, window.tileMapState.selectedCells)) {
                            window.tileMapState.selectedCells.push(rc);
                        }
                    });
                } else {
                    window.tileMapState.selectedCells.push(cell);
                }
            } else if (e && e.ctrlKey) {
                const idx = window.tileMapState.selectedCells.findIndex(c => cellsEqual(c, cell));
                if (idx === -1) {
                    window.tileMapState.selectedCells.push(cell);
                } else {
                    window.tileMapState.selectedCells.splice(idx, 1);
                }
            } else {
                window.tileMapState.selectedCells = [cell];
            }
            if (window.tileMapState.dotNetRef) {
                window.tileMapState.dotNetRef.invokeMethodAsync('OnJsSelectionChanged', window.tileMapState.selectedCells);
            }
        },
        keydown: (e) => {
            if (e.key === 'Escape') {
                window.tileMapState.selectedCells = [];
                if (window.tileMapState.dotNetRef) {
                    window.tileMapState.dotNetRef.invokeMethodAsync('OnJsSelectionChanged', window.tileMapState.selectedCells);
                }
            }
        }
    },
    elevate: {
        click: (x, y, layer, e) => {
            if (window.tileMapState.dotNetRef) {
                if (e.shiftKey) {
                    window.tileMapState.dotNetRef.invokeMethodAsync('OnJsLowerTile', x, y, layer);
                } else {
                    window.tileMapState.dotNetRef.invokeMethodAsync('OnJsRaiseTile', x, y, layer);
                }
            }
        }
    },
    fill: {
        cursor: "url('/cursors/fill.png'), auto",
        click: (x, y, layer, e) => {
            if (window.tileMapState.dotNetRef) {
                const ctrlKey = e && e.ctrlKey;
                window.tileMapState.dotNetRef.invokeMethodAsync('OnJsFill', x, y, layer, ctrlKey);
            }
        }
    },
    erase: {
        cursor: "url('/cursors/erase.png'), auto",
        click: (x, y, layer) => {
            if (window.tileMapState.dotNetRef) {
                window.tileMapState.dotNetRef.invokeMethodAsync('OnJsRemoveTile', x, y, layer);
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
                    window.tileMapState.origin.x += (panState.mousePos.x - pos.x) / window.tileMapState.scale;
                    window.tileMapState.origin.y += (panState.mousePos.y - pos.y) / window.tileMapState.scale;
                    panState.mousePos.x = pos.x;
                    panState.mousePos.y = pos.y;
                }
            },
            mouseup: () => { panState.active = false; },
            mouseleave: () => { panState.active = false; }
        };
    })()
};

window.tools = tools;

