using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class EngineBoundaryTests
{
    [Fact]
    public void Public_facade_is_usable_without_mnemon_types()
    {
        var asm = typeof(MnemosyneFacade).Assembly;
        asm.GetName().Name.Should().Be("Mnemosyne");

        var refs = asm.GetReferencedAssemblies().Select(a => a.Name!).ToArray();
        refs.Should().NotContain(n => n.StartsWith("Mnemon", StringComparison.Ordinal));
        refs.Should().NotContain("Microsoft.EntityFrameworkCore");
        refs.Should().NotContain("LibGit2Sharp");
        refs.Should().NotContain("Microsoft.AspNetCore");
    }
}
