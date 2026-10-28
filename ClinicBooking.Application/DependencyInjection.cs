using ClinicBooking.Application.Mappings;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        return services;
    }
}
