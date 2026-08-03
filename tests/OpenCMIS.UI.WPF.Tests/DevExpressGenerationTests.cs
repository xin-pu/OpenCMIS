using System.Runtime.CompilerServices;
using System.Xml.Linq;
using OpenCMIS.UI.WPF.ViewModels;
using Xunit;

namespace OpenCMIS.UI.WPF.Tests
{
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

        [Theory]
        [InlineData("ScanPortsCommand")]
        [InlineData("ConnectCommand")]
        [InlineData("DisconnectCommand")]
        public void Device_connection_view_model_exposes_commands_bound_by_xaml(string commandName)
        {
            Assert.NotNull(
                    typeof(DeviceConnectionViewModel).GetProperty(commandName));
        }

        [Fact]
        public void Compact_styles_exposes_flat_button_style_used_by_views()
        {
            XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
            var document = XDocument.Load(
                    FindRepositoryFile(
                            ["src", "OpenCMIS.UI.WPF", "Resources", "CompactStyles.xaml"]));

            Assert.Contains(
                    document.Descendants().Select(element => element.Attribute(xaml + "Key")?.Value),
                    key => key == "OpenCmisFlatButtonStyle");
        }

        private static string FindRepositoryFile(string[]                pathParts,
                                                 [CallerFilePath] string callerFilePath = "")
        {
            foreach (var startDirectory in new[]
                                           {
                                               Path.GetDirectoryName(callerFilePath) ?? string.Empty,
                                               AppContext.BaseDirectory,
                                               Environment.CurrentDirectory
                                           }.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var directory = new DirectoryInfo(startDirectory);
                while (directory is not null)
                {
                    var path = Path.Combine(
                            [directory.FullName, .. pathParts]);
                    if (File.Exists(path))
                        return path;

                    directory = directory.Parent;
                }
            }

            throw new FileNotFoundException(
                    $"Could not find repository file: {Path.Combine(pathParts)}");
        }
    }
}
