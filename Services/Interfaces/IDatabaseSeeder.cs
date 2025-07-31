using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync();
    }
} 