import React from 'react';
import './GameOfLifeGrid.css';

interface GameOfLifeGridProps {
  grid: boolean[][];
  chunkSize: number;
  chunksX: number;
  chunksY: number;
}

const GameOfLifeGrid: React.FC<GameOfLifeGridProps> = ({ grid, chunkSize, chunksX, chunksY }) => {
  if (!grid || grid.length === 0) {
    return <div className="grid-empty">No data</div>;
  }

  const rows = grid.length;
  const cols = grid[0]?.length || 0;

  // Calculate cell size based on grid dimensions to fit the viewport
  const maxDimension = Math.max(rows, cols);
  const cellSize = Math.max(2, Math.min(20, Math.floor(600 / maxDimension)));

  return (
    <div className="grid-wrapper">
      <div 
        className="game-grid"
        style={{
          gridTemplateColumns: `repeat(${cols}, ${cellSize}px)`,
          gridTemplateRows: `repeat(${rows}, ${cellSize}px)`,
        }}
      >
        {grid.map((row, rowIndex) =>
          row.map((cell, colIndex) => {
            // Determine if this cell is on a chunk boundary
            const isChunkBoundaryX = colIndex > 0 && colIndex % chunkSize === 0;
            const isChunkBoundaryY = rowIndex > 0 && rowIndex % chunkSize === 0;

            return (
              <div
                key={`${rowIndex}-${colIndex}`}
                className={`cell ${cell ? 'alive' : 'dead'} 
                  ${isChunkBoundaryX ? 'chunk-border-x' : ''} 
                  ${isChunkBoundaryY ? 'chunk-border-y' : ''}`}
                style={{
                  width: `${cellSize}px`,
                  height: `${cellSize}px`,
                }}
              />
            );
          })
        )}
      </div>

      <div className="grid-info">
        <p>Grid Size: {rows} × {cols} cells</p>
        <p>Chunk Configuration: {chunksX} × {chunksY} chunks ({chunkSize}×{chunkSize} each)</p>
        <p>Living Cells: {grid.flat().filter(cell => cell).length}</p>
      </div>
    </div>
  );
};

export default GameOfLifeGrid;
