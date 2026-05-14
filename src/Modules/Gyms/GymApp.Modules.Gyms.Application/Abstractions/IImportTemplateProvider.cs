namespace GymApp.Modules.Gyms.Application.Abstractions;

public interface IImportTemplateProvider
{
    Task<Stream> GenerateAsync(CancellationToken cancellationToken = default);
}
