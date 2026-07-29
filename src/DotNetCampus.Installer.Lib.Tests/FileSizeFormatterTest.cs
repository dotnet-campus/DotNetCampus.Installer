using System;
using System.Collections.Generic;
using System.Text;
using DotNetCampus.Installer.Lib.Utils;

namespace DotNetCampus.Installer.Lib.Tests;

[TestClass]
public class FileSizeFormatterTest
{
    [DataTestMethod]
    [DataRow(10 * 1024, "10.00KB")]
    [DataRow(10 * 1024 * 1024, "10.00MB")]
    [DataRow(1024 * 1024 * 1024, "1.00GB")]
    [DataRow(1536, "1.50KB")]
    public void TestFileSizeFormatter1(long bytes, string expected)
    {
        var formatSize = FileSizeFormatter.FormatSize(bytes);
        Assert.AreEqual(expected, formatSize);
    }
}
