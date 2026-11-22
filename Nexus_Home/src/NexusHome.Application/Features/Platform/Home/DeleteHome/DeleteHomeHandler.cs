namespace NexusHome.Application.Features.Platform.Home.DeleteHome;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for DeleteHomeCommand.
/// </summary>
public sealed class DeleteHomeHandler : IRequestHandler<DeleteHomeCommand, Result<DeleteHomeResponse>>
{
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IPlatformAuditLogRepository _auditLogRepository;

    public DeleteHomeHandler(
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IHomeRepository homeRepository,
        IPlatformAuditLogRepository auditLogRepository)
    {
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _homeRepository = homeRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<DeleteHomeResponse>> Handle(DeleteHomeCommand request, CancellationToken cancellationToken)
    {
        // Verify requester has PlatformAdmin or higher
        var hasPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(request.RequesterUserId, cancellationToken);
        if (!hasPlatformAdmin)
        {
            return Result<DeleteHomeResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Get home
        var home = await _homeRepository.GetByIdAsync(request.HomeId, cancellationToken);

        if (home == null)
        {
            return Result<DeleteHomeResponse>.Failure(Platform.HomeNotFound);
        }

        if (home.DeletedAt != null)
        {
            return Result<DeleteHomeResponse>.Failure(Platform.HomeDeleted);
        }

        // Soft delete home
        home.Delete();
        await _homeRepository.UpdateAsync(home, cancellationToken);

        // Audit log
        var auditLog = PlatformAuditLog.Create(
            request.RequesterUserId,
            "HomeDeleted",
            "Home",
            request.HomeId,
            $"{{\"name\":\"{home.Name}\"}}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        return Result<DeleteHomeResponse>.Success(new DeleteHomeResponse(
            request.HomeId,
            true));
    }
}