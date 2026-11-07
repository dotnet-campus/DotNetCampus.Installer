namespace DotNetCampus.Installer.Lib.Exceptions;

/// <summary>
/// 安装包配置无效时抛出的异常。
/// </summary>
public class InstallerConfigurationException : InstallerException
{
    /// <summary>
    /// 初始化安装包配置异常。
    /// </summary>
    public InstallerConfigurationException()
    {
    }

    /// <summary>
    /// 使用指定错误消息初始化安装包配置异常。
    /// </summary>
    /// <param name="message">描述配置错误的消息。</param>
    public InstallerConfigurationException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和内部异常初始化安装包配置异常。
    /// </summary>
    /// <param name="message">描述配置错误的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public InstallerConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
