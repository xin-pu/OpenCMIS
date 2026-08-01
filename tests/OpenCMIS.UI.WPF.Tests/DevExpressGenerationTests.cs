using OpenCMIS.UI.WPF.ViewModels;
using Xunit;

namespace OpenCMIS.UI.WPF.Tests;

public sealed class DevExpressGenerationTests
{
    [Fact]
    public void Main_view_model_exposes_collapsible_navigation_behavior()
    {
        Assert.NotNull(
            typeof(MainViewModel).GetProperty("IsNavigationPaneExpanded"));
        Assert.NotNull(
            typeof(MainViewModel).GetProperty("ToggleNavigationCommand"));
    }
}
