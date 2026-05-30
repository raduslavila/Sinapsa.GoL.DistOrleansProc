# Game of Life - React Visualizer

A React + TypeScript frontend for visualizing Conway's Game of Life running on a distributed Orleans backend.

## Features

- **Real-time Grid Visualization**: Live updates of the Game of Life universe
- **Chunk Boundaries**: Visual indicators showing distributed chunk boundaries (red lines)
- **Configurable Universe**: 
  - Adjustable number of chunks (X and Y)
  - Variable chunk sizes (8-128 cells)
  - Configurable initial live density (0-100%)
- **Simulation Controls**:
  - Single-step execution
  - Auto-run with variable speed (10ms to 1000ms intervals)
  - Pause/Resume functionality
- **Live Statistics**:
  - Current generation count
  - Grid dimensions
  - Living cell count
  - Chunk configuration

## Prerequisites

- Node.js (v14 or higher)
- npm or yarn
- .NET API running on `http://localhost:5050`

## Installation

```bash
cd frontend/gol-visualizer
npm install
```

## Running the Application

1. **Start the .NET API backend** (in the main solution directory):
   ```bash
   cd Sinapsa.GoL.DistOrleansProc
   dotnet run
   ```
   
   The API should be running on `http://localhost:5050`

2. **Start the React development server**:
   ```bash
   cd frontend/gol-visualizer
   npm start
   ```
   
   The app will open in your browser at `http://localhost:3000`

## Usage

1. **Configure the Universe**:
   - Set the number of chunks horizontally (Chunks X)
   - Set the number of chunks vertically (Chunks Y)
   - Set the size of each square chunk (8-128 cells)
   - Set the initial density of living cells (0-1, or 0-100%)

2. **Initialize**:
   - Click "Initialize Universe" to create the grid
   - The backend will create distributed grains for each chunk

3. **Run Simulation**:
   - Click "Step Once" to advance by one generation
   - Click "Start Auto-Run" to run continuously
   - Adjust the speed using the dropdown (10ms to 1000ms)
   - Click "Stop Auto-Run" to pause

## Project Structure

```
frontend/gol-visualizer/
??? src/
?   ??? components/
?   ?   ??? ControlPanel.tsx       # Configuration and control buttons
?   ?   ??? ControlPanel.css
?   ?   ??? GameOfLifeGrid.tsx     # Grid visualization component
?   ?   ??? GameOfLifeGrid.css
?   ??? services/
?   ?   ??? api.ts                 # API communication service
?   ??? App.tsx                    # Main application component
?   ??? App.css
?   ??? index.tsx                  # Application entry point
?   ??? index.css
??? package.json
```

## API Endpoints Used

- `POST /api/init` - Initialize universe with configuration
- `POST /api/step` - Advance simulation by one generation
- `GET /api/grid` - Fetch current grid state as structured data

## Configuration Examples

### Small Test Grid (Quick)
- Chunks X: 2
- Chunks Y: 2
- Chunk Size: 16
- Total: 32×32 = 1,024 cells

### Medium Grid (Recommended)
- Chunks X: 2
- Chunks Y: 2
- Chunk Size: 32
- Total: 64×64 = 4,096 cells

### Large Grid (Performance Test)
- Chunks X: 4
- Chunks Y: 4
- Chunk Size: 50
- Total: 200×200 = 40,000 cells

### Massive Grid (Stress Test)
- Chunks X: 10
- Chunks Y: 10
- Chunk Size: 64
- Total: 640×640 = 409,600 cells

## Features Explained

### Chunk Visualization
- Red lines indicate chunk boundaries
- Each chunk is processed by a separate Orleans grain
- Visualizes the distributed nature of the system

### Auto-Run Speed
- **10ms**: Very fast, good for observing rapid evolution
- **50ms**: Fast, smooth animation
- **100ms**: Normal speed, easy to follow
- **500ms**: Slow, good for analyzing patterns
- **1000ms**: Very slow, detailed observation

### Cell States
- **Blue cells**: Alive
- **White cells**: Dead
- Smooth transitions between states

## Performance Notes

- Cell size automatically adjusts based on grid dimensions
- Larger grids (>100×100) may have smaller cells to fit the viewport
- Auto-run speed may be limited by:
  - Backend processing time
  - Network latency
  - Browser rendering performance

## Future Enhancements (Planned)

- [ ] SignalR integration for real-time push updates
- [ ] Pattern library (Gliders, Oscillators, etc.)
- [ ] Click-to-toggle cell states
- [ ] Heat map showing cell change frequency
- [ ] Save/Load universe states
- [ ] Statistics dashboard (births, deaths, stable regions)
- [ ] Performance metrics (generations per second)

## Troubleshooting

### "Failed to initialize universe"
- Ensure the .NET API is running on `http://localhost:5050`
- Check browser console for CORS errors
- Verify Orleans dashboard shows healthy silos

### Grid not updating
- Check browser console for API errors
- Verify network tab shows successful API calls
- Ensure auto-run is started (green "Start Auto-Run" button)

### Performance issues
- Reduce grid size (fewer chunks or smaller chunk size)
- Increase auto-run interval
- Close other browser tabs

## Technologies Used

- **React 18** - UI framework
- **TypeScript** - Type-safe JavaScript
- **CSS Grid** - Grid layout and visualization
- **Fetch API** - Backend communication
- **Create React App** - Development tooling

## Development

```bash
# Install dependencies
npm install

# Start development server
npm start

# Build for production
npm build

# Run tests
npm test
```

## License

Part of the Sinapsa.GoL.DistOrleansProc project.
