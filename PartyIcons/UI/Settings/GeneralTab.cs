using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using PartyIcons.UI.Utils;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Numerics;

namespace PartyIcons.UI.Settings;

public sealed class GeneralTab
{
    
    private readonly FlashingText _flashingText = new();

    public void Draw()
    {
        ImGui.Dummy(new Vector2(0, 2f));

        using (ImRaii.PushColor(ImGuiCol.CheckMark, 0xFF888888)) {
            var usePriorityIcons = true;
            ImGui.Checkbox("##usePriorityIcons", ref usePriorityIcons);
            ImGui.SameLine();
            ImGui.Text("优先状态图标");
            using (ImRaii.PushIndent())
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudOrange)) {
                ImGui.TextWrapped(
                    "注意：优先级状态图标现在可通过“外观”选项卡中的“交换样式”选项按铭牌类型进行配置。您还可以在“状态图标”选项卡中配置哪些图标足够重要，需要优先显示。");
            }
            ImGui.Dummy(new Vector2(0, 3));
        }

        var testingMode = Plugin.Settings.TestingMode;
        if (ImGui.Checkbox("##testingMode", ref testingMode)) {
            Plugin.Settings.TestingMode = testingMode;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        using (_flashingText.PushColor(Plugin.Settings.TestingMode)) {
            ImGui.Text("启用测试模式");
        }
        ImGuiComponents.HelpMarker("将设置应用于任何玩家,而不是只应用于队伍中的玩家.");

        var chatContentMessage = Plugin.Settings.ChatContentMessage;

        if (ImGui.Checkbox("##chatmessage", ref chatContentMessage)) {
            Plugin.Settings.ChatContentMessage = chatContentMessage;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("显示进入任务时的聊天信息");
        ImGuiComponents.HelpMarker("可用于在完全加载前确定任务类型");

        ImGuiExt.Spacer(10);
        if (ImGuiExt.ButtonEnabledWhen(ImGui.GetIO().KeyCtrl, "再次显示更新指南")) {
            UpgradeGuideTab.ForceRedisplay = true;
        }
        ImGuiExt.HoverTooltip("按住Crtl来允许点击");

        
    }
}

