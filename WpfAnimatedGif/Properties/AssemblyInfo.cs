using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Markup;

[assembly: AssemblyTitle("WpfAnimatedGif")]
[assembly: AssemblyDescription("A library to display animated GIF images in WPF")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Thomas Levesque")]
[assembly: AssemblyProduct("WpfAnimatedGif")]
[assembly: AssemblyCopyright("Copyright ©  2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: Guid("a985ebe7-753b-4d73-b363-4b63d87f98b7")]

[assembly: AssemblyVersion(VersionInfo.VersionString)]
[assembly: AssemblyFileVersion(VersionInfo.VersionString)]
[assembly: AssemblyInformationalVersion(VersionInfo.VersionString)]

[assembly: XmlnsDefinition("http://wpfanimatedgif.codeplex.com", "WpfAnimatedGif")]
[assembly: XmlnsPrefix("http://wpfanimatedgif.codeplex.com", "gif")]

// ReSharper disable CheckNamespace
class VersionInfo
{
    /// <summary>
    /// Single place to define version
    /// </summary>
    public const string VersionString = "1.4.6";
}
// ReSharper restore CheckNamespace
