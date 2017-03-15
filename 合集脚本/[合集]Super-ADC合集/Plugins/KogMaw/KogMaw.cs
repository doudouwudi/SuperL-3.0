#region Licensing
// ---------------------------------------------------------------------
// <copyright file="KogMaw.cs" company="EloBuddy">
// 
// Marksman Master
// Copyright (C) 2016 by gero
// All rights reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
// </copyright>
// <summary>
// 
// Email: geroelobuddy@gmail.com
// PayPal: geroelobuddy@gmail.com
// </summary>
// ---------------------------------------------------------------------
#endregion
using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Marksman_Master.Utils;
using SharpDX;

namespace Marksman_Master.Plugins.KogMaw
{
    internal class KogMaw : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Active W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu FarmingMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;
        private static readonly Text Text;

        protected static uint[] WRange { get; } = {0, 660, 690, 720, 750, 780};

        protected static uint[] RRange { get; } = {0, 1200, 1500, 1800};

        protected static int[] EMana { get; } = {0, 80, 90, 100, 110, 120};

        protected static BuffInstance GetKogMawWBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(b => b.IsActive && b.DisplayName.Equals("kogmawbioarcanebarrage", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasKogMawWBuff
            =>
                Player.Instance.Buffs.Any(b => b.IsActive && b.DisplayName.Equals("kogmawbioarcanebarrage", StringComparison.CurrentCultureIgnoreCase));

        protected static BuffInstance GetKogMawRBuff
            => Player.Instance.Buffs.FirstOrDefault(b => b.IsActive && b.Name.Equals("kogmawlivingartillerycost", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasKogMawRBuff
            =>
                Player.Instance.Buffs.Any(b => b.IsActive && b.Name.Equals( "kogmawlivingartillerycost", StringComparison.CurrentCultureIgnoreCase));

        protected static bool FarmMode
            => (Orbwalker.ActiveModesFlags &
                (Orbwalker.ActiveModes.Harass | Orbwalker.ActiveModes.LaneClear | Orbwalker.ActiveModes.LastHit |
                 Orbwalker.ActiveModes.JungleClear)) != 0;

        static KogMaw()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1200, SkillShotType.Linear, 250, 1650, 70)
            {
                AllowedCollisionCount = 0
            };
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 1500, SkillShotType.Linear, 250, 1350, 120)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Skillshot(SpellSlot.R, 1800, SkillShotType.Circular, 1100, int.MaxValue, 230)
            {
                AllowedCollisionCount = int.MaxValue
            };

            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("KogMawQ", new ColorBGRA(243, 109, 160, 255));
            ColorPicker[1] = new ColorPicker("KogMawW", new ColorBGRA(255, 210, 54, 255));
            ColorPicker[2] = new ColorPicker("KogMawE", new ColorBGRA(241, 188, 160, 255));
            ColorPicker[3] = new ColorPicker("KogMawR", new ColorBGRA(241, 188, 160, 255));

            Text = new Text(string.Empty,
                new SharpDX.Direct3D9.FontDescription
                {
                    FaceName = "Verdana",
                    Weight = SharpDX.Direct3D9.FontWeight.Regular,
                    Quality = SharpDX.Direct3D9.FontQuality.NonAntialiased,
                    OutputPrecision = SharpDX.Direct3D9.FontPrecision.String,
                    Height = 19,
                    MipLevels = 1
                });

            Orbwalker.OnUnkillableMinion += Orbwalker_OnUnkillableMinion;
        }

        private static void Orbwalker_OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (!Settings.Farm.UseQOnUnkillableMinion || !FarmMode || !Q.IsReady() || !(Player.Instance.Mana - 40 > 150))
                return;

            var predictedHealth = Prediction.Health.GetPrediction(target, (int) (Player.Instance.Distance(target) / Q.Speed * 1000) + Q.CastDelay);
            var damage = Player.Instance.GetSpellDamageCached(target, SpellSlot.Q);

            if((damage < predictedHealth) || (predictedHealth <= 0))
                return;

            var qPrediction = Q.GetPrediction(target);

            if (qPrediction.HitChancePercent >= 70)
            {
                Q.Cast(qPrediction.CastPosition);
            }
        }

        protected override void OnDraw()
        {
            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[2].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[3].Color, R.Range, Player.Instance);

            if (!Settings.Drawings.DrawInfos)
                return;

            if (HasKogMawWBuff)
            {
                var hpPosition = Player.Instance.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 18;
                var timeLeft = GetKogMawWBuff.EndTime - Game.Time;
                var endPos = timeLeft * 1000 / 95;

                var degree = Misc.GetNumberInRangeFromProcent(timeLeft * 1000d / 8000d * 100d, 3, 110);
                var color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();

                Text.Color = color;
                Text.TextValue = timeLeft.ToString("F1");
                Text.X = (int)(hpPosition.X + 45 + endPos);
                Text.Y = (int)hpPosition.Y + 12; // + text size
                Text.Draw();

                Drawing.DrawLine(hpPosition.X + 45 + endPos, hpPosition.Y, hpPosition.X + 45, hpPosition.Y, 1, color);
                Drawing.DrawLine(hpPosition.X + 45 + endPos, hpPosition.Y, hpPosition.X + 45 + endPos, hpPosition.Y+8, 1, color);
            }

            foreach (var source in EntityManager.Heroes.Enemies.Where(x=> x.IsHPBarRendered && x.IsInRange(Player.Instance, R.Range)))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30; // tracker friendly.
                var percentDamage = Math.Min(100, Damage.GetRDamage(source) / source.TotalHealthWithShields(true) * 100);

                Text.Color = new Misc.HsvColor(Misc.GetNumberInRangeFromProcent(percentDamage, 3, 110), 1, 1).ColorFromHsv();
                Text.TextValue = percentDamage.ToString("F1") + "%";
                Text.X = (int) (hpPosition.X - Text.Bounding.Width - 5);
                Text.Y = (int)source.HPBarPosition.Y;
                Text.Draw();
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (E.IsReady() && Settings.Combo.UseEVsGapclosers && (Player.Instance.ManaPercent > 30) &&
                (args.End.Distance(Player.Instance) < 350) && sender.IsValidTarget(E.Range))
            {
                if (args.Delay == 0)
                    E.Cast(sender);
                else Core.DelayAction(() => E.Cast(sender), args.Delay);
            }
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("大嘴 连招 设置");

            ComboMenu.AddLabel("Q 设置 :");
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("W 设置 :");
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("E 设置 :");
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.UseEVsGapclosers", new CheckBox("使用E反突进"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("R 设置 :");
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.UseROnlyToKs", new CheckBox("仅抢人头使用R"));
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.RHitChancePercent",
                new Slider("R 命中率 : {0}", 60));
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.RAllowedStacks",
                new Slider("允许使用的数量", 2, 0, 10));
            ComboMenu.Add("Plugins.KogMaw.ComboMenu.RMaxHealth", new Slider("使用R对敌人最低血量百分比", 60));
            ComboMenu.AddSeparator(2);
            ComboMenu.AddLabel(
                "使用R对目标最低血量百分比. 如果仅对抢人头使用R 该选项将被忽略.");
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("大嘴 骚扰 设置");

            HarassMenu.AddLabel("Q 设置 :");
            HarassMenu.Add("Plugins.KogMaw.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.KogMaw.HarassMenu.MinManaToUseQ",
                new Slider("最小蓝 百分比 ({0}%) 使用Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("W 设置 :");
            HarassMenu.Add("Plugins.KogMaw.HarassMenu.UseW", new CheckBox("Use W"));
            HarassMenu.Add("Plugins.KogMaw.HarassMenu.MinManaToUseW", new Slider("最小蓝 百分比 ({0}%) 使用W", 40, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("R 设置 :");
            HarassMenu.Add("Plugins.KogMaw.HarassMenu.UseR", new CheckBox("Use R"));
            HarassMenu.Add("Plugins.KogMaw.HarassMenu.RAllowedStacks", new Slider("允许使用的数量", 2, 0, 10));

            HarassMenu.AddLabel("小妹妹汉化 ！");
            foreach (var aiHeroClient in EntityManager.Heroes.Enemies)
            {
                HarassMenu.Add("Plugins.KogMaw.HarassMenu.UseR."+ aiHeroClient.Hero, new CheckBox(aiHeroClient.Hero.ToString()));
            }

            FarmingMenu = MenuManager.Menu.AddSubMenu("Farm");
            FarmingMenu.AddGroupLabel("大嘴 发育 设置");

            FarmingMenu.AddLabel("Q 设置  :");
            FarmingMenu.Add("Plugins.KogMaw.FarmingMenu.UseQOnUnkillableMinion", new CheckBox("使用Q对杀不死的小兵"));

            MenuManager.BuildAntiGapcloserMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("大嘴 线圈 设置");

            DrawingsMenu.AddLabel("基本设置 :");
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("只在技能准备好的时候画出线圈"));
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawInfos",
                new CheckBox("画出信息"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Q 设置 :");
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawQ", new CheckBox("Q线圈", false));
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("W 设置 :");
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawW", new CheckBox("W线圈"));
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawWColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("E 设置 :");
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawE", new CheckBox("E线圈", false));
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("R 设置 :");
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawR", new CheckBox("R线圈"));
            DrawingsMenu.Add("Plugins.KogMaw.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[3].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);
        }

        protected override void PermaActive()
        {
            R.Range = RRange[R.Level];
            W.Range = WRange[W.Level];

            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            Modes.Combo.Execute();
        }

        protected override void HarassMode()
        {
            Modes.Harass.Execute();
        }

        protected override void LaneClear()
        {
            Modes.LaneClear.Execute();
        }

        protected override void JungleClear()
        {
            Modes.JungleClear.Execute();
        }

        protected override void LastHit()
        {
            Modes.LastHit.Execute();
        }

        protected override void Flee()
        {
            Modes.Flee.Execute();
        }

        protected static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.UseE"];

                public static bool UseEVsGapclosers => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.UseEVsGapclosers"];

                public static bool UseR => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.UseR"];

                public static bool UseROnlyToKs => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.UseROnlyToKs"];

                public static int RHitChancePercent => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.RHitChancePercent", true];

                public static int RAllowedStacks => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.RAllowedStacks", true];

                public static int RMaxHealth => MenuManager.MenuValues["Plugins.KogMaw.ComboMenu.RMaxHealth", true];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.KogMaw.HarassMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.KogMaw.HarassMenu.UseW"];

                public static int MinManaToUseQ => MenuManager.MenuValues["Plugins.KogMaw.HarassMenu.MinManaToUseQ", true];

                public static int MinManaToUseW => MenuManager.MenuValues["Plugins.KogMaw.HarassMenu.MinManaToUseW", true];

                public static bool UseR => MenuManager.MenuValues["Plugins.KogMaw.HarassMenu.UseR"];

                public static int RAllowedStacks => MenuManager.MenuValues["Plugins.KogMaw.HarassMenu.RAllowedStacks", true];

                public static bool IsHarassEnabledFor(AIHeroClient unit) => MenuManager.MenuValues["Plugins.KogMaw.HarassMenu.UseR." + unit.Hero];
            }

            internal static class Farm
            {
                public static bool UseQOnUnkillableMinion => MenuManager.MenuValues["Plugins.KogMaw.FarmingMenu.UseQOnUnkillableMinion"];
            }
    
            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.KogMaw.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawInfos => MenuManager.MenuValues["Plugins.KogMaw.DrawingsMenu.DrawInfos"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.KogMaw.DrawingsMenu.DrawQ"];

                public static bool DrawW => MenuManager.MenuValues["Plugins.KogMaw.DrawingsMenu.DrawW"];

                public static bool DrawE => MenuManager.MenuValues["Plugins.KogMaw.DrawingsMenu.DrawE"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.KogMaw.DrawingsMenu.DrawR"];
            }
        }

        protected static class Damage
        {
            public static int[] QDamage { get; } = {0, 80, 130, 180, 230, 280};
            public static float QBonusApMod { get; } = 0.5f;
            public static int[] EDamage { get; } = {0, 60, 110, 160, 210, 260};
            public static float EBonusApMod { get; } = 0.7f;
            public static int[] RDamage { get; } = {0, 100, 140, 180};
            public static float RBonusAdMod { get; } = 0.65f;
            public static float RBonusApMod { get; } = 0.25f;


            public static float GetQDamage(Obj_AI_Base target)
            {
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, QDamage[Q.Level] + Player.Instance.FlatMagicDamageMod*QBonusApMod);
            }

            public static float GetEDamage(Obj_AI_Base target)
            {
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, EDamage[E.Level] + Player.Instance.FlatMagicDamageMod * EBonusApMod);
            }

            public static float GetRDamage(Obj_AI_Base target)
            {
                var damage = RDamage[R.Level] + Player.Instance.FlatPhysicalDamageMod*RBonusAdMod +
                                                Player.Instance.TotalMagicalDamage*RBonusApMod;

                if (target.HealthPercent > 40)
                    damage *= (float)Misc.GetNumberInRangeFromProcent((100 - target.HealthPercent)/60*100, 1, 1.5);
                else if (target.HealthPercent <= 40)
                    damage *= 2;
                
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, damage);
            }

            public static bool IsTargetKillableFromR(Obj_AI_Base target)
            {
                if (!(target is AIHeroClient))
                {
                    return target.TotalHealthWithShields() <= GetRDamage(target);
                }

                var enemy = (AIHeroClient) target;

                if (enemy.HasSpellShield() || enemy.HasUndyingBuffA())
                    return false;

                if (enemy.ChampionName != "Blitzcrank")
                    return enemy.TotalHealthWithShields(true) < GetRDamage(target);

                if (!enemy.HasBuff("BlitzcrankManaBarrierCD") && !enemy.HasBuff("ManaBarrier"))
                {
                    return enemy.TotalHealthWithShields(true) + enemy.Mana / 2 < GetRDamage(target);
                }

                return enemy.TotalHealthWithShields(true) < GetRDamage(target);
            }
        }
    }
}