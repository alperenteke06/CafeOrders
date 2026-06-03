namespace CafeOrders.Tests;

public sealed class WebUiRegressionTests
{
    [Fact]
    public void NewOrderSoundScript_QueuesPlaybackForBackgroundTabsAndGestureUnlock()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");

        Assert.Contains("queueNewOrderSound('order-created')", index);
        Assert.Contains("pendingNewOrderSound", index);
        Assert.Contains("prepareNewOrderSoundGuard", index);
        Assert.Contains("visibilitychange", index);
        Assert.Contains("pointerdown", index);
        Assert.Contains("playFallbackOrderBeep", index);
    }

    [Fact]
    public void UploadScripts_SupportDropFileAndWebUrlFlows()
    {
        var index = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "Index.cshtml");

        Assert.Contains("handleProductDrop", index);
        Assert.Contains("handleProductFileChange", index);
        Assert.Contains("updateProductPreviewFromUrl", index);
        Assert.Contains("handleSoundDrop", index);
        Assert.Contains("handleSoundFileChange", index);
        Assert.Contains("updateSoundPreviewFromUrl", index);
        Assert.Contains("normalizeMediaUrl", index);
    }

    [Fact]
    public void DevicesAndCategoriesSections_UseCustomPagination()
    {
        var devices = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_DevicesSection.cshtml");
        var categories = ReadRepoFile("src", "CafeOrders.WebUI", "Views", "Dashboard", "_CategoriesSection.cshtml");

        Assert.Contains("pagination-pillbar", devices);
        Assert.Contains("loadSection('devices'", devices);
        Assert.Contains("pagination-pillbar", categories);
        Assert.Contains("loadSection('categories'", categories);
    }

    private static string ReadRepoFile(params string[] segments)
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(segments).ToArray()));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CafeOrders.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("CafeOrders repository root could not be resolved.");
    }
}
