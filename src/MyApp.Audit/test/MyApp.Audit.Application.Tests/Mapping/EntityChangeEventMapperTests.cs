using FluentAssertions;
using MyApp.Audit.Application.Mapping;
using Xunit;
using MyApp.Audit.Domain;
using MyApp.Shared.Domain.Events;

namespace MyApp.Audit.Application.Tests.Mapping;

public class EntityChangeEventMapperTests
{
    [Fact]
    public void ToCreateDto_MapsAddedChange_WithNullOriginalSnapshot()
    {
        var entityId = Guid.NewGuid();
        const string newJson = """{"Name":"Widget"}""";

        var dto = EntityChangeEventMapper.ToCreateDto(new EntityChangePayload(
            "Product",
            entityId,
            "Added",
            [new PropertyChangePayload("Name", null, "Widget")],
            OriginalValue: null,
            NewValue: newJson));

        dto.Should().NotBeNull();
        dto!.EntityName.Should().Be("Product");
        dto.EntityId.Should().Be(entityId);
        dto.ChangeType.Should().Be(ChangeTypeEnum.Created);
        dto.OriginalValue.Should().BeNull();
        dto.NewValue.Should().Be(newJson);
        dto.PropertyChanges.Should().ContainSingle(p => p.PropertyName == "Name" && p.NewValue == "Widget");
    }

    [Fact]
    public void ToCreateDto_ReturnsNull_WhenEntityIdIsNotGuid()
    {
        var dto = EntityChangeEventMapper.ToCreateDto(new EntityChangePayload(
            "Legacy",
            42,
            "Added",
            []));

        dto.Should().BeNull();
    }
}
