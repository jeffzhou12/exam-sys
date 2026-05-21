namespace ExamSystem.Application.Common.Interfaces;

/// <summary>
/// 文件存储工厂，支持按模块名获取对应存储实例（不同模块可配置不同 S3 Bucket 或统一使用本地目录）。
/// </summary>
public interface IFileStorageFactory
{
    /// <summary>
    /// 根据模块名获取文件存储服务。未单独配置的模块自动回落到 Default 存储。
    /// </summary>
    /// <param name="module">模块名，如 "Books"、"Media"，默认为 "Default"</param>
    IFileStorageService GetStorage(string module = "Default");
}
