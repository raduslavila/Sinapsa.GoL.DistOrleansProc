# Quick Start Guide - Frontend + Backend

## Prerequisites

- .NET 6+ SDK
- Node.js 14+
- npm

## Step 1: Start the Backend

Open a terminal in the solution root:

```bash
cd Sinapsa.GoL.DistOrleansProc
dotnet run
```

**Wait for:**
```
Now listening on: http://0.0.0.0:5050
Application started.
```

**Endpoints Available:**
- API: http://localhost:5050/api
- Swagger: http://localhost:5050/swagger
- Orleans Dashboard: http://localhost:8000/dashboard

## Step 2: Start the Frontend

Open a **new terminal** in the solution root:

```bash
cd frontend/gol-visualizer
npm install    # First time only
npm start
```

**Wait for:**
```
Compiled successfully!
Local: http://localhost:3000
```

Your browser will open automatically.

## Step 3: Use the Application

### Quick Test

1. **Click "Initialize Universe"** (uses defaults: 2×2 chunks, 32×32 each)
2. **Click "Start Auto-Run"** to watch the simulation
3. **Adjust speed** with the dropdown (try "50ms (Fast)")
4. **Click "Stop Auto-Run"** to pause

### Try Different Configurations

1. **Click "Re-Initialize Universe"** to reset
2. **Change settings:**
   - Chunks X: 4
   - Chunks Y: 4
   - Chunk Size: 32
   - Live Density: 0.2 (20%)
3. **Click "Initialize Universe"** again
4. **Start Auto-Run** to see a larger 128×128 grid

## What You'll See

- **Purple gradient background** with white control panel
- **Grid display** on the right showing the universe
- **Red lines** marking chunk boundaries
- **Blue cells** = alive
- **White cells** = dead
- **Live stats** showing generation count and living cells

## Tips

- **Small grids** (2×2 chunks × 16 size = 32×32) run fastest
- **Medium grids** (2×2 chunks × 32 size = 64×64) are most balanced
- **Large grids** (4×4 chunks × 50 size = 200×200) show distribution well
- **Very fast speed** (10ms) is good for observing rapid evolution
- **Slow speed** (500ms-1000ms) is good for studying patterns

## Troubleshooting

### Browser shows "Failed to initialize universe"
- Make sure the backend is running (check terminal)
- Verify you see "Now listening on: http://0.0.0.0:5050"

### Grid is not updating
- Check that "Auto-Run" button shows "Stop Auto-Run" (green)
- Try clicking "Step Once" manually
- Check browser console (F12) for errors

### Performance issues
- Reduce grid size (fewer chunks or smaller chunk size)
- Increase interval (e.g., 100ms instead of 10ms)
- Refresh the browser

## Architecture Overview

```
????????????????????        HTTP/REST         ????????????????????
?  React Frontend  ?????????????????????????????  ASP.NET Core   ?
?  (localhost:3000)?                           ?  (localhost:5050)?
?                  ?   POST /api/init          ?                  ?
?  - Configuration ?   POST /api/step          ?  - Controllers   ?
?  - Grid Display  ?   GET  /api/grid          ?  - Services      ?
?  - Controls      ?                           ?                  ?
????????????????????                           ????????????????????
                                                        ?
                                                        ?
                                               ????????????????????
                                               ?  Orleans Grains  ?
                                               ?  (Distributed)   ?
                                               ?                  ?
                                               ?  chunk_0_0       ?
                                               ?  chunk_0_1       ?
                                               ?  chunk_1_0       ?
                                               ?  chunk_1_1       ?
                                               ????????????????????
```

## Next Steps

- Check the Orleans Dashboard: http://localhost:8000/dashboard
- Open Swagger: http://localhost:5050/swagger
- Read the full docs: `REACT_FRONTEND_COMPLETE.md`
- Try different patterns and configurations
- Experiment with different auto-run speeds

Enjoy exploring Conway's Game of Life! ??
