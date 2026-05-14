using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Infrastructure.Jobs;
using Quartz;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class QuartzImportJobScheduler : IImportJobScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzImportJobScheduler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        var job = JobBuilder.Create<ProcessDataImportJob>()
            .WithIdentity($"data-import-{jobId:N}", "data-imports")
            .UsingJobData(ProcessDataImportJob.JobDataKey, jobId.ToString())
            .StoreDurably(false)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"data-import-trigger-{jobId:N}", "data-imports")
            .ForJob(job)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }
}
