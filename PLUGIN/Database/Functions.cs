using MySqlConnector;
using Dapper;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private static void DB_LoadConnectionString()
    {
        var dbConfig = Config!.DatabaseConnection;
        _connectionString = $"server={dbConfig.Host};database={dbConfig.Database};user={dbConfig.Username};password={dbConfig.Password};port={dbConfig.Port};";
    }

    private void DB_CreateTables()
    {
        using var connection = new MySqlConnection(_connectionString);
        try
        {
            connection.Open();
            Console.WriteLine("Database connection opened successfully.");
            
            var legacyTableExists = false;
            try 
            {
                var legacyCheck = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'players'");
                legacyTableExists = legacyCheck > 0;
            }
            catch (Exception)
            {
                // Ignore - table doesn't exist
            }
            
            var tablePrefix = Config!.DatabaseConnection.TablePrefix;
            var createPlayerGroupsTableSql = $@"
                CREATE TABLE IF NOT EXISTS {tablePrefix}player_groups (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    steamid64 BIGINT NOT NULL,
                    group_name VARCHAR(50) NOT NULL,
                    name VARCHAR(255) NOT NULL,
                    expires INT UNSIGNED NOT NULL,
                    UNIQUE INDEX unique_player_group (steamid64, group_name),
                    INDEX(steamid64)
                );
            ";
            
            connection.Execute(createPlayerGroupsTableSql);
            
            // Migrate data from legacy table if it exists
            if (legacyTableExists)
            {
                MigrateExistingData(connection);
            }
            
            _databaseLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating database tables: {ex.Message}");
        }
    }

    private static void CreateWeaponPreferencesTable()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            
            var tablePrefix = Config!.DatabaseConnection.TablePrefix;
            var createTableSql = $@"
                CREATE TABLE IF NOT EXISTS {tablePrefix}vip_weapon_preferences (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    steamid64 BIGINT NOT NULL,
                    team_num INT NOT NULL,
                    primary_weapon VARCHAR(64),
                    secondary_weapon VARCHAR(64),
                    last_updated BIGINT NOT NULL,
                    UNIQUE INDEX unique_player_team (steamid64, team_num)
                );
            ";
            
            connection.Execute(createTableSql);
            Console.WriteLine("[Mesharsky - VIP] Weapon preferences table initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesharsky - VIP] Error creating weapon preferences table: {ex.Message}");
        }
    }

    private static void SavePlayerWeaponPreferences(CCSPlayerController player)
    {
        try
        {
            if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer))
                return;
                    
            if (cachedPlayer.WeaponSelections.Count == 0)
                return;
                    
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var tablePrefix = Config!.DatabaseConnection.TablePrefix;
            using var transaction = connection.BeginTransaction();
            
            try
            {
                connection.Execute(
                    $"DELETE FROM {tablePrefix}vip_weapon_preferences WHERE steamid64 = @SteamID",
                    new { player.SteamID },
                    transaction
                );
                
                foreach (var selection in cachedPlayer.WeaponSelections.Values)
                {
                    if (string.IsNullOrEmpty(selection.PrimaryWeapon) && string.IsNullOrEmpty(selection.SecondaryWeapon))
                        continue;
                        
                    connection.Execute(
                        $@"INSERT INTO {tablePrefix}vip_weapon_preferences 
                        (steamid64, team_num, primary_weapon, secondary_weapon, last_updated) 
                        VALUES (@SteamID, @TeamNum, @PrimaryWeapon, @SecondaryWeapon, @LastUpdated)",
                        new {
                            player.SteamID,
                            selection.TeamNum,
                            selection.PrimaryWeapon,
                            selection.SecondaryWeapon,
                            LastUpdated = selection.SavedTime
                        },
                        transaction
                    );
                }
                
                transaction.Commit();
                Console.WriteLine($"[Mesharsky - VIP] Saved weapon preferences for player [ SteamID: {player.SteamID} ]");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[Mesharsky - VIP] Error saving weapon preferences (transaction rolled back): {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesharsky - VIP] Error saving weapon preferences: {ex.Message}");
        }
    }

    private void LoadPlayerWeaponPreferences(CCSPlayerController player)
    {
        try
        {
            if (!CanUseWeaponMenu(player))
                return;
                
            if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer))
                return;
                    
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var tablePrefix = Config!.DatabaseConnection.TablePrefix;
            var preferences = connection.Query<dynamic>(
                $"SELECT * FROM {tablePrefix}vip_weapon_preferences WHERE steamid64 = @SteamID",
                new { player.SteamID }
            ).ToList();
            
            if (preferences.Count == 0)
                return;
                
            foreach (var pref in preferences)
            {
                var teamNum = (int)pref.team_num;
                
                cachedPlayer.WeaponSelections[teamNum] = new PlayerWeaponSelection
                {
                    PrimaryWeapon = pref.primary_weapon,
                    SecondaryWeapon = pref.secondary_weapon,
                    TeamNum = teamNum,
                    SavedTime = (long)pref.last_updated
                };
            }
            
            Console.WriteLine($"[Mesharsky - VIP] Loaded {preferences.Count} weapon preferences for player [ SteamID: {player.SteamID} ]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesharsky - VIP] Error loading weapon preferences: {ex.Message}");
        }
    }

    private static void DeleteWeaponPreferencesFromDb(ulong steamId, int teamNum)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            var tablePrefix = Config!.DatabaseConnection.TablePrefix;
            connection.Execute(
                $"DELETE FROM {tablePrefix}vip_weapon_preferences WHERE steamid64 = @SteamID AND team_num = @TeamNum",
                new { SteamID = steamId, TeamNum = teamNum }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesharsky - VIP] Error deleting weapon preferences: {ex.Message}");
        }
    }

    private static void DB_CreateVipTestTable()
    {
        using var connection = new MySqlConnection(_connectionString);
        try
        {
            connection.Open();
            
            var tablePrefix = Config!.DatabaseConnection.TablePrefix;
            var createVipTestTableSql = $@"
                CREATE TABLE IF NOT EXISTS {tablePrefix}vip_test_history (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    steamid64 BIGINT NOT NULL,
                    name VARCHAR(255) NOT NULL,
                    last_test_date INT UNSIGNED NOT NULL,
                    UNIQUE INDEX unique_player (steamid64)
                );
            ";
            
            connection.Execute(createVipTestTableSql);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating vip_test_history table: {ex.Message}");
        }
    }
    
    private static void MigrateExistingData(MySqlConnection connection)
    {
        var tablePrefix = Config!.DatabaseConnection.TablePrefix;
        try
        {
          
            var playerGroupsCount = connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {tablePrefix}player_groups");
            
            var playersCount = 0;
            try
            {
                playersCount = connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM players");
            }
            catch (Exception)
            {
                return;
            }

            if (playerGroupsCount != 0 || playersCount <= 0) return;
            Console.WriteLine("[Mesharsky - VIP] Migrating existing player data to new multi-group system...");
            
            try
            {
                var migrateSql = $@"
                    INSERT INTO {tablePrefix}player_groups (steamid64, group_name, name, expires)
                    SELECT steamid64, `group`, name, expires FROM players
                    WHERE `group` != 'None' AND `group` != ''
                ";
                    
                var rowsAffected = connection.Execute(migrateSql);
                Console.WriteLine($"[Mesharsky - VIP] Migration complete. {rowsAffected} player groups migrated.");
                
                connection.Execute("DROP TABLE IF EXISTS players");
                Console.WriteLine("[Mesharsky - VIP] Legacy table 'players' dropped after successful migration.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Mesharsky - VIP] Error during data migration SQL execution: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesharsky - VIP] Error during data migration: {ex.Message}");
        }
    }
    
    private static bool HasUsedVipTest(ulong steamId)
    {
        if (Config!.VipTest.TestCooldown == 0)
        {
            // If cooldown is 0 (forever), just check if they've ever used it
            using var connection = new MySqlConnection(_connectionString);
            var tablePrefix = Config!.DatabaseConnection.TablePrefix;
            var count = connection.ExecuteScalar<int>(
                $"SELECT COUNT(*) FROM {tablePrefix}vip_test_history WHERE steamid64 = @SteamID",
                new { SteamID = steamId }
            );
            
            return count > 0;
        }
        else
        {
            // Check if they've used it within the cooldown period
            using var connection = new MySqlConnection(_connectionString);
            var tablePrefix = Config!.DatabaseConnection.TablePrefix;
            var lastTestDate = connection.ExecuteScalar<int?>(
                $"SELECT last_test_date FROM {tablePrefix}vip_test_history WHERE steamid64 = @SteamID",
                new { SteamID = steamId }
            );
            
            if (lastTestDate == null)
                return false;
            
            var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cooldownSeconds = Config.VipTest.TestCooldown * 86400; 
            
            return (currentTime - lastTestDate.Value) < cooldownSeconds;
        }
    }
    
    private static int GetRemainingCooldownDays(ulong steamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var tablePrefix = Config!.DatabaseConnection.TablePrefix;
        var lastTestDate = connection.ExecuteScalar<int?>(
            $"SELECT last_test_date FROM {tablePrefix}vip_test_history WHERE steamid64 = @SteamID",
            new { SteamID = steamId }
        );
        
        if (lastTestDate == null)
            return 0;
        
        var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var secondsElapsed = currentTime - lastTestDate.Value;
        var cooldownSeconds = Config!.VipTest.TestCooldown * 86400; 
        
        if (secondsElapsed >= cooldownSeconds)
            return 0;
        
        return (int)Math.Ceiling((cooldownSeconds - secondsElapsed) / 86400.0);
    }
    
    private static void RecordVipTestUsage(ulong steamId, string playerName)
    {
        using var connection = new MySqlConnection(_connectionString);
        var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tablePrefix = Config!.DatabaseConnection.TablePrefix;
        connection.Execute(
            $"INSERT INTO {tablePrefix}vip_test_history (steamid64, name, last_test_date) " +
            "VALUES (@SteamID, @Name, @Date) " +
            "ON DUPLICATE KEY UPDATE name = @Name, last_test_date = @Date",
            new { SteamID = steamId, Name = playerName, Date = currentTime }
        );
    }

    private static void DB_SyncGroups()
    {
        var tablePrefix = Config!.DatabaseConnection.TablePrefix;
        var tableString = $@"CREATE TABLE IF NOT EXISTS {tablePrefix}vip_groups (
            id INT AUTO_INCREMENT PRIMARY KEY,
            name VARCHAR(50) NOT NULL UNIQUE,
            flag VARCHAR(50) NOT NULL,
            description TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )";

        using var connection = new MySqlConnection(_connectionString);
        connection.Execute(tableString);

        // Get existing group names in one query
        var existingGroupNames = connection.Query<string>($"SELECT name FROM {tablePrefix}vip_groups").ToHashSet();
        var configGroupNames = Config!.GroupSettings.Select(g => g.Name).ToHashSet();

        // Batch UPSERT all config groups in one query
        if (Config.GroupSettings.Count > 0)
        {
            var groupData = Config.GroupSettings.Select(g => new { 
                Name = g.Name, 
                Flag = g.Flag, 
                Description = ""
            }).ToList();

            connection.Execute(
                $@"INSERT INTO {tablePrefix}vip_groups (name, flag, description) 
                  VALUES (@Name, @Flag, @Description)
                  ON DUPLICATE KEY UPDATE flag = VALUES(flag), description = VALUES(description)",
                groupData
            );
            Console.WriteLine($"[Mesharsky - VIP] Synced {groupData.Count} groups to database");
        }

        // Remove groups that don't exist in config (batch operation)
        var groupsToRemove = existingGroupNames.Except(configGroupNames).ToList();
        
        if (groupsToRemove.Count > 0)
        {
            // Batch delete groups that don't exist in config
            var deletePlaceholders = string.Join(",", groupsToRemove.Select((_, i) => $"@del{i}"));
            var deleteParams = groupsToRemove.Select((name, i) => new { Key = $"@del{i}", Value = name })
                                          .ToDictionary(x => x.Key, x => (object)x.Value);
            
            var removedCount = connection.Execute(
                $"DELETE FROM {tablePrefix}vip_groups WHERE name IN ({deletePlaceholders})",
                deleteParams
            );
            Console.WriteLine($"[Mesharsky - VIP] Removed {removedCount} groups from database (not in config)");
        }
        
        Console.WriteLine($"[Mesharsky - VIP] Group synchronization complete");
    }

}
