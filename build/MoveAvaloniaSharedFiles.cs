#:property TargetFramework=net10.0

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

// 一次性开发者工具；参数为包含 DotNetCampus.Installer.slnx 的仓库根目录。
// dotnet run --file build/MoveAvaloniaSharedFiles.cs -- .
var root = args[0];
var classic = Path.Combine(root, "src", "DotNetCampus.Installer.AvaloniaClassic");
var shared = Path.Combine(root, "src", "DotNetCampus.Installer.AvaloniaShared");

foreach (var directory in new[] { "Infrastructure", "InstallerPrograms", "ViewModels", "Localizations" })
{
    Directory.Move(Path.Combine(classic, directory), Path.Combine(shared, directory));
    Console.WriteLine($"Moved {directory} to AvaloniaShared.");
}
File.Move(Path.Combine(classic, "LocalizedText.cs"), Path.Combine(shared, "LocalizedText.cs"));
Console.WriteLine("Moved LocalizedText.cs to AvaloniaShared.");

var sharedProjectPath = Path.Combine(shared, "DotNetCampus.Installer.AvaloniaShared.csproj");
var sharedProject = XDocument.Load(sharedProjectPath, LoadOptions.PreserveWhitespace);
string[] linkedFiles =
[
    "../DotNetCampus.Installer.AvaloniaClassic/Infrastructure/*.cs",
    "../DotNetCampus.Installer.AvaloniaClassic/InstallerPrograms/*.cs",
    "../DotNetCampus.Installer.AvaloniaClassic/ViewModels/*.cs",
    "../DotNetCampus.Installer.AvaloniaClassic/LocalizedText.cs",
    "../DotNetCampus.Installer.AvaloniaClassic/Localizations/*.toml",
];
sharedProject.Root!.Elements("ItemGroup").Elements()
    .Where(element => (element.Name == "Compile" || element.Name == "AdditionalFiles")
        && linkedFiles.Contains((string?)element.Attribute("Include"), StringComparer.Ordinal))
    .ToList()
    .ForEach(RemoveItem);
sharedProject.Save(sharedProjectPath, SaveOptions.DisableFormatting);

var classicProjectPath = Path.Combine(classic, "DotNetCampus.Installer.AvaloniaClassic.csproj");
var classicProject = XDocument.Load(classicProjectPath, LoadOptions.PreserveWhitespace);
classicProject.Root!.Elements("ItemGroup").Elements("Compile")
    .Where(element => (string?)element.Attribute("Remove") == "Infrastructure/**/*.cs;InstallerPrograms/**/*.cs;ViewModels/**/*.cs;LocalizedText.cs")
    .ToList()
    .ForEach(RemoveItem);
classicProject.Save(classicProjectPath, SaveOptions.DisableFormatting);
Console.WriteLine("Removed shared file links and Classic Compile Remove items.");

static void RemoveItem(XElement element)
{
    // 连同对应的缩进一起删除，保留其余项目文件格式。
    ((XText)element.PreviousNode!).Remove();
    element.Remove();
}
