using Microsoft.AspNetCore.Mvc;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Services
{
    public class GoLService : IGoLService
    {
        private readonly IGrainFactory<IGoLChunkGrain, string> _grainChunkFactory;

        private const string myChunk = "myBallzz";

        public GoLService(IGrainFactory<IGoLChunkGrain, string> grainChunkFactory)
        {
            _grainChunkFactory = grainChunkFactory;
        }

        public void InitChunk()
        {
            var chunk = _grainChunkFactory.GetGrain(myChunk);

            chunk.InitChunk(32, 32, 0.15f);
        }

        public async Task RunUniverseStep()
        {
            var chunk = _grainChunkFactory.GetGrain(myChunk);

            await chunk.Advance();
        }

        public async Task<bool[,]> GetCurrentState()
        {
            var chunkyGrain = _grainChunkFactory.GetGrain(myChunk);

            var myChunkState = await chunkyGrain.GetChunk();

            var _cells = new bool[32, 32];
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    _cells[x, y] = myChunkState[x, y].IsAlive;

            return _cells;
        }

        public async Task<string> DisplayCurrentState()
        {
            var chunkyGrain = _grainChunkFactory.GetGrain(myChunk);

            var myChunkState = await chunkyGrain.GetChunk();

            StringBuilder sb = new StringBuilder();

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    sb.Append(myChunkState[x, y].IsAlive ? "X" : " ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
