namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private bool _databaseLoaded;
    private static string? _connectionString;

    private void LoadDatabase()
    {
        DB_LoadConnectionString();
        DB_CreateTables();
        CreateWeaponPreferencesTable();
        DB_CreateVipTestTable();
        DB_SyncGroups();
    }


}

