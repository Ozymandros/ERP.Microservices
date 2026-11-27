using MyApp.Crm.Domain.Tags;

namespace MyApp.Crm.Domain.Tests;

/// <summary>Tests for domain invariants enforced by the Tag entity.</summary>
public class TagInvariantsTests
{
    [Fact]
    public void Ctor_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Tag(Guid.NewGuid(), "  "));
    }

    [Fact]
    public void Ctor_TrimsName()
    {
        var tag = new Tag(Guid.NewGuid(), " vip ");

        Assert.Equal("vip", tag.Name);
    }

    [Fact]
    public void Rename_Blank_Throws()
    {
        var tag = new Tag(Guid.NewGuid(), "vip");

        Assert.Throws<ArgumentException>(() => tag.Rename(" "));
    }

    [Fact]
    public void Rename_TrimsName()
    {
        var tag = new Tag(Guid.NewGuid(), "vip");

        tag.Rename(" hot ");

        Assert.Equal("hot", tag.Name);
    }
}
