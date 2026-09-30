namespace DotNetCampus.Installer.Lib.Exceptions;

/// <summary>
/// 安装包异常的基类。
/// </summary>
public class InstallerException : Exception
{
    /// <summary>
    /// 安装包异常。
    /// </summary>
    public InstallerException()
    {
    }

    /// <summary>
    /// 使用指定错误消息初始化安装包异常。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public InstallerException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和内部异常初始化安装包异常。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public InstallerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
