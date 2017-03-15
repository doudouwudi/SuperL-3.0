#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Corki.cs" company="EloBuddy">
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
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;
using Marksman_Master.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using Marksman_Master.Cache.Modules;

namespace Marksman_Master.Plugins.Corki
{
    internal class Corki : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Active E { get; }
        protected static Spell.Skillshot R { get; }

        protected static int[] QMana { get; } = {0, 60, 70, 80, 90, 100};
        protected static int WMana { get; } = 100;
        protected static int EMana { get; } = 50;
        protected static int RMana { get; } = 20;

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu JungleClearMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }

        private static bool _changingRangeScan;
        
        private static readonly ColorPicker[] ColorPicker;

        protected static BuffInstance GetRBigMissileBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.Equals("corkimissilebarragecounterbig", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasBigRMissile
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.Equals("corkimissilebarragecounterbig", StringComparison.CurrentCultureIgnoreCase));

        protected static BuffInstance GetRNormalMissileBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.Equals("corkimissilebarragecounternormal", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasNormalRMissile
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.Equals("corkimissilebarragecounternormal", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasPackagesBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.Equals("corkiloaded", StringComparison.CurrentCultureIgnoreCase));

        protected static BuffInstance GetPackagesBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.Equals("corkiloaded"));
        
        protected static BuffInstance GetSheenBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.Equals("sheen", StringComparison.CurrentCultureIgnoreCase));

        protected static Cache.Cache Cache => StaticCacheProvider.Cache;

        protected static bool IsPreAttack { get; private set; }

        protected static float R_ETA(Vector3 position) => Player.Instance.DistanceCached(position) / R.Speed * 1000 + R.CastDelay;
        protected static float R_ETA(Obj_AI_Base unit) => Player.Instance.DistanceCached(unit) / R.Speed * 1000 + R.CastDelay;
        
        static Corki()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 825, SkillShotType.Circular, 250, 1000, 250)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Skillshot(SpellSlot.W, 600, SkillShotType.Linear, 250, 650, 120)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Active(SpellSlot.E, 1000);
            R = new Spell.Skillshot(SpellSlot.R, 1300, SkillShotType.Linear, 175, 2000, 40)
            {
                AllowedCollisionCount = 0
            };

            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("CorkiQ", new ColorBGRA(243, 109, 160, 255));
            ColorPicker[1] = new ColorPicker("CorkiW", new ColorBGRA(255, 210, 54, 255));
            ColorPicker[2] = new ColorPicker("CorkiR", new ColorBGRA(1, 109, 160, 255));
            ColorPicker[3] = new ColorPicker("CorkiHpBar", new ColorBGRA(255, 134, 0, 255));
            
            DamageIndicator.Initalize(ColorPicker[3].Color, 1300);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[3].OnColorChange += (sender, args) =>
            {
                DamageIndicator.Color = args.Color;
            };

            ChampionTracker.Initialize(ChampionTrackerFlags.PostBasicAttackTracker);
            ChampionTracker.OnPostBasicAttack += (sender, args) => IsPreAttack = false;
            Orbwalker.OnPreAttack += (target, args) => IsPreAttack = true;

            Obj_AI_Base.OnBuffGain += (sender, args) =>
            {
                if(sender.IsMe)
                    R.Range = HasBigRMissile ? (uint)1500 : 1300;
            };
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawDamageIndicator)
                return 0;

            var enemy = unit as AIHeroClient;
            return enemy != null ? Damage.GetComboDamage(enemy) : 0f;
        }

        protected override void OnDraw()
        {
            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawW && !HasPackagesBuff &&
                (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);

            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Corki.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("库奇 连招 设置");

            ComboMenu.AddLabel("Q设置 :");
            ComboMenu.Add("Plugins.Corki.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("E设置 :");
            ComboMenu.Add("Plugins.Corki.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("W设置 :");
            ComboMenu.Add("Plugins.Corki.ComboMenu.UseW", new CheckBox("Use W", false));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("R设置 :");
            ComboMenu.Add("Plugins.Corki.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Corki.ComboMenu.MinStacksForR", new Slider("最小成数使用R", 1, 1, 7));
            ComboMenu.AddSeparator(1);
            ComboMenu.Add("Plugins.Corki.ComboMenu.RAllowCollision", new CheckBox("允许R的敌人", false));
            ComboMenu.AddLabel("允许R的敌人 适用于敌人的小兵.");

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("库奇 骚扰 设置");

            HarassMenu.AddLabel("Q设置 :");
            HarassMenu.Add("Plugins.Corki.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Corki.HarassMenu.MinManaToUseQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 50, 1));
            HarassMenu.AddSeparator(5);
            
            HarassMenu.AddLabel("E设置 :");
            HarassMenu.Add("Plugins.Corki.HarassMenu.UseE", new CheckBox("Use E"));
            HarassMenu.Add("Plugins.Corki.HarassMenu.MinManaToUseE", new Slider("最小蓝 百分比 ({0}%) 使用E", 50, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("R设置 :");
            HarassMenu.Add("Plugins.Corki.HarassMenu.UseR", new CheckBox("Use R"));
            HarassMenu.Add("Plugins.Corki.HarassMenu.MinManaToUseR", new Slider("最小蓝 百分比 ({0}%) 使用R", 50, 1));
            HarassMenu.Add("Plugins.Corki.HarassMenu.MinStacksToUseR", new Slider("Minimum stacks to use R", 3, 1, 7));
            HarassMenu.AddSeparator(1);
            HarassMenu.Add("Plugins.Corki.HarassMenu.RAllowCollision", new CheckBox("允许R的敌人"));
            HarassMenu.AddLabel("允许R的敌人 适用于敌人的小兵.");

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Lane clear");
            LaneClearMenu.AddGroupLabel("库奇 清线 设置");

            LaneClearMenu.AddLabel("基本设置 :");
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.EnableLCIfNoEn", new CheckBox("只有附近没有敌人才能启用清线"));
            var scanRange = LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.ScanRange", new Slider("扫描敌人范围", 1500, 300, 2500));
            scanRange.OnValueChange += (a, b) =>
            {
                _changingRangeScan = true;
                Core.DelayAction(() =>
                {
                    if (!scanRange.IsLeftMouseDown && !scanRange.IsMouseInside)
                    {
                        _changingRangeScan = false;
                    }
                }, 2000);
            };
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.AllowedEnemies", new Slider("敌人数量", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Phosphorus Bomb (Q) settings :");
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.UseQ", new CheckBox("Use Q"));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinMinionsKilledToUseQ", new Slider("最少杀死使用Q", 2, 1, 6));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinManaToUseQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Gatling Gun (E) settings :");
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.UseE", new CheckBox("Use E", false));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinManaToUseE", new Slider("最小蓝 百分比 ({0}%) 使用E", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Missile Barrage (R) settings :");
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.UseR", new CheckBox("Use R"));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinManaToUseR", new Slider("最小蓝 百分比 ({0}%) 使用R", 50, 1));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinStacksToUseR", new Slider("最小成数使用R", 6, 1, 7));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinMinionsHitToUseR", new Slider("最少R到小兵", 3, 1, 4));

            JungleClearMenu = MenuManager.Menu.AddSubMenu("Jungle clear");
            JungleClearMenu.AddGroupLabel("库奇 打野 设置");

            JungleClearMenu.AddLabel("Q设置 :");
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.UseQ", new CheckBox("Use Q"));
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.MinManaToUseQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 50, 1));
            JungleClearMenu.AddSeparator(5);

            JungleClearMenu.AddLabel("Gatling Gun (E) settings :");
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.UseE", new CheckBox("Use E", false));
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.MinManaToUseE", new Slider("最小蓝 百分比 ({0}%) 使用E", 50, 1));
            JungleClearMenu.AddSeparator(5);

            JungleClearMenu.AddLabel("Missile Barrage (R) settings :");
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.UseR", new CheckBox("Use R"));
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.MinManaToUseR", new Slider("最小蓝 百分比 ({0}%) 使用R", 50, 1));
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.MinStacksToUseR", new Slider("最小成数使用R", 5, 1, 7));
            JungleClearMenu.AddSeparator(1);
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.RAllowCollision", new CheckBox("允许R的敌人"));
            JungleClearMenu.AddLabel("允许R的敌人也适用于敌人 小兵 野怪.");

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("库奇 其他 设置");
            MiscMenu.AddLabel("自动 骚扰 设置: ");
            MiscMenu.Add("Plugins.Corki.MiscMenu.AutoHarassEnabled",
                new KeyBind("自动骚扰热键", true, KeyBind.BindTypes.PressToggle, 'T'));
            MiscMenu.Add("Plugins.Corki.MiscMenu.UseBigBomb", new CheckBox("适用大炸弹", false));
            MiscMenu.Add("Plugins.Corki.MiscMenu.MinStacksToUseR", new Slider("最小成数使用R", 3, 1, 7));
            MiscMenu.AddSeparator(5);
            MiscMenu.AddLabel("自动骚扰开启 : ");

            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                MiscMenu.Add("Plugins.Corki.MiscMenu.AutoHarassEnabled."+enemy.Hero, new CheckBox(enemy.Hero.ToString()));
            }

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Corki addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Phosphorus Bomb (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Valkyrie (W) drawing settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawW", new CheckBox("Draw W range", false));
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawWColor", new CheckBox("Change color", false)).OnValueChange += (a, b) => 
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Missile Barrage (R) drawing settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Damage indicator drawing settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawDamageIndicator",
                new CheckBox("Draw damage indicator on enemy HP bars"));
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawDamageIndicatorColor",
                new CheckBox("Change color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[3].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
        }
        
        public static List<T> GetCollisionObjects<T>(Obj_AI_Base unit) where T : Obj_AI_Base
        {
            try
            {
                var minions = StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                        obj => obj.IsValidTargetCached() && Prediction.Position.PredictUnitPosition(obj, (int)R_ETA(obj)).IsInRangeCached(unit, HasBigRMissile ? 280 : 130)).ToList();

                var enemies = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        obj => obj.IsValidTargetCached() && Prediction.Position.PredictUnitPosition(obj, (int)R_ETA(obj)).IsInRangeCached(unit, HasBigRMissile ? 280 : 130)).ToList();

                if (typeof(T) == typeof(Obj_AI_Base))
                {
                    return (List<T>)Convert.ChangeType(minions.Cast<Obj_AI_Base>().Concat(enemies).ToList(), typeof(List<T>));
                }
                if (typeof (T) == typeof (AIHeroClient))
                {
                    return (List<T>) Convert.ChangeType(enemies, typeof (List<T>));
                }
                if (typeof (T) == typeof (Obj_AI_Minion))
                {
                    return (List<T>) Convert.ChangeType(minions, typeof (List<T>));
                }
                Logger.Error("Error at Corki.cs => GetCollisionObjects => Cannot cast to " + typeof(T));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        protected override void PermaActive()
        {
            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            if (!Player.Instance.HasSheenBuff() && !IsPreAttack)
            {
                Modes.Combo.Execute();
            }
        }

        protected override void HarassMode()
        {
            if (!Player.Instance.HasSheenBuff() && !IsPreAttack)
            {
                Modes.Harass.Execute();
            }
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Corki.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Corki.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Corki.ComboMenu.UseE"];

                public static bool UseR => MenuManager.MenuValues["Plugins.Corki.ComboMenu.UseR"];

                public static bool RAllowCollision => MenuManager.MenuValues["Plugins.Corki.ComboMenu.RAllowCollision"];

                public static int MinStacksForR => MenuManager.MenuValues["Plugins.Corki.ComboMenu.MinStacksForR", true];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Corki.HarassMenu.UseQ"];

                public static int MinManaToUseQ => MenuManager.MenuValues["Plugins.Corki.HarassMenu.MinManaToUseQ", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Corki.HarassMenu.UseE"];

                public static int MinManaToUseE => MenuManager.MenuValues["Plugins.Corki.HarassMenu.MinManaToUseE", true];

                public static bool UseR => MenuManager.MenuValues["Plugins.Corki.HarassMenu.UseR"];

                public static bool RAllowCollision => MenuManager.MenuValues["Plugins.Corki.HarassMenu.RAllowCollision"];

                public static int MinManaToUseR => MenuManager.MenuValues["Plugins.Corki.HarassMenu.MinManaToUseR", true];

                public static int MinStacksToUseR => MenuManager.MenuValues["Plugins.Corki.HarassMenu.MinStacksToUseR", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQ => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.UseQ"];

                public static int MinMinionsKilledToUseQ => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.MinMinionsKilledToUseQ", true];

                public static int MinManaToUseQ => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.MinManaToUseQ", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.UseE"];

                public static int MinManaToUseE => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.MinManaToUseE", true];

                public static bool UseR => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.UseR"];

                public static int MinManaToUseR => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.MinManaToUseR", true];

                public static int MinStacksToUseR => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.MinStacksToUseR", true];

                public static int MinMinionsHitToUseR => MenuManager.MenuValues["Plugins.Corki.LaneClearMenu.MinMinionsHitToUseR", true]; 
            }

            internal static class JungleClear
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Corki.JungleClearMenu.UseQ"];

                public static int MinManaToUseQ => MenuManager.MenuValues["Plugins.Corki.JungleClearMenu.MinManaToUseQ", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Corki.JungleClearMenu.UseE"];

                public static int MinManaToUseE => MenuManager.MenuValues["Plugins.Corki.JungleClearMenu.MinManaToUseE", true];

                public static bool UseR => MenuManager.MenuValues["Plugins.Corki.JungleClearMenu.UseR"];

                public static bool RAllowCollision => MenuManager.MenuValues["Plugins.Corki.JungleClearMenu.RAllowCollision"];

                public static int MinManaToUseR => MenuManager.MenuValues["Plugins.Corki.JungleClearMenu.MinManaToUseR", true];

                public static int MinStacksToUseR => MenuManager.MenuValues["Plugins.Corki.JungleClearMenu.MinStacksToUseR", true];
            }

            internal static class Misc
            {
                public static bool AutoHarassEnabled => MenuManager.MenuValues["Plugins.Corki.MiscMenu.AutoHarassEnabled"];

                public static bool UseBigBomb => MenuManager.MenuValues["Plugins.Corki.MiscMenu.UseBigBomb"];

                public static int MinStacksToUseR => MenuManager.MenuValues["Plugins.Corki.MiscMenu.MinStacksToUseR", true];

                public static bool IsAutoHarassEnabledFor(AIHeroClient champion) => MenuManager.MenuValues["Plugins.Corki.MiscMenu.AutoHarassEnabled." + champion.Hero];

                public static bool IsAutoHarassEnabledFor(Champion hero) => MenuManager.MenuValues["Plugins.Corki.MiscMenu.AutoHarassEnabled." + hero];

                public static bool IsAutoHarassEnabledFor(string championName) => MenuManager.MenuValues["Plugins.Corki.MiscMenu.AutoHarassEnabled." + championName];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Corki.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.Corki.DrawingsMenu.DrawQ"];

                public static bool DrawW => MenuManager.MenuValues["Plugins.Corki.DrawingsMenu.DrawW"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.Corki.DrawingsMenu.DrawR"];

                public static bool DrawDamageIndicator => MenuManager.MenuValues["Plugins.Corki.DrawingsMenu.DrawDamageIndicator"];
            }
        }

        protected static class Damage
        {
            private static float[] QDamage { get; } = {0, 70, 115, 160, 205, 250};
            private static float QDamageBounsAdMod { get; } = 0.5f;
            private static float QDamageTotalApMod { get; } = 0.5f;
            private static float[] EDamage { get; } = {0, 80, 140, 200, 260, 320};
            private static float EDamageBounsAdMod { get; } = 1.6f;
            private static float[] RDamageNormal { get; } = {0, 100, 130, 160};
            private static float[] RDamageNormalTotalAdMod { get; } = {0, 0.2f, 0.5f, 0.8f};
            private static float RDamageNormalTotalApMod { get; } = 0.3f;
            private static float[] RDamageBig { get; } = { 0, 150, 195, 240 };
            private static float[] RDamageBigTotalAdMod { get; } = { 0, 0.3f, 0.75f, 1.2f };
            private static float RDamageBigTotalApMod { get; } = 0.45f;

            private static CustomCache<Tuple<int, uint, uint>, float> CachedComboDamage => Cache.Resolve<CustomCache<Tuple<int, uint, uint>, float>>(1000);
            private static CustomCache<int, float> CachedQDamage => Cache.Resolve<CustomCache<int, float>>(1000);
            private static CustomCache<KeyValuePair<int, float>, float> CachedEDamage => Cache.Resolve<CustomCache<KeyValuePair<int, float>, float>>(1000);
            private static CustomCache<int, float> CachedRDamage => Cache.Resolve<CustomCache<int, float>>(1000);

            public static float GetComboDamage(AIHeroClient enemy, uint autos = 1, uint bombs = 1)
            {
                if (MenuManager.IsCacheEnabled &&
                    CachedComboDamage.Exist(new Tuple<int, uint, uint>(enemy.NetworkId, autos, bombs)))
                {
                    return CachedComboDamage.Get(new Tuple<int, uint, uint>(enemy.NetworkId, autos, bombs));
                }

                float damage = 0;

                if (Q.IsReady())
                    damage += GetSpellDamage(enemy, SpellSlot.Q);

                if ((Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing] != null) && Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Blade_of_the_Ruined_King);

                if ((Activator.Activator.Items[ItemsEnum.Cutlass] != null) && Activator.Activator.Items[ItemsEnum.Cutlass].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Bilgewater_Cutlass);

                if ((Activator.Activator.Items[ItemsEnum.Gunblade] != null) && Activator.Activator.Items[ItemsEnum.Gunblade].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Hextech_Gunblade);

                if (E.IsReady())
                    damage += GetSpellDamage(enemy, SpellSlot.R, 2);

                if (R.IsReady())
                    damage += GetSpellDamage(enemy, SpellSlot.R) * bombs;

                damage += Player.Instance.GetAutoAttackDamageCached(enemy, true) * autos;

                if (MenuManager.IsCacheEnabled)
                {
                    CachedComboDamage.Add(new Tuple<int, uint, uint>(enemy.NetworkId, autos, bombs), damage);
                }
                return damage;
            }

            public static float GetSpellDamage(Obj_AI_Base unit, SpellSlot slot, float time = 4)
            {
                if (unit == null)
                    return 0f;

                switch (slot)
                {
                    case SpellSlot.Q:
                    {
                        return GetQDamage(unit);
                    }
                    case SpellSlot.E:
                    {
                        return GetEDamage(unit, time);
                    }
                    case SpellSlot.R:
                    {
                        return GetRDamage(unit);
                    }
                    default:
                        return 0f;
                }
            }

            private static float GetQDamage(Obj_AI_Base unit)
            {
                if (unit == null)
                    return 0f;

                if (MenuManager.IsCacheEnabled && CachedQDamage.Exist(unit.NetworkId))
                {
                    return CachedQDamage.Get(unit.NetworkId);
                }
                
                float damage;

                if (unit.GetType() != typeof(AIHeroClient))
                {
                    damage = QDamage[Q.Level] + Player.Instance.FlatPhysicalDamageMod*QDamageBounsAdMod +
                             Player.Instance.FlatMagicDamageMod*QDamageTotalApMod;

                    damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);
                    
                    if (MenuManager.IsCacheEnabled)
                    {
                        CachedQDamage.Add(unit.NetworkId, damage);
                    }
                    return damage;
                }

                var client = unit as AIHeroClient;

                if ((client == null) || client.HasSpellShield() || client.HasUndyingBuffA())
                    return 0f;

                damage = QDamage[Q.Level] + Player.Instance.FlatPhysicalDamageMod * QDamageBounsAdMod +
                             Player.Instance.FlatMagicDamageMod*QDamageTotalApMod;

                damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);

                if (MenuManager.IsCacheEnabled)
                {
                    CachedQDamage.Add(unit.NetworkId, damage);
                }
                return damage;
            }

            private static float GetEDamage(Obj_AI_Base unit, float time = 4)
            {
                if ((unit == null) || (time < 0.25f) || (time > 4))
                    return 0f;

                if (MenuManager.IsCacheEnabled && CachedEDamage.Exist(new KeyValuePair<int, float>(unit.NetworkId, time)))
                {
                    return CachedEDamage.Get(new KeyValuePair<int, float>(unit.NetworkId, time));
                }

                float damage;

                float actualTIme = 0;

                if (!(Math.Abs(time % 0.25f) <= 0))
                {
                    actualTIme = time - time % 0.25f;
                }

                if (unit.GetType() != typeof(AIHeroClient))
                {
                    damage = EDamage[Q.Level] / 16 + Player.Instance.FlatPhysicalDamageMod * EDamageBounsAdMod / 16;

                    damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Mixed, damage * (16 / (4 / actualTIme)));
                    
                    if (MenuManager.IsCacheEnabled)
                    {
                        CachedEDamage.Add(new KeyValuePair<int, float>(unit.NetworkId, time), damage);
                    }
                    return damage;
                }

                var client = unit as AIHeroClient;

                if ((client == null) || client.HasUndyingBuffA())
                    return 0f;
                
                damage = EDamage[Q.Level] / 16 + Player.Instance.FlatPhysicalDamageMod * EDamageBounsAdMod / 16;
                damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Mixed, damage * (16 / (4 / actualTIme)));

                if (MenuManager.IsCacheEnabled)
                {
                    CachedEDamage.Add(new KeyValuePair<int, float>(unit.NetworkId, time), damage);
                }
                return damage;
            }

            private static float GetRDamage(Obj_AI_Base unit)
            {
                if (unit == null)
                    return 0f;

                if (MenuManager.IsCacheEnabled && CachedRDamage.Exist(unit.NetworkId))
                {
                    return CachedRDamage.Get(unit.NetworkId);
                }

                float damage;

                if (unit.GetType() != typeof(AIHeroClient))
                {
                    if (HasBigRMissile)
                    {
                        damage = RDamageBig[R.Level] + Player.Instance.TotalAttackDamage * RDamageBigTotalAdMod[R.Level] +
                                 Player.Instance.FlatMagicDamageMod * RDamageBigTotalApMod;
                    }
                    else
                    {
                        damage = RDamageNormal[R.Level] + Player.Instance.TotalAttackDamage * RDamageNormalTotalAdMod[R.Level] +
                                 Player.Instance.FlatMagicDamageMod * RDamageNormalTotalApMod;
                    }

                    damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);

                    if (MenuManager.IsCacheEnabled)
                    {
                        CachedRDamage.Add(unit.NetworkId, damage);
                    }
                    return damage;
                }

                var client = unit as AIHeroClient;

                if ((client == null) || client.HasSpellShield() || client.HasUndyingBuffA())
                    return 0f;
                
                if (HasBigRMissile)
                {
                    damage = RDamageBig[R.Level] + Player.Instance.TotalAttackDamage*RDamageBigTotalAdMod[R.Level] +
                             Player.Instance.FlatMagicDamageMod*RDamageBigTotalApMod;
                }
                else
                {
                    damage = RDamageNormal[R.Level] + Player.Instance.TotalAttackDamage*RDamageNormalTotalAdMod[R.Level] +
                             Player.Instance.FlatMagicDamageMod*RDamageNormalTotalApMod;
                }

                damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);
                
                if (MenuManager.IsCacheEnabled)
                {
                    CachedRDamage.Add(unit.NetworkId, damage);
                }
                return damage;
            }
        }
    }
}