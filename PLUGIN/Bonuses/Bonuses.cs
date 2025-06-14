using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private readonly Dictionary<string, int> _grenadeIndex = new()
    {
        ["weapon_flashbang"] = 14,
        ["weapon_smokegrenade"] = 15,
        ["weapon_decoy"] = 17,
        ["weapon_incgrenade"] = 16,
        ["weapon_molotov"] = 16,
        ["weapon_hegrenade"] = 13
    };

    private void PlayerSpawn_CombineBonuses(CCSPlayerController player, List<Service?> services)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || services.Count == 0)
            return;

        var bestHp = services.Max(s => s!.PlayerHp);
        var bestMaxHp = services.Max(s => s!.PlayerMaxHp);
        var hasArmor = services.Any(s => s!.PlayerVest);
        var hasHelmet = services.Any(s => s!.PlayerHelmet);
        var hasDefuser = services.Any(s => s!.PlayerDefuser);
        
        int earliestArmorRound = 1;
        if (hasArmor)
        {
            var armorServices = services.Where(s => s!.PlayerVest).ToList();
            if (armorServices.Count > 0)
                earliestArmorRound = armorServices.Min(s => s!.PlayerVestRound);
        }
        
        int earliestHelmetRound = 1;
        if (hasHelmet)
        {
            var helmetServices = services.Where(s => s!.PlayerHelmet).ToList();
            if (helmetServices.Count > 0)
                earliestHelmetRound = helmetServices.Min(s => s!.PlayerHelmetRound);
        }
        
        if (bestHp > 100)
        {
            playerPawn.Health = bestHp;
            playerPawn.MaxHealth = bestMaxHp;
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
        }

        var effectiveRound = GetEffectiveRoundNumber();
        var isPistol = IsPistolRound();

        // Prevent kevlar and helmet on all pistol rounds (first and second half)
        if (!isPistol) // Only give on non-pistol rounds
        {
            if (hasArmor && effectiveRound >= earliestArmorRound)
            {
                playerPawn.ArmorValue = 100;
                Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_ArmorValue");
            }

            if (hasHelmet && effectiveRound >= earliestHelmetRound)
            {
                if (playerPawn.ItemServices != null)
                {
                    new CCSPlayer_ItemServices(playerPawn.ItemServices.Handle).HasHelmet = true;
                    Utilities.SetStateChanged(playerPawn, "CCSPlayer_ItemServices", "m_bHasHelmet");
                }
            }
        }
        
        if (hasDefuser && player.TeamNum == 3 && !HasWeapon(player, "item_defuser"))
        {
            if (playerPawn.ItemServices != null)
            {
                new CCSPlayer_ItemServices(playerPawn.ItemServices.Handle).HasDefuser = true;
                Utilities.SetStateChanged(playerPawn, "CCSPlayer_ItemServices", "m_bHasDefuser");
            }
        }
        
        Bonus_AssignPlayerGrenades_Combined(player, playerPawn, services!);
        Bonus_AssignPlayerHealthshot_Combined(player, playerPawn, services!);
    }
    
    private void Bonus_AssignPlayerGrenades_Combined(CCSPlayerController player, CCSPlayerPawn playerPawn, List<Service> services)
    {
        var weaponService = playerPawn.WeaponServices;
        if (weaponService == null)
            return;

        var heAmount = services.Max(s => s.HeAmount);
        var flashAmount = services.Max(s => s.FlashAmount);
        var smokeAmount = services.Max(s => s.SmokeAmount);
        var decoyAmount = services.Max(s => s.DecoyAmount);
        var molotovAmount = services.Max(s => s.MolotovAmount);

        var grenadeConfig = new Dictionary<string, int>
        {
            { "weapon_hegrenade", heAmount },
            { "weapon_flashbang", flashAmount },
            { "weapon_smokegrenade", smokeAmount },
            { "weapon_decoy", decoyAmount }
        };

        switch (player.TeamNum)
        {
            // CT
            case 3:
                grenadeConfig["weapon_incgrenade"] = molotovAmount;
                break;
            // Terrorists
            case 2:
                grenadeConfig["weapon_molotov"] = molotovAmount;
                break;
        }

        foreach (var (grenadeName, maxGrenades) in grenadeConfig)
        {
            if (maxGrenades <= 0)
                continue;

            if (!_grenadeIndex.TryGetValue(grenadeName, out var ammoIndex)) continue;
            if (ammoIndex < 0 || ammoIndex >= weaponService.Ammo.Length)
                continue;

            int currentGrenades = weaponService.Ammo[ammoIndex];

            for (var i = currentGrenades; i < maxGrenades; i++)
            {
                player.GiveNamedItem(grenadeName);
            }
        }
    }
    
    private static void Bonus_AssignPlayerHealthshot_Combined(CCSPlayerController player, CCSPlayerPawn playerPawn, List<Service> services)
    {
        var healthshotAmount = services.Max(s => s.HealthshotAmount);
        if (healthshotAmount == 0)
            return;
        
        var weaponServices = playerPawn.WeaponServices;
        if (weaponServices == null) 
            return;

        int curHealthshotCount = weaponServices.Ammo[20];
        for (var i = 0; i < healthshotAmount - curHealthshotCount; i++)
        {
            player.GiveNamedItem("weapon_healthshot");
        }
    }
}
