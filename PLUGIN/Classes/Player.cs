using System.Collections.Concurrent;
using Dapper;
using MySqlConnector;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private static readonly ConcurrentDictionary<ulong, Player> PlayerCache = new();

    public class PlayerGroup
    {
        public required string GroupName { get; set; }
        public required string PlayerName { get; set; }
        public int ExpiryTime { get; set; }
        public bool Active { get; set; }
    }

    public class Player
    {
        public required string PlayerName { get; set; }
        public ulong PlayerSteamId { get; set; }
        
        public string? LoadedGroup { get; set; }
        public int GroupExpiryTime { get; set; }
        public bool Active { get; set; }
        
        public List<PlayerGroup> Groups { get; set; } = [];
        public Dictionary<int, PlayerWeaponSelection> WeaponSelections { get; set; } = new();
        public PlayerSettings BonusSettings { get; set; } = new();
        
        public PlayerGroup? GetPrimaryGroup()
        {
            return Groups.FirstOrDefault(g => g.Active);
        }
        
        public bool HasGroup(string groupName)
        {
            return Groups.Any(g => g.GroupName == groupName && g.Active);
        }
    }

    private static Player GetOrCreatePlayer(ulong steamId, string playerName)
    {
        if (PlayerCache.TryGetValue(steamId, out var player))
        {
            if (player.PlayerName == playerName || string.IsNullOrEmpty(playerName)) return player;
            
            player.PlayerName = playerName;
            
            foreach (var group in player.Groups)
            {
                group.PlayerName = playerName;
            }
            
            UpdatePlayerName(steamId, playerName);

            return player;
        }
        
        var playerData = new Player
        {
            PlayerName = playerName,
            PlayerSteamId = steamId,
            LoadedGroup = null,
            GroupExpiryTime = 0,
            Active = false
        };
        
        using var connection = new MySqlConnection(_connectionString);
        
        var tablePrefix = Config!.DatabaseConnection.TablePrefix;
        var groups = connection.Query<PlayerGroup>(
            $"SELECT group_name AS GroupName, name AS PlayerName, expires AS ExpiryTime FROM {tablePrefix}player_groups WHERE steamid64 = @SteamID",
            new { SteamID = steamId }
        ).ToList();
        
        Console.WriteLine($"[Mesharsky - VIP] Retrieved player data: Name={playerData.PlayerName}, Groups={groups.Count}");
        
        // Process groups
        foreach (var group in groups)
        {
            var service = ServiceManager.GetService(group.GroupName);
            if (service != null)
            {
                Console.WriteLine($"[Mesharsky - VIP] Found service for group: {service.Name}");
                Console.WriteLine($"[Mesharsky - VIP] Current Unix Time: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, Group Expiry Time: {group.ExpiryTime}");
                
                group.Active = group.ExpiryTime == 0 || group.ExpiryTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                
                if (group.PlayerName != playerName && !string.IsNullOrEmpty(playerName))
                {
                    group.PlayerName = playerName;
                    
                    connection.Execute(
                        $"UPDATE {tablePrefix}player_groups SET name = @Name WHERE steamid64 = @SteamID AND group_name = @GroupName",
                        new { Name = playerName, SteamID = steamId, GroupName = group.GroupName }
                    );
                }
                
                Console.WriteLine($"[Mesharsky - VIP] Group Active Status: {group.Active}");
            }
            else
            {
                Console.WriteLine($"[Mesharsky - VIP] No service found for group: {group.GroupName}");
                group.Active = false;
            }
        }
        
        playerData.Groups = groups;
        
        var primaryGroup = playerData.GetPrimaryGroup();
        if (primaryGroup != null)
        {
            playerData.LoadedGroup = primaryGroup.GroupName;
            playerData.GroupExpiryTime = primaryGroup.ExpiryTime;
            playerData.Active = true;
        }
        else
        {
            playerData.LoadedGroup = null;
            playerData.GroupExpiryTime = 0;
            playerData.Active = false;
        }
        
        PlayerCache[steamId] = playerData;
        return playerData;
    }

    private static void UpdatePlayerName(ulong steamId, string playerName)
    {
        using var connection = new MySqlConnection(_connectionString);
        
        var tablePrefix = Config!.DatabaseConnection.TablePrefix;
        connection.Execute(
            $"UPDATE {tablePrefix}player_groups SET name = @Name WHERE steamid64 = @SteamID",
            new { Name = playerName, SteamID = steamId }
        );
    }

    private static void UpdatePlayer(Player player)
    {
        PlayerCache[player.PlayerSteamId] = player;

        using var connection = new MySqlConnection(_connectionString);
    
        var tablePrefix = Config!.DatabaseConnection.TablePrefix;
        foreach (var group in from @group in player.Groups let isNightVipGroup = Config!.NightVip.Enabled && 
                     @group.GroupName == Config.NightVip.InheritGroup && 
                     @group.ExpiryTime == 0 && 
                     IsNightVipTime() where !isNightVipGroup select @group)
        {
            connection.Execute(
                $"INSERT INTO {tablePrefix}player_groups (steamid64, group_name, name, expires) " +
                "VALUES (@SteamID, @GroupName, @Name, @ExpiryTime) " +
                "ON DUPLICATE KEY UPDATE expires = @ExpiryTime, name = @Name",
                new { 
                    SteamID = player.PlayerSteamId,
                    group.GroupName, 
                    Name = player.PlayerName,
                    group.ExpiryTime 
                }
            );
        }
    }

    private static void AddGroupToPlayer(ulong steamId, string groupName, int expiryTime = 0)
    {
        var player = GetOrCreatePlayer(steamId, string.Empty);
    
        var isNightVipGroup = Config!.NightVip.Enabled && 
                              groupName == Config.NightVip.InheritGroup && 
                              expiryTime == 0 && 
                              IsNightVipTime();
                         
        var existingGroup = player.Groups.FirstOrDefault(g => g.GroupName == groupName);
        if (existingGroup != null)
        {
            if (!isNightVipGroup && (expiryTime == 0 || (existingGroup.ExpiryTime != 0 && expiryTime > existingGroup.ExpiryTime)))
            {
                existingGroup.ExpiryTime = expiryTime;
            }
            existingGroup.Active = expiryTime == 0 || expiryTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        else
        {
            player.Groups.Add(new PlayerGroup
            {
                GroupName = groupName,
                PlayerName = player.PlayerName,
                ExpiryTime = expiryTime,
                Active = expiryTime == 0 || expiryTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
        }
    
        var primaryGroup = player.GetPrimaryGroup();
        if (primaryGroup != null)
        {
            player.LoadedGroup = primaryGroup.GroupName;
            player.GroupExpiryTime = primaryGroup.ExpiryTime;
            player.Active = true;
        }
    
        UpdatePlayer(player);
    }

    private static void RemoveGroupFromPlayer(ulong steamId, string groupName)
    {
        var player = GetOrCreatePlayer(steamId, string.Empty);
    
        var groupToRemove = player.Groups.FirstOrDefault(g => g.GroupName == groupName);
        if (groupToRemove == null) return;
        
        using var connection = new MySqlConnection(_connectionString);
        var tablePrefix = Config!.DatabaseConnection.TablePrefix;
        connection.Execute(
            $"DELETE FROM {tablePrefix}player_groups WHERE steamid64 = @SteamID AND group_name = @GroupName",
            new { SteamID = steamId, GroupName = groupName }
        );
        
        player.Groups.Remove(groupToRemove);
    
        var primaryGroup = player.GetPrimaryGroup();
        if (primaryGroup != null)
        {
            player.LoadedGroup = primaryGroup.GroupName;
            player.GroupExpiryTime = primaryGroup.ExpiryTime;
            player.Active = true;
        }
        else
        {
            player.LoadedGroup = null;
            player.GroupExpiryTime = 0;
            player.Active = false;
        }
        
        PlayerCache[player.PlayerSteamId] = player;
    }
}