using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class EngineBoundaryTests
{
    [Fact]
    public void Shared_facade_supports_independent_concurrent_calls()
    {
        Parallel.For(0, 64, documentNumber =>
        {
            var title = $"Document {documentNumber}";
            var path = $"docs/{documentNumber}.md";
            var options = new MarkdownParseOptions { Path = path };
            var before = MnemosyneFacade.Parse($"# {title}\n[local](#details)", options);
            var after = MnemosyneFacade.Parse($"# {title}\n## Details\n[local](#details)", options);

            after.Title.Should().Be(title);
            MnemosyneFacade.Explain($"# {title}", options).Document.Title.Should().Be(title);
            MnemosyneFacade.ResolveLinks(after, new MarkdownPathIndex([path])).Should().BeEmpty();
            MnemosyneFacade.Diff([before], [after]).Changes.Should().Contain(change =>
                change.Kind == SnapshotChangeKind.HeadingAdded && change.HeadingSlug == "details");
        });
    }

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
