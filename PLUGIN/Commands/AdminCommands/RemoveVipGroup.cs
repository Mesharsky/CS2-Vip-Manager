﻿using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    private void cmd_RemoveVipGroup(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || !CheckCommandAccess(player))
            return;
        
        ShowRemoveVipPlayerSelectionMenu(player);
    }

    private void ShowRemoveVipPlayerSelectionMenu(CCSPlayerController admin)
    {
        var onlinePlayers = Utilities.GetPlayers()
            .Where(p => p is { IsValid: true, IsBot: false })
            .ToList();
        
        if (onlinePlayers.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.player.none");
            return;
        }
        
        ShowPlayerSelectionMenu(admin, onlinePlayers, _localizer!.ForPlayer(admin, "admin.menu.title.selectplayer"), (selectedPlayer) => {
            var cachedPlayer = GetOrCreatePlayer(selectedPlayer.SteamID, selectedPlayer.PlayerName);
            
            if (cachedPlayer.Groups.Count == 0 || !cachedPlayer.Groups.Any(g => g.Active))
            {
                ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.player.nogroups", selectedPlayer.PlayerName);
                ShowRemoveVipPlayerSelectionMenu(admin);
                return;
            }
            
            ShowRemoveGroupMenu(admin, selectedPlayer);
        });
    }

    private void ShowRemoveGroupMenu(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        
        var manager = GetMenuManager();
        if (manager == null)
            return;
            
        var menu = manager.CreateMenu(_localizer!.ForPlayer(admin, "admin.menu.title.removegroup", targetPlayer.PlayerName), isSubMenu: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.player.info", targetPlayer.PlayerName, targetPlayer.SteamID), (_, _) => { });
        
        foreach (var group in cachedPlayer.Groups.Where(g => g.Active))
        {
            var expiryMsg = group.ExpiryTime == 0 
                ? _localizer!.ForPlayer(admin, "admin.group.expiry.permanent") 
                : _localizer!.ForPlayer(admin, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(group.ExpiryTime).ToLocalTime());
                
            menu.AddOption($"{group.GroupName} - {expiryMsg}", (p, _) => {
                ShowRemoveConfirmationMenu(p, targetPlayer, group.GroupName);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            ShowRemoveVipPlayerSelectionMenu(p);
        });
        
        manager.OpenSubMenu(admin, menu);
    }
    
    private void ShowRemoveConfirmationMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName)
    {
        var manager = GetMenuManager();
        if (manager == null)
            return;
            
        var menu = manager.CreateMenu(_localizer!.ForPlayer(admin, "admin.menu.title.confirmremove", groupName, targetPlayer.PlayerName), isSubMenu: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.confirm.areyousure"), (_, _) => { });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.confirm.yes"), (p, _) => {
            RemoveVipGroupFromPlayer(p, targetPlayer, groupName);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.confirm.no"), (p, _) => {
            ShowRemoveGroupMenu(p, targetPlayer);
        });
        
        manager.OpenSubMenu(admin, menu);
    }

    private void RemoveVipGroupFromPlayer(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName)
    {
        var service = ServiceManager.GetService(groupName);
        if (service == null)
        {
            ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.notfound", groupName);
            return;
        }
        
        var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        
        if (!cachedPlayer.HasGroup(groupName))
        {
            ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.playerdoesnothave", targetPlayer.PlayerName, groupName);
            return;
        }
        
        RemoveGroupFromPlayer(targetPlayer.SteamID, groupName);
        
        AdminManager.RemovePlayerPermissions(targetPlayer, service.Flag);
        
        ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.removed", groupName, targetPlayer.PlayerName);
        
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.removed", groupName);
        
        ShowRemoveVipPlayerSelectionMenu(admin);
    }
}