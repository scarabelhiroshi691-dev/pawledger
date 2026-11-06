namespace PetShop.Web.Infrastructure;

public sealed class AppPaths
{
    public required string Root { get; init; }
    public required string DatabaseFile { get; init; }
    public required string BackupDirectory { get; init; }
    public required string DataProtectionKeysDirectory { get; init; }

    public static AppPaths Create()
    {
        var configured = Environment.GetEnvironmentVariable("PETSHOP_DATA_DIR");
        string root;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            root = Path.GetFullPath(configured);
        }
        // Preserve data created by early PawLedger preview packages, but do not
        // select an arbitrary developer drive for a fresh installation.
        else if (Directory.Exists(@"D:\Codex\PetShopData"))
        {
            root = @"D:\Codex\PetShopData";
        }
        else
        {
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PawLedger");
        }

        var paths = new AppPaths
        {
            Root = root,
            DatabaseFile = Path.Combine(root, "data", "pawledger.db"),
            BackupDirectory = Path.Combine(root, "backups"),
            DataProtectionKeysDirectory = Path.Combine(root, "keys")
        };

        Directory.CreateDirectory(Path.GetDirectoryName(paths.DatabaseFile)!);
        Directory.CreateDirectory(paths.BackupDirectory);
        Directory.CreateDirectory(paths.DataProtectionKeysDirectory);
        return paths;
    }
}
