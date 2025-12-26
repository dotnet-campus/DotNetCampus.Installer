using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Lib.Logging;

/// <summary>
/// 安装器的日志记录器
/// </summary>
public class InstallerLogger
{
    public FileInfo? LogFile { get; private set; }

    public void SetLogFile(FileInfo logFile)
    {
        if (LogFile != null)
        {
            throw new InvalidOperationException($"已经设置了日志文件: {LogFile.FullName}；不能被 '{logFile}' 覆盖");
        }

        lock (_locker)
        {
            LogFile = logFile;

            if (_logCache != null)
            {
                var messageStringBuilder = new StringBuilder();
                foreach (var message in _logCache)
                {
                    messageStringBuilder.Append(message);
                    // 不需要 AppendLine 因为 Message 里面已经包含换行符了
                }

                File.AppendAllText(LogFile.FullName, messageStringBuilder.ToString(), Encoding.UTF8);

                _logCache.Clear();
                _logCache = null;
            }
        }
    }

    private List<string>? _logCache;
    private readonly Lock _locker = new Lock();

    public void WriteLog(string message)
    {
        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
        Trace.WriteLine(text);

        lock (_locker)
        {
            if (LogFile != null)
            {
                File.AppendAllText(LogFile.FullName, text, Encoding.UTF8);
            }
            else
            {
                _logCache ??= new List<string>();
                _logCache.Add(text);
            }
        }
    }
}
