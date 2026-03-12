using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using MoonlightFarm.Server.Models;
using MoonlightFarm.Server.Services;

namespace MoonlightFarm.Server.Services
{
    public class GameLoopService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _tickRate = TimeSpan.FromMilliseconds(625); // ~15 min day (1440 * 0.625 = 900s)

        public GameLoopService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var roomManager = scope.ServiceProvider.GetRequiredService<RoomManager>();
                    var rooms = roomManager.GetAllRooms();

                    foreach (var room in rooms)
                    {
                        var context = room.Context;
                        
                        // Advance time
                        TimeManager.AdvanceTime(context, 1);

                        // Update Animals
                        AnimalManager.UpdateAnimals(context);
                        
                        // Update Monsters in Mines
                        MiningManager.UpdateMonsters(context);
                        
                        // Update Crops (diseases, etc.)
                        FarmingManager.UpdateCrops(context);

                        // Update Moonlight System
                        MoonlightSystem.Update(context);

                        // If state changed, we could broadcast here, 
                        // but for now we rely on client polling or specific action broadcasts
                    }
                }

                await Task.Delay(_tickRate, stoppingToken);
            }
        }
    }
}
