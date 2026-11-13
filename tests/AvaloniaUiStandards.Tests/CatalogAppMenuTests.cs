using Avalonia.Controls;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The catalog's own application menu replaces Avalonia's default "About Avalonia".</summary>
public class CatalogAppMenuTests
{
    [Fact]
    public void AppMenu_HasCatalogAbout_AndNoAvaloniaAbout()
    {
        var headers = App.CreateMenu().Items.OfType<NativeMenuItem>().Select(i => i.Header).ToList();

        Assert.Equal("Avalonia UI Standards Catalog", AppInfo.Name);
        Assert.Contains("About Avalonia UI Standards Catalog", headers);
        Assert.DoesNotContain("About Avalonia", headers);
        Assert.StartsWith("AvaloniaUiStandards ", AppInfo.LibraryText);
    }
}
