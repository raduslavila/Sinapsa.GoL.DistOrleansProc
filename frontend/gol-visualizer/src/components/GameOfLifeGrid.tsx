import React, { useEffect, useMemo, useRef, useState } from 'react';
import './GameOfLifeGrid.css';

interface GameOfLifeGridProps {
  grid: boolean[][];
  chunkSize: number;
  chunksX: number;
  chunksY: number;
}

const MAX_GRID_SIZE = 600;
const MIN_CELL_SIZE = 2;
const MAX_CELL_SIZE = 20;
const GRID_LINE_COLOR = 'rgba(200, 200, 200, 0.3)';
const CHUNK_BORDER_COLOR = '#ff6b6b';
const ALIVE_CELL_COLOR = '#667eea';
const DEAD_CELL_COLOR = '#ffffff';

const GameOfLifeGrid: React.FC<GameOfLifeGridProps> = ({ grid, chunkSize, chunksX, chunksY }) => {
  const viewportRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [availableWidth, setAvailableWidth] = useState(MAX_GRID_SIZE);
  const rows = grid?.length || 0;
  const cols = grid[0]?.length || 0;

  const livingCells = useMemo(
    () => grid.reduce((count, row) => count + row.filter(Boolean).length, 0),
    [grid]
  );

  useEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport) {
      return;
    }

    const updateAvailableWidth = () => {
      setAvailableWidth(Math.max(viewport.clientWidth, MIN_CELL_SIZE));
    };

    updateAvailableWidth();

    if (typeof ResizeObserver === 'undefined') {
      window.addEventListener('resize', updateAvailableWidth);
      return () => window.removeEventListener('resize', updateAvailableWidth);
    }

    const observer = new ResizeObserver(() => updateAvailableWidth());
    observer.observe(viewport);

    return () => observer.disconnect();
  }, []);

  const maxDimension = Math.max(rows, cols);
  const targetGridSize = Math.min(MAX_GRID_SIZE, Math.max(availableWidth, MIN_CELL_SIZE * maxDimension));
  const cellSize = Math.max(
    MIN_CELL_SIZE,
    Math.min(MAX_CELL_SIZE, Math.floor(targetGridSize / Math.max(maxDimension, 1)))
  );
  const gridWidth = cols * cellSize;
  const gridHeight = rows * cellSize;

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || cols === 0 || rows === 0) {
      return;
    }

    const context = canvas.getContext('2d');
    if (!context) {
      return;
    }

    const dpr = window.devicePixelRatio || 1;

    canvas.width = Math.max(1, Math.floor(gridWidth * dpr));
    canvas.height = Math.max(1, Math.floor(gridHeight * dpr));
    canvas.style.width = `${gridWidth}px`;
    canvas.style.height = `${gridHeight}px`;

    context.setTransform(1, 0, 0, 1, 0, 0);
    context.clearRect(0, 0, canvas.width, canvas.height);
    context.setTransform(dpr, 0, 0, dpr, 0, 0);

    context.fillStyle = DEAD_CELL_COLOR;
    context.fillRect(0, 0, gridWidth, gridHeight);

    for (let rowIndex = 0; rowIndex < rows; rowIndex += 1) {
      for (let colIndex = 0; colIndex < cols; colIndex += 1) {
        if (!grid[rowIndex][colIndex]) {
          continue;
        }

        const x = colIndex * cellSize;
        const y = rowIndex * cellSize;

        context.fillStyle = ALIVE_CELL_COLOR;
        context.fillRect(x, y, cellSize, cellSize);

        if (cellSize > 3) {
          context.fillStyle = 'rgba(102, 126, 234, 0.15)';
          context.fillRect(x + 1, y + 1, Math.max(cellSize - 2, 1), Math.max(cellSize - 2, 1));
        }
      }
    }

    context.strokeStyle = GRID_LINE_COLOR;
    context.lineWidth = 1;
    context.beginPath();

    for (let rowIndex = 1; rowIndex < rows; rowIndex += 1) {
      const y = rowIndex * cellSize + 0.5;
      context.moveTo(0, y);
      context.lineTo(gridWidth, y);
    }

    for (let colIndex = 1; colIndex < cols; colIndex += 1) {
      const x = colIndex * cellSize + 0.5;
      context.moveTo(x, 0);
      context.lineTo(x, gridHeight);
    }

    context.stroke();

    context.strokeStyle = CHUNK_BORDER_COLOR;
    context.lineWidth = 2;
    context.beginPath();

    for (let rowIndex = chunkSize; rowIndex < rows; rowIndex += chunkSize) {
      const y = rowIndex * cellSize;
      context.moveTo(0, y);
      context.lineTo(gridWidth, y);
    }

    for (let colIndex = chunkSize; colIndex < cols; colIndex += chunkSize) {
      const x = colIndex * cellSize;
      context.moveTo(x, 0);
      context.lineTo(x, gridHeight);
    }

    context.stroke();
  }, [cellSize, chunkSize, cols, grid, gridHeight, gridWidth, rows]);

  if (!grid || rows === 0 || cols === 0) {
    return <div className="grid-empty">No data</div>;
  }

  return (
    <div className="grid-wrapper">
      <div ref={viewportRef} className="game-grid-viewport">
        <canvas
          ref={canvasRef}
          className="game-grid-canvas"
          aria-label={`Game of Life grid with ${rows} rows and ${cols} columns`}
        />
      </div>

      <div className="grid-info">
        <p>Grid Size: {rows} x {cols} cells</p>
        <p>Chunk Configuration: {chunksX} x {chunksY} chunks ({chunkSize}x{chunkSize} each)</p>
        <p>Living Cells: {livingCells}</p>
      </div>
    </div>
  );
};

export default GameOfLifeGrid;
