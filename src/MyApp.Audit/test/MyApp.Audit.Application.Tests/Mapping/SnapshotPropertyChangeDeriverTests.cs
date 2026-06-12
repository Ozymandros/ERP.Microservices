using FluentAssertions;
using MyApp.Audit.Application.Mapping;
using MyApp.Audit.Domain;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Audit.Application.Tests.Mapping;

public class SnapshotPropertyChangeDeriverTests
{
    [Fact]
    public void DeriveCreateDtos_ReturnsChangedProperties_FromJsonSnapshots()
    {
        const string original = """{"Name":"Old","Email":"same@test.com"}""";
        const string updated = """{"Name":"New","Email":"same@test.com"}""";

        var result = SnapshotPropertyChangeDeriver.DeriveCreateDtos(original, updated);

        result.Should().ContainSingle();
        result[0].PropertyName.Should().Be("Name");
        result[0].OriginalValue.Should().Be("Old");
        result[0].NewValue.Should().Be("New");
    }

    [Fact]
    public void DeriveCreateDtos_ReturnsEmpty_WhenSnapshotsAreMissing()
    {
        SnapshotPropertyChangeDeriver.DeriveCreateDtos(null, """{"Name":"New"}""")
            .Should().BeEmpty();
    }
}

public class EntityChangeEventMapperUpdatedTests
{
    [Fact]
    public void ToCreateDto_DerivesPropertyChangesForUpdated_WhenPayloadPropertiesEmpty()
    {
        var entityId = Guid.NewGuid();
        const string original = """{"Name":"Old","Email":"old@test.com"}""";
        const string updated = """{"Name":"New","Email":"new@test.com"}""";

        var dto = EntityChangeEventMapper.ToCreateDto(new EntityChangePayload(
            "Customer",
            entityId,
            "Modified",
            [],
            OriginalValue: original,
            NewValue: updated));

        dto.Should().NotBeNull();
        dto!.ChangeType.Should().Be(ChangeTypeEnum.Updated);
        dto.PropertyChanges.Should().HaveCount(2);
        dto.PropertyChanges.Should().Contain(p =>
            p.PropertyName == "Name" && p.OriginalValue == "Old" && p.NewValue == "New");
        dto.PropertyChanges.Should().Contain(p =>
            p.PropertyName == "Email" && p.OriginalValue == "old@test.com" && p.NewValue == "new@test.com");
    }
}
