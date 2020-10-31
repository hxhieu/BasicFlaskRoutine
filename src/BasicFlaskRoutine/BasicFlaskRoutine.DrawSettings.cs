using ImGuiNET;
using TreeRoutine.Menu;
using TreeRoutine.Routine.BasicFlaskRoutine.Flask;


namespace TreeRoutine.Routine.BasicFlaskRoutine
{
    public partial class BasicFlaskRoutine
    {
        public const string VERSION = "hxhieu-0.1.0";

        public override void DrawSettings()
        {
            //base.DrawSettings();

            ImGuiTreeNodeFlags collapsingHeaderFlags = ImGuiTreeNodeFlags.CollapsingHeader;

            ImGui.Text($"Version: {VERSION}");
            ImGui.Separator();

            if (ImGui.TreeNodeEx("Plugin Options", collapsingHeaderFlags))
            {
                Settings.EnableInHideout.Value = ImGuiExtension.Checkbox("Enable in Hideout", Settings.EnableInHideout);
                ImGui.Separator();
                Settings.TicksPerSecond.Value = ImGuiExtension.IntSlider("Ticks Per Second", Settings.TicksPerSecond); ImGuiExtension.ToolTipWithText("(?)", "Determines how many times the plugin checks flasks every second.\nLower for less resources, raise for faster response (but higher chance to chug potions).");
                ImGui.Separator();
                Settings.Debug.Value = ImGuiExtension.Checkbox("Debug Mode", Settings.Debug);
                ImGui.TreePop();
            }


            if (ImGui.TreeNodeEx("Flask Options", collapsingHeaderFlags))
            {
                if (ImGui.TreeNode("Individual Flask Settings"))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        FlaskSetting currentFlask = Settings.FlaskSettings[i];
                        if (ImGui.TreeNode("Flask " + (i + 1) + " Settings"))
                        {
                            currentFlask.Enabled.Value = ImGuiExtension.Checkbox("Enable", currentFlask.Enabled);
                            currentFlask.Hotkey.Value = ImGuiExtension.HotkeySelector("Hotkey", currentFlask.Hotkey);
                            currentFlask.ReservedUses.Value =
                                ImGuiExtension.IntSlider("Reserved Uses", currentFlask.ReservedUses);
                            ImGuiExtension.ToolTipWithText("(?)",
                                "The absolute number of uses reserved on a flask.\nSet to 1 to always have 1 use of the flask available for manual use.");
                            ImGui.TreePop();
                        }
                    }

                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Health and Mana"))
                {
                    Settings.AutoFlask.Value = ImGuiExtension.Checkbox("Enable", Settings.AutoFlask);

                    ImGuiExtension.SpacedTextHeader("Settings");
                    Settings.ForceBubblingAsInstantOnly.Value =
                        ImGuiExtension.Checkbox("Force Bubbling as Instant only", Settings.ForceBubblingAsInstantOnly);
                    ImGuiExtension.ToolTipWithText("(?)",
                        "When enabled, flasks with the Bubbling mod will only be used as an instant flask.");
                    Settings.ForcePanickedAsInstantOnly.Value =
                        ImGuiExtension.Checkbox("Force Panicked as Instant only", Settings.ForcePanickedAsInstantOnly);
                    ImGuiExtension.ToolTipWithText("(?)",
                        "When enabled, flasks with the Panicked mod will only be used as an instant flask. \nNote, Panicked will not be used until under 35%% with this enabled."); //
                    ImGuiExtension.SpacedTextHeader("Health Flask");
                    Settings.HPPotion.Value = ImGuiExtension.IntSlider("Min Life % Auto HP Flask", Settings.HPPotion);
                    Settings.InstantHPPotion.Value =
                        ImGuiExtension.IntSlider("Min Life % Auto Instant HP Flask", Settings.InstantHPPotion);
                    Settings.DisableLifeSecUse.Value =
                        ImGuiExtension.Checkbox("Disable Life/Hybrid Flask Offensive/Defensive Usage",
                            Settings.DisableLifeSecUse);

                    ImGuiExtension.SpacedTextHeader("Mana Flask");
                    ImGui.Spacing();
                    Settings.ManaPotion.Value =
                        ImGuiExtension.IntSlider("Min Mana % Auto Mana Flask", Settings.ManaPotion);
                    Settings.InstantManaPotion.Value = ImGuiExtension.IntSlider("Min Mana % Auto Instant MP Flask",
                        Settings.InstantManaPotion);
                    Settings.MinManaFlask.Value =
                        ImGuiExtension.IntSlider("Min Mana Auto Mana Flask", Settings.MinManaFlask);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Remove Ailments"))
                {
                    Settings.RemAilment.Value = ImGuiExtension.Checkbox("Enable", Settings.RemAilment);

                    ImGuiExtension.SpacedTextHeader("Ailments");
                    Settings.RemFrozen.Value = ImGuiExtension.Checkbox("Frozen", Settings.RemFrozen);
                    ImGui.SameLine();
                    Settings.RemBurning.Value = ImGuiExtension.Checkbox("Burning", Settings.RemBurning);
                    Settings.RemShocked.Value = ImGuiExtension.Checkbox("Shocked", Settings.RemShocked);
                    ImGui.SameLine();
                    Settings.RemCurse.Value = ImGuiExtension.Checkbox("Cursed", Settings.RemCurse);
                    Settings.RemPoison.Value = ImGuiExtension.Checkbox("Poison", Settings.RemPoison);
                    ImGui.SameLine();
                    Settings.RemBleed.Value = ImGuiExtension.Checkbox("Bleed", Settings.RemBleed);
                    Settings.CorruptCount.Value =
                        ImGuiExtension.IntSlider("Corrupting Blood Stacks", Settings.CorruptCount);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Speed Flasks"))
                {
                    Settings.SpeedFlaskEnable.Value = ImGuiExtension.Checkbox("Enable", Settings.SpeedFlaskEnable);

                    ImGuiExtension.SpacedTextHeader("Flasks");
                    Settings.QuicksilverFlaskEnable.Value =
                        ImGuiExtension.Checkbox("Quicksilver Flask", Settings.QuicksilverFlaskEnable);
                    Settings.SilverFlaskEnable.Value =
                        ImGuiExtension.Checkbox("Silver Flask", Settings.SilverFlaskEnable);

                    ImGuiExtension.SpacedTextHeader("Settings");
                    Settings.MinMsPlayerMoving.Value =
                        ImGuiExtension.IntSlider("Milliseconds Spent Moving", Settings.MinMsPlayerMoving);
                    ImGuiExtension.ToolTipWithText("(?)",
                        "Milliseconds spent moving before flask will be used.\n1000 milliseconds = 1 second");
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Defensive Flasks"))
                {
                    Settings.DefensiveFlaskEnable.Value =
                        ImGuiExtension.Checkbox("Enable", Settings.DefensiveFlaskEnable);
                    ImGui.Spacing();
                    ImGui.Separator();
                    Settings.HPPercentDefensive.Value =
                        ImGuiExtension.IntSlider("Min Life %", Settings.HPPercentDefensive);
                    Settings.ESPercentDefensive.Value =
                        ImGuiExtension.IntSlider("Min ES %", Settings.ESPercentDefensive);
                    Settings.OffensiveAsDefensiveEnable.Value =
                        ImGuiExtension.Checkbox("Use offensive flasks for defense",
                            Settings.OffensiveAsDefensiveEnable);
                    ImGui.Separator();

                    Settings.DefensiveMonsterCount.Value =
                        ImGuiExtension.IntSlider("Monster Count", Settings.DefensiveMonsterCount);
                    Settings.DefensiveMonsterDistance.Value =
                        ImGuiExtension.IntSlider("Monster Distance", Settings.DefensiveMonsterDistance);
                    Settings.DefensiveCountNormalMonsters.Value =
                        ImGuiExtension.Checkbox("Normal Monsters", Settings.DefensiveCountNormalMonsters);
                    Settings.DefensiveCountRareMonsters.Value =
                        ImGuiExtension.Checkbox("Rare Monsters", Settings.DefensiveCountRareMonsters);
                    Settings.DefensiveCountMagicMonsters.Value =
                        ImGuiExtension.Checkbox("Magic Monsters", Settings.DefensiveCountMagicMonsters);
                    Settings.DefensiveCountUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Unique Monsters", Settings.DefensiveCountUniqueMonsters);
                    Settings.DefensiveIgnoreFullHealthUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Ignore Full Health Unique Monsters", Settings.DefensiveIgnoreFullHealthUniqueMonsters);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Offensive Flasks"))
                {
                    Settings.OffensiveFlaskEnable.Value =
                        ImGuiExtension.Checkbox("Enable", Settings.OffensiveFlaskEnable);
                    ImGui.Spacing();
                    ImGui.Separator();
                    Settings.HPPercentOffensive.Value =
                        ImGuiExtension.IntSlider("Min Life %", Settings.HPPercentOffensive);
                    Settings.ESPercentOffensive.Value =
                        ImGuiExtension.IntSlider("Min ES %", Settings.ESPercentOffensive);
                    ImGui.Separator();
                    Settings.OffensiveMonsterCount.Value =
                        ImGuiExtension.IntSlider("Monster Count", Settings.OffensiveMonsterCount);
                    Settings.OffensiveMonsterDistance.Value =
                        ImGuiExtension.IntSlider("Monster Distance", Settings.OffensiveMonsterDistance);
                    Settings.OffensiveCountNormalMonsters.Value =
                        ImGuiExtension.Checkbox("Normal Monsters", Settings.OffensiveCountNormalMonsters);
                    Settings.OffensiveCountRareMonsters.Value =
                        ImGuiExtension.Checkbox("Rare Monsters", Settings.OffensiveCountRareMonsters);
                    Settings.OffensiveCountMagicMonsters.Value =
                        ImGuiExtension.Checkbox("Magic Monsters", Settings.OffensiveCountMagicMonsters);
                    Settings.OffensiveCountUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Unique Monsters", Settings.OffensiveCountUniqueMonsters);
                    Settings.OffensiveIgnoreFullHealthUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Ignore Full Health Unique Monsters", Settings.OffensiveIgnoreFullHealthUniqueMonsters);
                    ImGui.TreePop();
                }

                ImGui.TreePop();
            }
        }
    }
}
