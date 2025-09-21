using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using PartyIcons.Configuration;
using PartyIcons.UI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace PartyIcons.UI.Settings;

public sealed class NameplateTab
{
    private readonly Dictionary<NameplateMode, IDalamudTextureWrap> _nameplateExamples = new();

    public NameplateTab()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var examplesImageNames = new Dictionary<NameplateMode, string>
        {
            { NameplateMode.SmallJobIcon, "PartyIcons.Resources.1.png" },
            { NameplateMode.BigJobIcon, "PartyIcons.Resources.2.png" },
            { NameplateMode.BigJobIconAndPartySlot, "PartyIcons.Resources.3.png" },
            { NameplateMode.RoleLetters, "PartyIcons.Resources.4.png" }
        };

        foreach (var kv in examplesImageNames) {
            using var fileStream = assembly.GetManifestResourceStream(kv.Value);

            if (fileStream == null) {
                Service.Log.Error($"Failed to get resource stream for {kv.Value}");

                continue;
            }

            _nameplateExamples[kv.Key] = Service.TextureProvider.CreateFromImageAsync(fileStream).Result;
        }
    }

    public void Draw()
    {
        ImGui.Dummy(new Vector2(0, 2f));

        ImGui.Text("图标设置:");
        ImGui.SameLine();
        ImGuiExt.SetComboWidth(Enum.GetValues<IconSetId>().Select(UiNames.GetName));

        ImGuiExt.DrawIconSetCombo("##icon_set", false, () => Plugin.Settings.IconSetId, iconSetId =>
        {
            Plugin.Settings.IconSetId = iconSetId;
            Plugin.Settings.Save();
        });

        var iconSizeMode = Plugin.Settings.SizeMode;
        ImGui.Text("名牌大小:");
        ImGui.SameLine();
        ImGuiExt.SetComboWidth(Enum.GetValues<NameplateSizeMode>().Select(x => x.ToString()));

        using (var combo = ImRaii.Combo("##icon_size", iconSizeMode.ToString())) {
            if (combo) {
                foreach (var mode in Enum.GetValues<NameplateSizeMode>()) {
                    if (ImGui.Selectable(mode + "##icon_set_" + mode)) {
                        Plugin.Settings.SizeMode = mode;
                        Plugin.Settings.Save();
                    }
                }
            }
        }

        ImGuiComponents.HelpMarker("影响除游戏默认之外的所有预设。");

        if (Plugin.Settings.SizeMode == NameplateSizeMode.自定义) {
            var scale = Plugin.Settings.SizeModeCustom;
            if (ImGui.SliderFloat("自定义大小", ref scale, 0.3f, 3f)) {
                Plugin.Settings.SizeModeCustom = Math.Clamp(scale, 0.1f, 10f);
                Plugin.Settings.Save();
            }

            ImGuiComponents.HelpMarker("按住 Control 键并单击滑块以输入精确值");
        }

        var hideLocalNameplate = Plugin.Settings.HideLocalPlayerNameplate;
        if (ImGui.Checkbox("##hidelocal", ref hideLocalNameplate)) {
            Plugin.Settings.HideLocalPlayerNameplate = hideLocalNameplate;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("隐藏自己的名牌");
        ImGuiComponents.HelpMarker(
            "您可以打开自己的名牌，也可以将此\n 设置设置为仅使用名牌显示自己的团队位置。\n 如果您不想通过此设置显示您的位置，您只需在角色设置中禁用\n 您的名牌即可。");
        ImGuiExt.Spacer(6);

        ImGuiExt.SectionHeader("主世界");
        using (ImRaii.PushIndent(15f)) {
            NameplateModeSection("##np_overworld", () => Plugin.Settings.DisplaySelectors.DisplayOverworld,
                sel => Plugin.Settings.DisplaySelectors.DisplayOverworld = sel,
                "Party:");

            NameplateModeSection("##np_others", () => Plugin.Settings.DisplaySelectors.DisplayOthers,
                sel => Plugin.Settings.DisplaySelectors.DisplayOthers = sel,
                "Others:");
        }

        ImGuiExt.SectionHeader("副本");
        using (ImRaii.PushIndent(15f)) {
            NameplateModeSection("##np_dungeon", () => Plugin.Settings.DisplaySelectors.DisplayDungeon,
                (sel) => Plugin.Settings.DisplaySelectors.DisplayDungeon = sel,
                "迷宫:");

            NameplateModeSection("##np_raid", () => Plugin.Settings.DisplaySelectors.DisplayRaid,
                sel => Plugin.Settings.DisplaySelectors.DisplayRaid = sel,
                "讨伐:");

            NameplateModeSection("##np_alliance", () => Plugin.Settings.DisplaySelectors.DisplayAllianceRaid,
                sel => Plugin.Settings.DisplaySelectors.DisplayAllianceRaid = sel,
                "团本:");

            NameplateModeSection("##np_chaotic", () => Plugin.Settings.DisplaySelectors.DisplayChaoticRaid,
               sel => Plugin.Settings.DisplaySelectors.DisplayChaoticRaid = sel,
               "暗云:");
        }

        ImGuiExt.SectionHeader("特殊场景探索选项");

        using (ImRaii.PushIndent(15f)) {
            ImGui.TextDisabled("优雷卡,博兹雅等");

            NameplateModeSection("##np_field_party", () => Plugin.Settings.DisplaySelectors.DisplayFieldOperationParty,
                sel => Plugin.Settings.DisplaySelectors.DisplayFieldOperationParty = sel, "小队:");

            NameplateModeSection("##np_field_others",
                () => Plugin.Settings.DisplaySelectors.DisplayFieldOperationOthers,
                sel => Plugin.Settings.DisplaySelectors.DisplayFieldOperationOthers = sel, "其他:");
        }

        ImGuiExt.SectionHeader("PvP");

        using (ImRaii.PushIndent(15f)) {
            ImGui.TextDisabled("此插件在 PvP 比赛期间被禁用.");
        }

        ImGuiExt.Spacer(15);

        if (ImGui.CollapsingHeader("举例")) {
            foreach (var kv in _nameplateExamples) {
                CollapsibleExampleImage(kv.Key, kv.Value);
            }
        }
    }

    private static void CollapsibleExampleImage(NameplateMode mode, IDalamudTextureWrap tex)
    {
        if (ImGui.CollapsingHeader(UiNames.GetName(mode))) {
            ImGui.Image(tex.Handle, new Vector2(tex.Width, tex.Height));
        }
    }

    private static void NameplateModeSection(string label, Func<DisplaySelector> getter, Action<DisplaySelector> setter,
        string title = "名牌: ")
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 3f);
        ImGui.Text(title);
        ImGui.SameLine(100f);
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y - 3f);
        ImGuiExt.SetComboWidth(Plugin.Settings.DisplayConfigs.Selectors.Select(UiNames.GetName));

        using var combo = ImRaii.Combo(label, UiNames.GetName(getter()));
        if (!combo) return;

        foreach (var selector in Plugin.Settings.DisplayConfigs.Selectors) {
            using var col = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen,
                selector.Preset == DisplayPreset.Custom);
            if (ImGui.Selectable(UiNames.GetName(selector), selector == getter())) {
                setter(selector);
                Plugin.Settings.Save();
            }
        }
    }
}