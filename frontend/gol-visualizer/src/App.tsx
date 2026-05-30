import React, { useState, useCallback, useRef, useEffect } from 'react';
import './App.css';
import GameOfLifeGrid from './components/GameOfLifeGrid';
import ControlPanel from './components/ControlPanel';
import { getGridUpdate, initUniverse, reinitUniverse, runStep, UniverseGridUpdateDto } from './services/api';

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
  const autoRunTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const generationRef = useRef(0);
  const isStepInFlightRef = useRef(false);

  const applyUpdate = useCallback((update: UniverseGridUpdateDto) => {
    if (update.isFullGrid || !grid.length || update.grid) {
      if (update.grid?.cells) {
        setGrid(update.grid.cells);
      }
      setGeneration(update.currentGeneration);
      return;
    }

    if (update.deltas.length > 0) {
      setGrid(prev => {
        const next = prev.map(column => [...column]);
        for (const delta of update.deltas) {
          if (next[delta.x] && next[delta.x][delta.y] !== undefined) {
            next[delta.x][delta.y] = delta.isAlive;
          }
        }
        return next;
      });
    }

    setGeneration(update.currentGeneration);
  }, [grid.length]);

  useEffect(() => {
    generationRef.current = generation;
  }, [generation]);

  const clearAutoRunTimeout = useCallback(() => {
    if (autoRunTimeoutRef.current) {
      clearTimeout(autoRunTimeoutRef.current);
      autoRunTimeoutRef.current = null;
    }
  }, []);

  const stopAutoRun = useCallback(() => {
    clearAutoRunTimeout();
    setIsRunning(false);
  }, [clearAutoRunTimeout]);

  const handleInit = async () => {
    try {
      // Stop auto-run if it's running
      stopAutoRun();

      await initUniverse(config.chunksX, config.chunksY, config.chunkSize, config.liveDensity);
      const update = await getGridUpdate(-1);
      applyUpdate(update);
      setIsInitialized(true);
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
      const update = await getGridUpdate(-1);
      applyUpdate(update);
      setIsInitialized(true);
    } catch (error) {
      console.error('Failed to re-initialize universe:', error);
      alert('Failed to re-initialize universe. Make sure the API is running on http://localhost:5050');
    }
  };

  const handleStep = useCallback(async () => {
    if (isStepInFlightRef.current) {
      return;
    }

    isStepInFlightRef.current = true;

    try {
      await runStep();
      const update = await getGridUpdate(generationRef.current);
      applyUpdate(update);
    } catch (error) {
      console.error('Failed to run step:', error);
      stopAutoRun();
    } finally {
      isStepInFlightRef.current = false;
    }
  }, [applyUpdate, stopAutoRun]);

  const handleAutoRun = () => {
    if (isRunning) {
      stopAutoRun();
    } else {
      setIsRunning(true);
    }
  };

  useEffect(() => {
    return () => {
      clearAutoRunTimeout();
    };
  }, [clearAutoRunTimeout]);

  useEffect(() => {
    clearAutoRunTimeout();

    if (!isRunning || !isInitialized || isStepInFlightRef.current) {
      return;
    }

    autoRunTimeoutRef.current = setTimeout(() => {
      void handleStep();
    }, autoRunInterval);

    return clearAutoRunTimeout;
  }, [autoRunInterval, clearAutoRunTimeout, generation, handleStep, isInitialized, isRunning]);

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
