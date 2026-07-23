using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Features.Worker;

public sealed class WorkerMappingProfile : AutoMapper.Profile
{
    public WorkerMappingProfile()
    {
        CreateMap<WorkerProfile, WorkerProfileDto>();
        CreateMap<Cleaning.DAL.Entities.WorkerService, WorkerSkillDto>();
        CreateMap<WorkerEarning, WorkerEarningDto>();
    }
}
