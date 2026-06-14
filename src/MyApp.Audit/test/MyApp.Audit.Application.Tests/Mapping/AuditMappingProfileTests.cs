using AutoMapper;
using FluentAssertions;
using MyApp.Audit.Application.Tests.Common;
using MyApp.Audit.Domain;
using Xunit;

namespace MyApp.Audit.Application.Tests.Mapping;

public class AuditMappingProfileTests
{
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    [Fact]
    public void Map_EntityChangeWithProperties_MapsChangeTypeAndChildren()
    {
        var changeId = Guid.NewGuid();
        var entityChange = new EntityChange(changeId)
        {
            EntityName = "Product",
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeTypeEnum.Updated,
            OriginalValue = """{"Name":"Old"}""",
            NewValue = """{"Name":"New"}""",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "tester"
        };

        entityChange.PropertyChanges.Add(new PropertyChange(Guid.NewGuid())
        {
            EntityChangeId = changeId,
            PropertyName = "Name",
            OriginalValue = "Old",
            NewValue = "New"
        });

        var dto = _mapper.Map<Application.Contracts.DTOs.EntityChangeDto>(entityChange);

        dto.ChangeType.Should().Be("Updated");
        dto.EntityName.Should().Be("Product");
        dto.PropertyChanges.Should().HaveCount(1);
        dto.PropertyChanges[0].PropertyName.Should().Be("Name");
        dto.PropertyChanges[0].OriginalValue.Should().Be("Old");
        dto.PropertyChanges[0].NewValue.Should().Be("New");
    }
}
