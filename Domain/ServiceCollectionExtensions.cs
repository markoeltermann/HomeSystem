using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Domain;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHomeSystemContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HomeSystemContext");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<JobStatus>("job_status");
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<HomeSystemContext>(options =>
            options.UseNpgsql(dataSource));

        return services;
    }
}
