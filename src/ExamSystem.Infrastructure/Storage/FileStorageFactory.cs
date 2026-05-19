using ExamSystem.Application.Common.Interfaces;

namespace ExamSystem.Infrastructure.Storage;

/// <summary>
/// 文件存储工厂实现。
/// 持有每个模块对应的 IFileStorageService 实例，按模块名路由，未命中则回落 Default。
/// </summary>
public class FileStorageFactory : IFileStorageFactory
{
    private readonly IReadOnlyDictionary<string, IFileStorageService> _storages;

    public FileStorageFactory(IReadOnlyDictionary<string, IFileStorageService> storages)
    {
        if (storages is null || storages.Count == 0)
            throw new ArgumentException("至少需要注册一个存储实例。", nameof(storages));
        _storages = storages;
    }

    public IFileStorageService GetStorage(string module = "Default")
    {
        // 1. 精确匹配模块名（不区分大小写）
        if (_storages.TryGetValue(module, out var svc)) return svc;
        // 2. 回落 Default
        if (_storages.TryGetValue("Default", out var def)) return def;
        // 3. 兜底：返回第一个
        return _storages.Values.First();
    }
}
