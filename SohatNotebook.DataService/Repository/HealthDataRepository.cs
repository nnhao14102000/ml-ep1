using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SohatNotebook.DataService.Data;
using SohatNotebook.DataService.IRepository;
using SohatNotebook.Entities.DbSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SohatNotebook.DataService.Repository
{
    public class HealthDataRepository : GenericRepository<HealthData>, IHealthDataRepository
    {
        public HealthDataRepository(
            AppDbContext context,
            ILogger logger) : base(context, logger)
        {
        }

        public override async Task<IEnumerable<HealthData>> All()
        {
            try
            {
                return await dbSet.Where(x => x.Status == 1)
                            .AsNoTracking()
                            .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} All method has generated an error", typeof(HealthDataRepository));
                return new List<HealthData>();
            }
        }

        public async Task<bool> UpdateHealthData(HealthData healthData)
        {
            try
            {
                var existingHealthData = await dbSet.Where(x => x.Status == 1 && x.Id == healthData.Id)
                                        .FirstOrDefaultAsync();
                if (existingHealthData is null) return false;

                existingHealthData.BloodType = healthData.BloodType;
                existingHealthData.Height = healthData.Height;
                existingHealthData.Race = healthData.Race;
                existingHealthData.Weight = healthData.Weight;
                existingHealthData.UseGlasses = healthData.UseGlasses;
                existingHealthData.UpdateDate = DateTime.UtcNow;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} UpdateHealthData method has generated an error", typeof(HealthDataRepository));
                return false;
            }
        }
    }
}
