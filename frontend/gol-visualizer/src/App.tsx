import React, { useState, useCallback, useRef, useEffect } from 'react';
import './App.css';
import GameOfLifeGrid from './components/GameOfLifeGrid';
import ControlPanel from './components/ControlPanel';
import { initUniverse, reinitUniverse, runStep, getGrid } from './services/api';

interface UniverseConfig {
  chunksX: number;
  chunksY: number;
  chunkSize: number;
  liveDensity: number;
}

function App() {
  const [grid, setGrid] = useState<boolean[][]>([]);
  const [isInitialized, setIsInitialized] = useState(false);
  const [isRunning, setIsRunning] = useState(false);
  const [generation, setGeneration] = useState(0);
  const [config, setConfig] = useState<UniverseConfig>({
    chunksX: 2,
    chunksY: 2,
    chunkSize: 32,
    liveDensity: 0.15
  });
  const [autoRunInterval, setAutoRunInterval] = useState<number>(100);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const stopAutoRun = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    setIsRunning(false);
  }, []);

  const handleInit = async () => {
    try {
      // Stop auto-run if it's running
      stopAutoRun();

      await initUniverse(config.chunksX, config.chunksY, config.chunkSize, config.liveDensity);
      const gridData = await getGrid();
      setGrid(gridData);
      setIsInitialized(true);
      setGeneration(0);
    } catch (error) {
      console.error('Failed to initialize universe:', error);
      alert('Failed to initialize universe. Make sure the API is running on http://localhost:5050');
    }
  };

  const handleReinit = async () => {
    try {
      // Stop auto-run if it's running
      stopAutoRun();

      await reinitUniverse(config.chunksX, config.chunksY, config.chunkSize, config.liveDensity);
      const gridData = await getGrid();
      setGrid(gridData);
      setIsInitialized(true);
      setGeneration(0);
    } catch (error) {
      console.error('Failed to re-initialize universe:', error);
      alert('Failed to re-initialize universe. Make sure the API is running on http://localhost:5050');
    }
  };

  const handleStep = useCallback(async () => {
    try {
      await runStep();
      const gridData = await getGrid();
      setGrid(gridData);
      setGeneration(prev => prev + 1);
    } catch (error) {
      console.error('Failed to run step:', error);
    }
  }, []);

  const handleAutoRun = () => {
    if (isRunning) {
      stopAutoRun();
    } else {
      setIsRunning(true);
      intervalRef.current = setInterval(handleStep, autoRunInterval);
    }
  };

  useEffect(() => {
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, []);

  useEffect(() => {
    if (isRunning && intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = setInterval(handleStep, autoRunInterval);
    }
  }, [autoRunInterval, isRunning, handleStep]);

  return (
    <div className="App">
      <header className="App-header">
        <h1>Conway's Game of Life - Distributed Orleans</h1>
        <p className="subtitle">Multi-Chunk Visualization</p>
      </header>

      <div className="main-content">
        <ControlPanel
          config={config}
          setConfig={setConfig}
          onInit={handleInit}
          onReinit={handleReinit}
          onStep={handleStep}
          onAutoRun={handleAutoRun}
          isRunning={isRunning}
          isInitialized={isInitialized}
          autoRunInterval={autoRunInterval}
          setAutoRunInterval={setAutoRunInterval}
          generation={generation}
        />

        <div className="grid-container">
          {isInitialized ? (
            <GameOfLifeGrid 
              grid={grid} 
              chunkSize={config.chunkSize}
              chunksX={config.chunksX}
              chunksY={config.chunksY}
            />
          ) : (
            <div className="placeholder">
              <p>Click "Initialize Universe" to start</p>
            </div>
          )}
        </div>
      </div>

      <footer className="App-footer">
        <p>Built with React + TypeScript + Orleans</p>
        <p>Generation: {generation}</p>
      </footer>
    </div>
  );
}

export default App;
