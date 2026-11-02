using Microsoft.EntityFrameworkCore;
using HealthcareAiApi.Models;

namespace HealthcareAiApi.Data
{
    public class ClinicalDbContext : DbContext
    {
        public ClinicalDbContext(DbContextOptions<ClinicalDbContext> options) : base(options) { }

        public DbSet<ClinicalLog> ClinicalLogs => Set<ClinicalLog>();
    }
}