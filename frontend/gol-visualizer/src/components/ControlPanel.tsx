import React from 'react';
import './ControlPanel.css';

interface UniverseConfig {
  chunksX: number;
  chunksY: number;
  chunkSize: number;
  liveDensity: number;
}

interface ControlPanelProps {
  config: UniverseConfig;
  setConfig: React.Dispatch<React.SetStateAction<UniverseConfig>>;
  onInit: () => void;
  onReinit: () => void;
  onStep: () => void;
  onAutoRun: () => void;
  isRunning: boolean;
  isInitialized: boolean;
  autoRunInterval: number;
  setAutoRunInterval: (interval: number) => void;
  generation: number;
}

const ControlPanel: React.FC<ControlPanelProps> = ({
  config,
  setConfig,
  onInit,
  onReinit,
  onStep,
  onAutoRun,
  isRunning,
  isInitialized,
  autoRunInterval,
  setAutoRunInterval,
  generation
}) => {
  const intervalOptions = [
    { value: 10, label: '10ms (Very Fast)' },
    { value: 50, label: '50ms (Fast)' },
    { value: 100, label: '100ms (Normal)' },
    { value: 500, label: '500ms (Slow)' },
    { value: 1000, label: '1000ms (Very Slow)' }
  ];

  return (
    <div className="control-panel">
      <div className="config-section">
        <h2>Universe Configuration</h2>

        <div className="form-group">
          <label htmlFor="chunksX">Chunks X:</label>
          <input
            id="chunksX"
            type="number"
            min="1"
            max="10"
            value={config.chunksX}
            onChange={(e) => setConfig({ ...config, chunksX: parseInt(e.target.value) || 1 })}
            disabled={isRunning}
          />
        </div>

        <div className="form-group">
          <label htmlFor="chunksY">Chunks Y:</label>
          <input
            id="chunksY"
            type="number"
            min="1"
            max="10"
            value={config.chunksY}
            onChange={(e) => setConfig({ ...config, chunksY: parseInt(e.target.value) || 1 })}
            disabled={isRunning}
          />
        </div>

        <div className="form-group">
          <label htmlFor="chunkSize">Chunk Size:</label>
          <input
            id="chunkSize"
            type="number"
            min="8"
            max="128"
            step="8"
            value={config.chunkSize}
            onChange={(e) => setConfig({ ...config, chunkSize: parseInt(e.target.value) || 8 })}
            disabled={isRunning}
          />
        </div>

        <div className="form-group">
          <label htmlFor="liveDensity">Live Density:</label>
          <input
            id="liveDensity"
            type="number"
            min="0"
            max="1"
            step="0.05"
            value={config.liveDensity}
            onChange={(e) => setConfig({ ...config, liveDensity: parseFloat(e.target.value) || 0 })}
            disabled={isRunning}
          />
          <span className="helper-text">{(config.liveDensity * 100).toFixed(0)}%</span>
        </div>

        <div className="universe-info">
          <p>Total Grid: {config.chunksX * config.chunkSize} × {config.chunksY * config.chunkSize}</p>
          <p>Total Cells: {config.chunksX * config.chunkSize * config.chunksY * config.chunkSize}</p>
          <p>Total Chunks: {config.chunksX * config.chunksY}</p>
        </div>

        {!isInitialized ? (
          <button 
            className="btn btn-primary btn-init" 
            onClick={onInit}
            disabled={isRunning}
          >
            Initialize Universe
          </button>
        ) : (
          <button 
            className="btn btn-warning btn-init" 
            onClick={onReinit}
            disabled={isRunning}
            title="Stops auto-run and re-initializes with current configuration"
          >
            Re-Initialize Universe
          </button>
        )}
      </div>

      <div className="control-section">
        <h2>Simulation Controls</h2>

        <div className="stats">
          <div className="stat-item">
            <span className="stat-label">Generation:</span>
            <span className="stat-value">{generation}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Status:</span>
            <span className={`stat-value ${isRunning ? 'running' : 'paused'}`}>
              {isRunning ? 'Running' : 'Paused'}
            </span>
          </div>
        </div>

        <div className="button-group">
          <button 
            className="btn btn-secondary" 
            onClick={onStep}
            disabled={!isInitialized || isRunning}
          >
            Step Once
          </button>

          <button 
            className={`btn ${isRunning ? 'btn-danger' : 'btn-success'}`}
            onClick={onAutoRun}
            disabled={!isInitialized}
          >
            {isRunning ? 'Stop Auto-Run' : 'Start Auto-Run'}
          </button>
        </div>

        <div className="form-group">
          <label htmlFor="interval">Auto-Run Speed:</label>
          <select
            id="interval"
            value={autoRunInterval}
            onChange={(e) => setAutoRunInterval(parseInt(e.target.value))}
            disabled={!isInitialized}
          >
            {intervalOptions.map(option => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>

        {isRunning && (
          <div className="warning-message">
            <p>?? Auto-run is active. Re-Initialize will stop it automatically.</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default ControlPanel;
