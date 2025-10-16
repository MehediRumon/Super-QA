using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SuperQA.Infrastructure.Data;

public class SuperQADbContextFactory : IDesignTimeDbContextFactory<SuperQADbContext>
{
    public SuperQADbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SuperQADbContext>();
        
        // Use a dummy connection string for migrations
        // The actual connection string will be used at runtime
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SuperQA;Trusted_Connection=True;MultipleActiveResultSets=true",
            b => b.MigrationsAssembly("SuperQA.Infrastructure"));

        return new SuperQADbContext(optionsBuilder.Options);
    }
}
