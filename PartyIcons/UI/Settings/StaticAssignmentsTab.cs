using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using PartyIcons.Entities;
using PartyIcons.UI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PartyIcons.UI.Settings;

public sealed class StaticAssignmentsTab
{
    private string _occupationNewName = "Character Name@World";
    private RoleId _occupationNewRole = RoleId.Undefined;

    public void Draw()
    {
        ImGui.Dummy(new Vector2(0, 2f));
        
        var easternNamingConvention = Plugin.Settings.EasternNamingConvention;

        if (ImGui.Checkbox("##easteannaming", ref easternNamingConvention))
        {
            Plugin.Settings.EasternNamingConvention = easternNamingConvention;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Eastern role naming convention");
        ImGuiComponents.HelpMarker("Use Japanese data center role naming convention (MT ST D1-D4 H1-2).");

        var displayRoleInPartyList = Plugin.Settings.DisplayRoleInPartyList;

        if (ImGui.Checkbox("##displayrolesinpartylist", ref displayRoleInPartyList))
        {
            Plugin.Settings.DisplayRoleInPartyList = displayRoleInPartyList;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("小队列表中用职能替换序号");
        ImGuiExt.ImGuiHelpTooltip(
            "仅当名牌设置为‘角色字母’或‘小工作图标、角色和名称’时才有效'.");

        var useContextMenu = Plugin.Settings.UseContextMenu;
        
        if (ImGui.Checkbox("##useContextMenu", ref useContextMenu))
        {
            Plugin.Settings.UseContextMenu = useContextMenu;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("添加上下文菜单命令来分配角色");
        ImGuiComponents.HelpMarker("添加快捷菜单命令，用于为玩家分配角色。适用时，还会添加用于切换角色和使用建议角色的命令。");

        var useContextMenuStatic = Plugin.Settings.UseContextMenuStatic;

        if (ImGui.Checkbox("##useContextMenuStatic", ref useContextMenuStatic))
        {
            Plugin.Settings.UseContextMenuStatic = useContextMenuStatic;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("添加上下文菜单命令以将角色保存为静态");

        var useContextMenuSubmenu = Plugin.Settings.UseContextMenuSubmenu;

        if (ImGui.Checkbox("##useContextMenuSubmenu", ref useContextMenuSubmenu))
        {
            Plugin.Settings.UseContextMenuSubmenu = useContextMenuSubmenu;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("将上下文菜单命令（如果启用）放置在专用子菜单中");

        var assignFromChat = Plugin.Settings.AssignFromChat;

        if (ImGui.Checkbox("##assignFromChat", ref assignFromChat))
        {
            Plugin.Settings.AssignFromChat = assignFromChat;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("允许队伍成员通过队伍聊天自行分配角色");
        ImGuiComponents.HelpMarker("允许队员为自己分配角色，例如在队伍聊天中说“h1”将赋予该玩家H1职能。");
        
        ImGui.Dummy(new Vector2(0, 2f));
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("静态职能");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));
        {
            ImGui.PushStyleColor(0, ImGuiColors.ParsedGrey);
            {
                ImGui.TextWrapped(
                    "名称应包含世界名称，并以@分隔。请注意，如果玩家的职业与分配的角色不匹配，该分配将被忽略！");
                ImGui.Dummy(new Vector2(0f, 25f));
            }
            ImGui.PopStyleColor();
        }
        
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y - 22f);
        foreach (var kv in new Dictionary<string, RoleId>(Plugin.Settings.StaticAssignments))
        {
            if (ImGui.Button("x##remove_occupation_" + kv.Key))
            {
                Plugin.Settings.StaticAssignments.Remove(kv.Key);
                Plugin.Settings.Save();

                continue;
            }

            ImGui.SameLine();
            ImGuiExt.SetComboWidth(Enum.GetValues<RoleId>().Select(x => Plugin.PlayerStylesheet.GetRoleName(x)));

            if (ImGui.BeginCombo("##role_combo_" + kv.Key,
                    Plugin.PlayerStylesheet.GetRoleName(Plugin.Settings.StaticAssignments[kv.Key])))
            {
                foreach (var roleId in Enum.GetValues<RoleId>())
                {
                    if (ImGui.Selectable(Plugin.PlayerStylesheet.GetRoleName(roleId) + "##role_combo_option_" + kv.Key + "_" +
                                         roleId))
                    {
                        Plugin.Settings.StaticAssignments[kv.Key] = roleId;
                        Plugin.Settings.Save();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.Text(kv.Key);
        }

        if (ImGui.Button("+##add_occupation"))
        {
            Plugin.Settings.StaticAssignments[_occupationNewName] = _occupationNewRole;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGuiExt.SetComboWidth(Enum.GetValues<RoleId>().Select(x => Plugin.PlayerStylesheet.GetRoleName(x)));

        if (ImGui.BeginCombo("##new_role_combo", Plugin.PlayerStylesheet.GetRoleName(_occupationNewRole)))
        {
            foreach (var roleId in Enum.GetValues<RoleId>())
            {
                if (ImGui.Selectable(Plugin.PlayerStylesheet.GetRoleName(roleId) + "##new_role_combo_option_" + "_" + roleId))
                {
                    _occupationNewRole = roleId;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.InputText("##new_role_name", ref _occupationNewName, 64);
        
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 22f);

    }
}