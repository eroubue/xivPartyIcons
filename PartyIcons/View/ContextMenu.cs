using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Lumina.Excel.Sheets;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;

namespace PartyIcons.View;

public sealed class ContextMenu : IDisposable
{
    private readonly RoleTracker _roleTracker;
    private readonly Settings _configuration;
    private readonly PlayerStylesheet _stylesheet;

    private record CharacterInfo(string Name, uint World, RoleId? AssignedRole, RoleId? SuggestedRole);

    public ContextMenu(RoleTracker roleTracker, Settings configuration, PlayerStylesheet stylesheet)
    {
        _roleTracker = roleTracker;
        _configuration = configuration;
        _stylesheet = stylesheet;

        Service.ContextMenu.OnMenuOpened += OnMenuOpened;
    }

    public void Dispose()
    {
        Service.ContextMenu.OnMenuOpened -= OnMenuOpened;
    }

    private CharacterInfo? GetCharacterInfo(IMenuOpenedArgs args)
    {
        if (args is { MenuType: ContextMenuType.Default, Target: MenuTargetDefault menuTarget }) {
            if (menuTarget.TargetCharacter is { Name: { } tcName, HomeWorld.RowId: var tcWorld }) {
                return new CharacterInfo(
                    tcName,
                    tcWorld,
                    _roleTracker.TryGetAssignedRole(tcName, tcWorld, out var assigned) ? assigned : null,
                    _roleTracker.TryGetSuggestedRole(tcName, tcWorld, out var suggested) ? suggested : null
                );
            }

            if (menuTarget.TargetObject is IPlayerCharacter { Name.TextValue: { } pcName, HomeWorld.RowId: var pcWorld }) {
                return new CharacterInfo(
                    pcName,
                    pcWorld,
                    _roleTracker.TryGetAssignedRole(pcName, pcWorld, out var assigned) ? assigned : null,
                    _roleTracker.TryGetSuggestedRole(pcName, pcWorld, out var suggested) ? suggested : null
                );
            }
        }

        return null;
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (_configuration is { UseContextMenu: false, UseContextMenuStatic: false } || GetCharacterInfo(args) is not { } characterInfo) {
            return;
        }

        if (_configuration.UseContextMenuSubmenu) {
            var menuName = "设置职责";
            if (characterInfo.AssignedRole is { } assignedRole) {
                menuName = $"更改职责 ({_stylesheet.GetRoleName(assignedRole)})";
            }

            args.AddMenuItem(new MenuItem
            {
                Name = menuName,
                Prefix = SeIconChar.BoxedLetterP,
                PrefixColor = 37,
                Priority = 10,
                IsSubmenu = true,
                OnClicked = subArgs =>
                {
                    var items = CreateMenuItems(characterInfo);
                    if (items.Count == 0) {
                        items.Add(new MenuItem
                        {
                            Name = "无可用操作",
                            IsEnabled = false
                        });
                    }

                    subArgs.OpenSubmenu(items);
                }
            });
        }
        else {
            foreach (var item in CreateMenuItems(characterInfo)) {
                args.AddMenuItem(item);
            }
        }
    }

    private List<MenuItem> CreateMenuItems(CharacterInfo characterInfo)
    {
        var list = new List<MenuItem>();
        AddRoleSuggestion(list, characterInfo);
        AddRoleSwap(list, characterInfo);
        AddRoleAssignments(list, characterInfo);
        AddStatic(list, characterInfo);
        return list;
    }

    private void AddRoleSuggestion(List<MenuItem> list, CharacterInfo characterInfo)
    {
        if (characterInfo.SuggestedRole is { } role) {
            list.Add(new MenuItem
            {
                Name = $"分配 {_stylesheet.GetRoleName(role)} （建议）",
                Prefix = SeIconChar.ArrowRight,
                PrefixColor = 31,
                Priority = 100,
                OnClicked = _ => { AssignRole(characterInfo.Name, characterInfo.World, role); }
            });
        }
    }

    private void AddRoleSwap(List<MenuItem> list, CharacterInfo characterInfo)
    {
        if (characterInfo.AssignedRole is { } role) {
            var swappedRole = RoleIdUtils.Counterpart(role);
            list.Add(new MenuItem
            {
                Name = $"切换到 {_stylesheet.GetRoleName(swappedRole)}",
                Prefix = SeIconChar.ArrowRight,
                PrefixColor = 3,
                Priority = 101,
                OnClicked = _ => { AssignRole(characterInfo.Name, characterInfo.World, swappedRole); }
            });
        }
    }

    private void AddRoleAssignments(List<MenuItem> list, CharacterInfo characterInfo)
    {
        foreach (var role in Enum.GetValues<RoleId>()) {
            if (role == RoleId.Undefined) {
                continue;
            }

            var roleName = _stylesheet.GetRoleName(role);

            list.Add(new MenuItem
            {
                Name = $"分配 {roleName}",
                Prefix = (SeIconChar)PlayerStylesheet.BoxedCharacter(roleName[0]),
                PrefixColor = PlayerStylesheet.GetRoleColor(role),
                Priority = 101 + (int)role,
                OnClicked = _ => { AssignRole(characterInfo.Name, characterInfo.World, role); }
            });
        }
    }

    private void AddStatic(List<MenuItem> list, CharacterInfo characterInfo)
    {
        if (Plugin.Settings.UseContextMenuStatic && characterInfo.AssignedRole is { } role && GetWorldName(characterInfo.World) is { } worldName) {
            var staticName = $"{characterInfo.Name}@{worldName}";
            list.Add(new MenuItem
            {
                Name = $"保存为固定分配",
                Prefix = SeIconChar.LevelDe,
                PrefixColor = 21,
                Priority = 110,
                OnClicked = _ =>
                {
                    Plugin.Settings.StaticAssignments[staticName] = role;
                    Plugin.Settings.Save();

                    AssignRole(characterInfo.Name, characterInfo.World, role);
                }
            });
        }
    }

    private void AssignRole(string name, uint world, RoleId role)
    {
        _roleTracker.OccupyRole(name, world, role);
        _roleTracker.CalculateUnassignedPartyRoles();
    }

    private static string? GetWorldName(uint worldId)
    {
        return Service.DataManager.GameData.GetExcelSheet<World>()?.GetRowOrDefault(worldId)?.Name.ToString();
    }
}