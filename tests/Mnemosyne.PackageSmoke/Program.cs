using Mnemosyne;

var options = new MarkdownParseOptions { Path = "docs/roadmap/workspace/README.md" };
var markdown = "---\nstatus: Active\nowner: platform\n---\n# Workspace plan\n## Next\nShip the package.";
var document = MnemosyneFacade.Parse(markdown, options);
Require(document.Title == "Workspace plan", "Document title");
Require(document.Semantics.DocKind.ToString() == "Epic", "Document kind");
Require(document.Semantics.Lifecycle.ToString() == "Active", "Lifecycle");
Require(document.Semantics.Owner == "platform", "Owner");
Require(MnemosyneFacade.Explain(markdown, options).RulesFired.Count > 0, "Classification explanation");

var linked = MnemosyneFacade.Parse(
    "See [the guide](../guides/README.md#install).",
    new MarkdownParseOptions { Path = "docs/README.md" });
var index = new MarkdownPathIndex(
    ["guides/README.md"],
    new Dictionary<string, IEnumerable<string>> { ["guides/README.md"] = ["install"] });
Require(MnemosyneFacade.ResolveLinks(linked, index).Count == 0, "Link resolution");

var before = MnemosyneFacade.Parse("# Guide", new MarkdownParseOptions { Path = "guide.md" });
var after = MnemosyneFacade.Parse("# Guide\n## Install", new MarkdownParseOptions { Path = "guide.md" });
Require(MnemosyneFacade.Diff([before], [after]).Changes.Any(change =>
    change.Kind == SnapshotChangeKind.HeadingAdded && change.HeadingSlug == "install"), "Snapshot diff");
Console.WriteLine("NuGet consumer smoke test passed.");

static void Require(bool condition, string contract)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Package contract failed: {contract}");
    }
}