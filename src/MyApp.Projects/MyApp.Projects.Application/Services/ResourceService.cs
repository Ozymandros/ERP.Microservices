using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Projects.Application.Services;

/// <summary>Resource and resource group management (resources live in the Projects service for now).</summary>
public class ResourceService : AppServiceBase, IResourceService
{
    private readonly IResourceRepository _resources;
    private readonly IResourceGroupRepository _groups;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of the <see cref="ResourceService"/> class.</summary>
    public ResourceService(
        IResourceRepository resources,
        IResourceGroupRepository groups,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<ResourceService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Projects)
    {
        _resources = resources;
        _groups = groups;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<List<ResourceDto>> ListAsync(CancellationToken cancellationToken = default) =>
        _mapper.Map<List<ResourceDto>>(await _resources.ListAsync(cancellationToken));

    /// <inheritdoc />
    public async Task<PaginatedResult<ResourceDto>> QueryAsync(ISpecification<Resource> spec, CancellationToken cancellationToken = default)
    {
        var result = await _resources.QueryAsync(spec);
        return new PaginatedResult<ResourceDto>(
            _mapper.Map<List<ResourceDto>>(result.Items), result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <inheritdoc />
    public async Task<ResourceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resource = await _resources.GetByIdAsync(id);
        if (resource is null)
            return null;
        if (resource.ResourceGroupId.HasValue)
            resource.ResourceGroup ??= await _groups.GetByIdAsync(resource.ResourceGroupId.Value);
        return _mapper.Map<ResourceDto>(resource);
    }

    /// <inheritdoc />
    public async Task<ResourceDto> CreateAsync(CreateUpdateResourceDto dto, CancellationToken cancellationToken = default)
    {
        var resourceNo = dto.ResourceNo.Trim().ToUpperInvariant();
        if (await _resources.GetByNumberAsync(resourceNo, cancellationToken) is not null)
            throw new InvalidOperationException($"Resource {resourceNo} already exists.");

        var resource = new Resource(Guid.NewGuid()) { ResourceNo = resourceNo };
        await ApplyAsync(resource, dto);

        await _resources.AddAsync(resource);
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ResourceDto>(resource);
    }

    /// <inheritdoc />
    public async Task<ResourceDto> UpdateAsync(Guid id, CreateUpdateResourceDto dto, CancellationToken cancellationToken = default)
    {
        var resource = await _resources.GetByIdAsync(id) ?? throw new NotFoundException(nameof(Resource), id);

        var resourceNo = dto.ResourceNo.Trim().ToUpperInvariant();
        if (resourceNo != resource.ResourceNo)
        {
            var other = await _resources.GetByNumberAsync(resourceNo, cancellationToken);
            if (other is not null && other.Id != id)
                throw new InvalidOperationException($"Resource {resourceNo} already exists.");
            resource.ResourceNo = resourceNo;
        }

        await ApplyAsync(resource, dto);
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ResourceDto>(resource);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resource = await _resources.GetByIdAsync(id) ?? throw new NotFoundException(nameof(Resource), id);
        if (await _resources.IsInUseAsync(id, cancellationToken))
            throw new InvalidOperationException($"Resource {resource.ResourceNo} is used on projects; block it instead of deleting it.");
        await _resources.DeleteAsync(resource);
        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ResourceGroupDto>> ListGroupsAsync(CancellationToken cancellationToken = default) =>
        _mapper.Map<List<ResourceGroupDto>>((await _groups.GetAllAsync()).OrderBy(g => g.Code));

    /// <inheritdoc />
    public async Task<ResourceGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetByIdAsync(id);
        return group is null ? null : _mapper.Map<ResourceGroupDto>(group);
    }

    /// <inheritdoc />
    public async Task<ResourceGroupDto> CreateGroupAsync(CreateUpdateResourceGroupDto dto, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _groups.GetByCodeAsync(code, cancellationToken) is not null)
            throw new InvalidOperationException($"Resource group {code} already exists.");

        var group = new ResourceGroup(Guid.NewGuid()) { Code = code, Name = dto.Name.Trim() };
        await _groups.AddAsync(group);
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ResourceGroupDto>(group);
    }

    /// <inheritdoc />
    public async Task<ResourceGroupDto> UpdateGroupAsync(Guid id, CreateUpdateResourceGroupDto dto, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetByIdAsync(id) ?? throw new NotFoundException(nameof(ResourceGroup), id);
        var code = dto.Code.Trim().ToUpperInvariant();
        if (code != group.Code)
        {
            var other = await _groups.GetByCodeAsync(code, cancellationToken);
            if (other is not null && other.Id != id)
                throw new InvalidOperationException($"Resource group {code} already exists.");
            group.Code = code;
        }

        group.Name = dto.Name.Trim();
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ResourceGroupDto>(group);
    }

    /// <inheritdoc />
    public async Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetByIdAsync(id) ?? throw new NotFoundException(nameof(ResourceGroup), id);
        if (await _groups.IsInUseAsync(id, cancellationToken))
            throw new InvalidOperationException($"Resource group {group.Code} is used by one or more resources.");

        await _groups.DeleteAsync(group);
        await SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAsync(Resource resource, CreateUpdateResourceDto dto)
    {
        if (dto.ResourceGroupId.HasValue)
        {
            resource.ResourceGroup = await _groups.GetByIdAsync(dto.ResourceGroupId.Value)
                ?? throw new ArgumentException($"Resource group {dto.ResourceGroupId} does not exist.");
        }
        else
        {
            resource.ResourceGroup = null;
        }

        if (dto.Type == ResourceType.Machine && dto.UserId.HasValue)
            throw new ArgumentException("Machines cannot be linked to a user.");

        resource.Name = dto.Name.Trim();
        resource.Type = dto.Type;
        resource.ResourceGroupId = dto.ResourceGroupId;
        resource.UserId = dto.UserId;
        resource.BaseUnitOfMeasure = dto.BaseUnitOfMeasure.Trim().ToUpperInvariant();
        resource.UnitCost = dto.UnitCost;
        resource.UnitPrice = dto.UnitPrice;
        resource.DailyCapacity = dto.DailyCapacity;
        resource.Blocked = dto.Blocked;
    }
}
