using Microsoft.Data.Sqlite;
using PetShop.Web.Infrastructure;

namespace PetShop.Web.Services;

public sealed class BackupService(AppPaths paths, TimeProvider clock)
{
    public IReadOnlyList<FileInfo> List() => new DirectoryInfo(paths.BackupDirectory).GetFiles("pawledger-*.db").OrderByDescending(x => x.CreationTimeUtc).ToList();
    public async Task<string> CreateAsync(CancellationToken ct = default)
    {
        var destination = Path.Combine(paths.BackupDirectory, $"pawledger-{clock.GetLocalNow():yyyyMMdd-HHmmss}.db");
        await using var source = new SqliteConnection($"Data Source={paths.DatabaseFile};Mode=ReadOnly");
        await using var target = new SqliteConnection($"Data Source={destination}");
        await source.OpenAsync(ct); await target.OpenAsync(ct); source.BackupDatabase(target);
        await using var check = target.CreateCommand(); check.CommandText = "PRAGMA integrity_check;";
        var result = (string?)await check.ExecuteScalarAsync(ct);
        if (result != "ok") { target.Close(); File.Delete(destination); throw new InvalidOperationException($"备份完整性检查失败：{result}"); }
        return destination;
    }
}
