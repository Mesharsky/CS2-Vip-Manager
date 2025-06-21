using Tomlyn;
using Tomlyn.Model;

namespace Mesharsky_Vip;

#pragma warning disable 8601, 8604

public partial class MesharskyVip
{
    public static VipConfig? Config { get; private set; }

    public void LoadConfiguration()
    {
        var configPath = Path.Combine(ModuleDirectory, "Config/Configuration.toml");
        var configText = File.ReadAllText(configPath);
        var model = Toml.ToModel(configText);

        var dbTable = (TomlTable)model["DatabaseConnection"];
        var databaseConnection = new DatabaseConnectionConfig
        {
            Host = dbTable["host"].ToString(),
            Username = dbTable["username"].ToString(),
            Database = dbTable["database"].ToString(),
            Password = dbTable["password"].ToString(),
            Port = int.Parse(dbTable["port"].ToString()),
            TablePrefix = dbTable["table_prefix"].ToString()
        };

        var pluginTable = (TomlTable)model["PluginSettings"];
        var pluginSettings = new PluginSettingsConfig
        {
            PluginTag = pluginTable.TryGetValue("PluginTag", out var tag) ? tag.ToString() : "{red}[VIP]{default}",
            OnlineList = bool.Parse(pluginTable["online_list"].ToString()),
            BonusesList = bool.Parse(pluginTable["bonuses_list"].ToString()),
            BypassFlag = pluginTable["bypass_flag"].ToString(),
            BypassFlagGive = pluginTable["bypass_flag_give"].ToString()
        };
        
        var vipTestConfig = new VipTestConfig();
        if (model.TryGetValue("VipTest", out var vipTestTableObj) && vipTestTableObj is TomlTable vipTestTable)
        {
            if (vipTestTable.TryGetValue("enabled", out var enabledObj) && enabledObj is bool enabled)
                vipTestConfig.Enabled = enabled;
    
            if (vipTestTable.TryGetValue("test_group", out var testGroupObj) && testGroupObj is string testGroup)
                vipTestConfig.TestGroup = testGroup;
    
            if (vipTestTable.TryGetValue("test_duration", out var testDurationObj) && testDurationObj is long testDuration)
                vipTestConfig.TestDuration = (int)testDuration;
    
            if (vipTestTable.TryGetValue("test_cooldown", out var testCooldownObj) && testCooldownObj is long testCooldown)
                vipTestConfig.TestCooldown = (int)testCooldown;
    
            if (vipTestTable.TryGetValue("test_command", out var testCommandObj) && testCommandObj is TomlArray testCommandArray)
            {
                vipTestConfig.TestCommand = [.. testCommandArray.Cast<string>()];
            }
        }

        var nightVipConfig = new NightVipConfig();
        if (model.TryGetValue("NightVip", out var nightVipTableObj) && nightVipTableObj is TomlTable nightVipTable)
        {
            if (nightVipTable.TryGetValue("enabled", out var enabledObj) && enabledObj is bool enabled)
                nightVipConfig.Enabled = enabled;
            
            if (nightVipTable.TryGetValue("inherit_group", out var inheritGroupObj) && inheritGroupObj is string inheritGroup)
                nightVipConfig.InheritGroup = inheritGroup;
            
            if (nightVipTable.TryGetValue("flag", out var flagObj) && flagObj is string flag)
                nightVipConfig.Flag = flag;
            
            if (nightVipTable.TryGetValue("start_hour", out var startHourObj) && startHourObj is long startHour)
                nightVipConfig.StartHour = (int)startHour;
            
            if (nightVipTable.TryGetValue("end_hour", out var endHourObj) && endHourObj is long endHour)
                nightVipConfig.EndHour = (int)endHour;
        }

        var commandSettings = new CommandSettingsConfig();
        
        if (model.TryGetValue("CommandSettings", out var commandTableObj) && commandTableObj is TomlTable commandTable)
        {
            // Parse player commands
            if (commandTable.TryGetValue("vip_command", out var vipCmdObj) && vipCmdObj is TomlArray vipCmdArray)
            {
                commandSettings.VipCommand = [.. vipCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("benefits_command", out var benefitsCmdObj) && benefitsCmdObj is TomlArray benefitsCmdArray)
            {
                commandSettings.BenefitsCommand = [.. benefitsCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("online_command", out var onlineCmdObj) && onlineCmdObj is TomlArray onlineCmdArray)
            {
                commandSettings.OnlineCommand = [.. onlineCmdArray.Cast<string>()];
            }
            
            
            if (commandTable.TryGetValue("weapons_menu_command", out var weaponsMenuCmdObj) && weaponsMenuCmdObj is TomlArray weaponsMenuCmdArray)
            {
                commandSettings.WeaponsMenuCommand = [.. weaponsMenuCmdArray.Cast<string>()];
            }

            if (commandTable.TryGetValue("weapons_menu_reset_command", out var weaponsMenuResetCmdObj) && weaponsMenuResetCmdObj is TomlArray weaponsMenuResetCmdArray)
            {
                commandSettings.WeaponsMenuResetCommand = [.. weaponsMenuResetCmdArray.Cast<string>()];
            }
            
            // Parse admin commands
            if (commandTable.TryGetValue("addvip_command", out var addVipCmdObj) && addVipCmdObj is TomlArray addVipCmdArray)
            {
                commandSettings.AddVipCommand = [.. addVipCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("removevip_command", out var removeVipCmdObj) && removeVipCmdObj is TomlArray removeVipCmdArray)
            {
                commandSettings.RemoveVipCommand = [.. removeVipCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("listvip_command", out var listVipCmdObj) && listVipCmdObj is TomlArray listVipCmdArray)
            {
                commandSettings.ListVipCommand = [.. listVipCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("listavailable_command", out var listAvailableCmdObj) && listAvailableCmdObj is TomlArray listAvailableCmdArray)
            {
                commandSettings.ListAvailableCommand = [.. listAvailableCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("addvipsteam_command", out var addVipSteamCmdObj) && addVipSteamCmdObj is TomlArray addVipSteamCmdArray)
            {
                commandSettings.AddVipSteamCommand = [.. addVipSteamCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("removevipsteam_command", out var removeVipSteamCmdObj) && removeVipSteamCmdObj is TomlArray removeVipSteamCmdArray)
            {
                commandSettings.RemoveVipSteamCommand = [.. removeVipSteamCmdArray.Cast<string>()];
            }
        }

        var groupSettings = new List<GroupSettingsConfig>();
        var groupSettingsArray = (TomlTableArray)model["GroupSettings"];
        foreach (var group in groupSettingsArray)
        {
            var groupConfig = new GroupSettingsConfig
            {
                Name = group["name"].ToString(),
                Flag = group["flag"].ToString(),
                PlayerHp = int.Parse(group["player_hp"].ToString()),
                PlayerMaxHp = int.Parse(group["player_max_hp"].ToString()),
                PlayerVest = bool.Parse(group["player_vest"].ToString()),
                PlayerVestRound = int.Parse(group["player_vest_round"].ToString()),
                PlayerHelmet = bool.Parse(group["player_helmet"].ToString()),
                PlayerHelmetRound = int.Parse(group["player_helmet_round"].ToString()),
                PlayerDefuser = bool.Parse(group["player_defuser"].ToString()),
                HeAmount = int.Parse(group["he_amount"].ToString()),
                FlashAmount = int.Parse(group["flash_amount"].ToString()),
                SmokeAmount = int.Parse(group["smoke_amount"].ToString()),
                DecoyAmount = int.Parse(group["decoy_amount"].ToString()),
                MolotovAmount = int.Parse(group["molotov_amount"].ToString()),
                HealthshotAmount = int.Parse(group["healthshot_amount"].ToString()),
                PlayerExtraJumps = int.Parse(group["player_extra_jumps"].ToString()),
                PlayerExtraJumpHeight = double.Parse(group["player_extra_jump_height"].ToString()),
                PlayerBunnyhop = bool.Parse(group["player_bunnyhop"].ToString()),
                SmokeColor = new SmokeColorConfig(),
                InfiniteAmmo = group.TryGetValue("infinite_ammo", out var infiniteAmmoObj) && 
                               infiniteAmmoObj is bool infiniteAmmo && infiniteAmmo,
                FastReload = group.TryGetValue("fast_reload", out var fastReloadObj) && 
                             fastReloadObj is bool fastReload && fastReload,
                KillScreen = group.TryGetValue("kill_screen", out var killScreenObj) && 
                             killScreenObj is bool killScreen and true,
                HealthPerKill = group.TryGetValue("health_per_kill", out var healthPerKillObj) && 
                                healthPerKillObj is long healthPerKill ? (int)healthPerKill : 0,
                HealthPerHeadshot = group.TryGetValue("health_per_headshot", out var healthPerHeadshotObj) && 
                                    healthPerHeadshotObj is long healthPerHeadshot ? (int)healthPerHeadshot : 0,
                HealthPerKnifeKill = group.TryGetValue("health_per_knife_kill", out var healthPerKnifeKillObj) && 
                                     healthPerKnifeKillObj is long healthPerKnifeKill ? (int)healthPerKnifeKill : 0,
                HealthPerNoScope = group.TryGetValue("health_per_noscope", out var healthPerNoScopeObj) && 
                                   healthPerNoScopeObj is long healthPerNoScope ? (int)healthPerNoScope : 0,
                WeaponMenu = new WeaponMenuConfig()             
            };

            if (group.TryGetValue("smoke_color", out var smokeColorObj) && smokeColorObj is TomlTable smokeColorTable)
            {
                if (smokeColorTable.TryGetValue("enabled", out var enabledObj) && enabledObj is bool enabled)
                    groupConfig.SmokeColor.Enabled = enabled;
    
                if (smokeColorTable.TryGetValue("random", out var randomObj) && randomObj is bool random)
                    groupConfig.SmokeColor.Random = random;
    
                if (smokeColorTable.TryGetValue("red", out var redObj) && redObj is long red)
                    groupConfig.SmokeColor.Red = (int)red;
    
                if (smokeColorTable.TryGetValue("green", out var greenObj) && greenObj is long green)
                    groupConfig.SmokeColor.Green = (int)green;
    
                if (smokeColorTable.TryGetValue("blue", out var blueObj) && blueObj is long blue)
                    groupConfig.SmokeColor.Blue = (int)blue;
            }

            groupSettings.Add(groupConfig);
            
            var weaponMenuConfig = ParseWeaponMenuConfig(group);
            groupConfig.WeaponMenu = weaponMenuConfig;

            var service = new Service
            {
                Name = groupConfig.Name,
                Flag = groupConfig.Flag,
                PlayerHp = groupConfig.PlayerHp,
                PlayerMaxHp = groupConfig.PlayerMaxHp,
                PlayerVest = groupConfig.PlayerVest,
                PlayerVestRound = groupConfig.PlayerVestRound,
                PlayerHelmet = groupConfig.PlayerHelmet,
                PlayerHelmetRound = groupConfig.PlayerHelmetRound,
                PlayerDefuser = groupConfig.PlayerDefuser,
                HeAmount = groupConfig.HeAmount,
                FlashAmount = groupConfig.FlashAmount,
                SmokeAmount = groupConfig.SmokeAmount,
                DecoyAmount = groupConfig.DecoyAmount,
                MolotovAmount = groupConfig.MolotovAmount,
                HealthshotAmount = groupConfig.HealthshotAmount,
                PlayerExtraJumps = groupConfig.PlayerExtraJumps,
                PlayerExtraJumpHeight = groupConfig.PlayerExtraJumpHeight,
                PlayerBunnyhop = groupConfig.PlayerBunnyhop,
                SmokeColorEnabled = groupConfig.SmokeColor.Enabled,
                SmokeColorRandom = groupConfig.SmokeColor.Random,
                SmokeColorR = groupConfig.SmokeColor.Red,
                SmokeColorG = groupConfig.SmokeColor.Green,
                SmokeColorB = groupConfig.SmokeColor.Blue,
                InfiniteAmmo = groupConfig.InfiniteAmmo,
                FastReload = groupConfig.FastReload,
                KillScreen = groupConfig.KillScreen,
                HealthPerKill = groupConfig.HealthPerKill,
                HealthPerHeadshot = groupConfig.HealthPerHeadshot,
                HealthPerKnifeKill = groupConfig.HealthPerKnifeKill,
                HealthPerNoScope = groupConfig.HealthPerNoScope,
                WeaponMenu = groupConfig.WeaponMenu
            };

            ServiceManager.RegisterService(service);
            Console.WriteLine($"[Mesharsky - VIP] Registered service: {service.Name}");
        }

        Config = new VipConfig
        { 
            DatabaseConnection = databaseConnection,
            PluginSettings = pluginSettings,
            CommandSettings = commandSettings,
            GroupSettings = groupSettings,
            NightVip = nightVipConfig,
            VipTest = vipTestConfig
        };


        

    }
    
    private WeaponMenuConfig ParseWeaponMenuConfig(TomlTable group)
    {
        var weaponMenuConfig = new WeaponMenuConfig();
        
        if (group.TryGetValue("weapon_menu", out var weaponMenuObj) && weaponMenuObj is TomlTable weaponMenuTable)
        {
            if (weaponMenuTable.TryGetValue("enabled", out var enabledObj) && enabledObj is bool enabled)
                weaponMenuConfig.Enabled = enabled;
                
            if (weaponMenuTable.TryGetValue("min_round", out var minRoundObj) && minRoundObj is long minRound)
                weaponMenuConfig.MinRound = (int)minRound;
                
            if (weaponMenuTable.TryGetValue("counter_terrorists_primary_weapons", out var ctPrimaryObj) && ctPrimaryObj is TomlArray ctPrimaryArray)
            {
                weaponMenuConfig.CTPrimaryWeapons = ctPrimaryArray.Cast<string>().ToList();
            }
            else if (weaponMenuTable.TryGetValue("ct_primary", out var ctPrimaryAltObj) && ctPrimaryAltObj is TomlArray ctPrimaryAltArray)
            {
                weaponMenuConfig.CTPrimaryWeapons = ctPrimaryAltArray.Cast<string>().ToList();
            }
            
            if (weaponMenuTable.TryGetValue("counter_terrorists_secondary_weapons", out var ctSecondaryObj) && ctSecondaryObj is TomlArray ctSecondaryArray)
            {
                weaponMenuConfig.CTSecondaryWeapons = ctSecondaryArray.Cast<string>().ToList();
            }
            else if (weaponMenuTable.TryGetValue("ct_secondary", out var ctSecondaryAltObj) && ctSecondaryAltObj is TomlArray ctSecondaryAltArray)
            {
                weaponMenuConfig.CTSecondaryWeapons = ctSecondaryAltArray.Cast<string>().ToList();
            }
            
            if (weaponMenuTable.TryGetValue("terrorist_primary_weapons", out var tPrimaryObj) && tPrimaryObj is TomlArray tPrimaryArray)
            {
                weaponMenuConfig.TPrimaryWeapons = tPrimaryArray.Cast<string>().ToList();
            }
            else if (weaponMenuTable.TryGetValue("t_primary", out var tPrimaryAltObj) && tPrimaryAltObj is TomlArray tPrimaryAltArray)
            {
                weaponMenuConfig.TPrimaryWeapons = tPrimaryAltArray.Cast<string>().ToList();
            }
            
            if (weaponMenuTable.TryGetValue("terrorist_secondary_weapons", out var tSecondaryObj) && tSecondaryObj is TomlArray tSecondaryArray)
            {
                weaponMenuConfig.TSecondaryWeapons = tSecondaryArray.Cast<string>().ToList();
            }
            else if (weaponMenuTable.TryGetValue("t_secondary", out var tSecondaryAltObj) && tSecondaryAltObj is TomlArray tSecondaryAltArray)
            {
                weaponMenuConfig.TSecondaryWeapons = tSecondaryAltArray.Cast<string>().ToList();
            }
            
            Console.WriteLine($"[Mesharsky - VIP] Weapon menu config: Enabled={weaponMenuConfig.Enabled}, MinRound={weaponMenuConfig.MinRound}");
            Console.WriteLine($"[Mesharsky - VIP] CT Primary Weapons: {weaponMenuConfig.CTPrimaryWeapons.Count}");
            Console.WriteLine($"[Mesharsky - VIP] CT Secondary Weapons: {weaponMenuConfig.CTSecondaryWeapons.Count}");
            Console.WriteLine($"[Mesharsky - VIP] T Primary Weapons: {weaponMenuConfig.TPrimaryWeapons.Count}");
            Console.WriteLine($"[Mesharsky - VIP] T Secondary Weapons: {weaponMenuConfig.TSecondaryWeapons.Count}");
        }
        
        return weaponMenuConfig;
    }


}
