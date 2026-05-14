using Microsoft.AspNetCore.Routing;

namespace GymApp.Common.Presentation;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
