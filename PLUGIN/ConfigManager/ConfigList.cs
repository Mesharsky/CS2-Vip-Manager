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
    }

    public class DatabaseConnectionConfig
    {
        public required string Host { get; set; }
        public required string Username { get; set; }
        public required string Database { get; set; }
        public required string Password { get; set; }
        public int Port { get; set; }
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

    public class CommandSettingsConfig
    {
        // Player commands
        public List<string> VipCommand { get; set; } = new List<string>();
        public List<string> BenefitsCommand { get; set; } = new List<string>();
        public List<string> OnlineCommand { get; set; } = new List<string>();
    
        // Admin commands
        public List<string> AddVipCommand { get; set; } = new List<string>();
        public List<string> RemoveVipCommand { get; set; } = new List<string>();
        public List<string> ListVipCommand { get; set; } = new List<string>();
        public List<string> ListAvailableCommand { get; set; } = new List<string>();
        public List<string> AddVipSteamCommand { get; set; } = new List<string>();
        public List<string> RemoveVipSteamCommand { get; set; } = new List<string>();
    }

    public class GroupSettingsConfig
    {
        public required string Name { get; set; }
        public required string Flag { get; set; }

        // Bonuses
        public int PlayerHp { get; set; }
        public int PlayerMaxHp { get; set; }
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

        // Special Bonuses
        public int PlayerExtraJumps { get; set; }
        public double PlayerExtraJumpHeight { get; set; }
        public bool PlayerBunnyhop { get; set; }
        public bool PlayerWeaponmenu { get; set; }
    }
}