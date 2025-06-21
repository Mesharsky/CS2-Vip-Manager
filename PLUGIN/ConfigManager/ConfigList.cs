using System.Collections.Generic;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public class VipConfig
    {
        public required DatabaseConnectionConfig DatabaseConnection { get; set; }
        public required PluginSettingsConfig PluginSettings { get; set; }
        public required CommandSettingsConfig CommandSettings { get; set; }
        public required List<GroupSettingsConfig> GroupSettings { get; set; }
        public required NightVipConfig NightVip { get; set; }
        public required VipTestConfig VipTest { get; set; }
    }

    public class DatabaseConnectionConfig
    {
        public required string Host { get; set; }
        public required string Username { get; set; }
        public required string Database { get; set; }
        public required string Password { get; set; }
        public int Port { get; set; }
        public string TablePrefix { get; set; } = "";
    }

    public class PluginSettingsConfig
    {
        public string PluginTag { get; set; } = "{red}[VIP]{default}";
        public bool OnlineList { get; set; }
        public bool BonusesList { get; set; }
        public string? BypassFlag { get; set; }
        public string? BypassFlagGive { get; set; }
    }

    public class NightVipConfig
    {
        public bool Enabled { get; set; }
        public string InheritGroup { get; set; } = "VIP";
        public string Flag { get; set; } = "@mesharsky/nightvip";
        public int StartHour { get; set; } = 18;
        public int EndHour { get; set; } = 6;
    }
    
    public class VipTestConfig
    {
        public bool Enabled { get; set; } = false;
        public string TestGroup { get; set; } = "VIP";
        public int TestDuration { get; set; } = 7;
        public int TestCooldown { get; set; } = 0; // 0 = forever, otherwise days
        public List<string> TestCommand { get; set; } = [];
    }

    public class CommandSettingsConfig
    {
        // Player commands
        public List<string> VipCommand { get; set; } = [];
        public List<string> BenefitsCommand { get; set; } = [];
        public List<string> OnlineCommand { get; set; } = [];
        public List<string> WeaponsMenuCommand { get; set; } = [];
        public List<string> WeaponsMenuResetCommand { get; set; } = [];
    
        // Admin commands
        public List<string> AddVipCommand { get; set; } = [];
        public List<string> RemoveVipCommand { get; set; } = [];
        public List<string> ListVipCommand { get; set; } = [];
        public List<string> ListAvailableCommand { get; set; } = [];
        public List<string> AddVipSteamCommand { get; set; } = [];
        public List<string> RemoveVipSteamCommand { get; set; } = [];
    }
    
    public class SmokeColorConfig
    {
        public bool Enabled { get; set; }
        public bool Random { get; set; }
        public int Red { get; set; } = 255;
        public int Green { get; set; } = 255;
        public int Blue { get; set; } = 255;
    }

    public class WeaponMenuConfig
    {
        public bool Enabled { get; set; } = false;
        public int MinRound { get; set; } = 1;
        public List<string> CTPrimaryWeapons { get; set; } = [];
        public List<string> CTSecondaryWeapons { get; set; } = [];
        public List<string> TPrimaryWeapons { get; set; } = [];
        public List<string> TSecondaryWeapons { get; set; } = [];
    }

    public class GroupSettingsConfig
    {
        public required string Name { get; set; }
        public required string Flag { get; set; }
        
        // Health & Armor Bonuses
        public int PlayerHp { get; set; } = 100;
        public int PlayerMaxHp { get; set; } = 100;
        public bool PlayerVest { get; set; }
        public int PlayerVestRound { get; set; }
        public bool PlayerHelmet { get; set; }
        public int PlayerHelmetRound { get; set; }
        public bool PlayerDefuser { get; set; }
        
        // Grenades
        public int HeAmount { get; set; }
        public int FlashAmount { get; set; }
        public int SmokeAmount { get; set; }
        public int DecoyAmount { get; set; }
        public int MolotovAmount { get; set; }
        public int HealthshotAmount { get; set; }
        
        // Special Abilities
        public int PlayerExtraJumps { get; set; }
        public double PlayerExtraJumpHeight { get; set; }
        public bool PlayerBunnyhop { get; set; }
        public bool PlayerWeaponmenu { get; set; }
        
        public bool InfiniteAmmo { get; set; }
        public bool FastReload { get; set; }
        public bool KillScreen { get; set; }
        
        // Kill Health Bonuses
        public int HealthPerKill { get; set; } = 0;
        public int HealthPerHeadshot { get; set; } = 0;
        public int HealthPerKnifeKill { get; set; } = 0;
        public int HealthPerNoScope { get; set; } = 0;
        
        // Smoke Color
        public required SmokeColorConfig SmokeColor { get; set; }
        
        // Weapon Menu
        public required WeaponMenuConfig WeaponMenu { get; set; } = new();
    }
}
