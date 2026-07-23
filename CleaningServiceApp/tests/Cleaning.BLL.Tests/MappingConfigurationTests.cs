using Cleaning.BLL.Features.Admin;
using Cleaning.BLL.Features.Ai;
using Cleaning.BLL.Features.Bookings;
using Cleaning.BLL.Features.Chat;
using Cleaning.BLL.Features.Reviews;
using Cleaning.BLL.Features.ServiceCatalog;
using Cleaning.BLL.Features.UserAddresses;
using Cleaning.BLL.Features.Worker;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

public sealed class MappingConfigurationTests
{
    [Fact(DisplayName = "[UT-MAP-01] All AutoMapper profiles are internally consistent")]
    public void AllProfiles_ConfigurationIsValid()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<BookingMappingProfile>();
            cfg.AddProfile<WorkerMappingProfile>();
            cfg.AddProfile<ReviewMappingProfile>();
            cfg.AddProfile<UserAddressMappingProfile>();
            cfg.AddProfile<ServiceMappingProfile>();
            cfg.AddProfile<AdminMappingProfile>();
            cfg.AddProfile<ChatMappingProfile>();
            cfg.AddProfile<AiMappingProfile>();
        }, NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
    }
}
