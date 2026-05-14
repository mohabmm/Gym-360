using GymApp.Common.Domain;
using MediatR;

namespace GymApp.Common.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
