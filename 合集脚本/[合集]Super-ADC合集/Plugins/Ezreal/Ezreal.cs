#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Ezreal.cs" company="EloBuddy">
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
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using EloBuddy.SDK.Rendering;
using Marksman_Master.Cache.Modules;
using Marksman_Master.PermaShow.Values;
using Marksman_Master.Utils;
using Color = SharpDX.Color;

namespace Marksman_Master.Plugins.Ezreal
{
    internal class Ezreal : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu MiscMenu { get; set; }

        private static ColorPicker[] ColorPicker { get; }

        private static BoolItem AutoHarassItem { get; set; }
        private static BoolItem AutoHarassItem2 { get; set; }

        private static bool _changingRangeScan;

        protected static bool IsPreAttack { get; private set; }
        protected static bool IsPostAttack { get; private set; }

        protected static bool HasPassiveBuff
            => Player.Instance.Buffs.Any(b => b.IsActive && b.Name.Equals("ezrealrisingspellforce", StringComparison.CurrentCultureIgnoreCase));

        protected static BuffInstance GetPassiveBuff
            => Player.Instance.Buffs.Find(b => b.IsActive && b.Name.Equals("ezrealrisingspellforce", StringComparison.CurrentCultureIgnoreCase));

        protected static int GetPassiveBuffAmount
            => HasPassiveBuff ? Player.Instance.Buffs.Find(
                        b => b.IsActive && b.Name.Equals("ezrealrisingspellforce", StringComparison.CurrentCultureIgnoreCase)).Count : 0;

        protected static Cache.Cache Cache => StaticCacheProvider.Cache;

        protected static CustomCache<int, float> ComboDamages { get; }

        private static readonly Text Text;

        protected static bool FarmMode
            => (Orbwalker.ActiveModesFlags &
                 (Orbwalker.ActiveModes.Harass | Orbwalker.ActiveModes.LaneClear | Orbwalker.ActiveModes.LastHit |
                  Orbwalker.ActiveModes.JungleClear)) != 0;

        static Ezreal()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1300, SkillShotType.Linear, 250, 2000, 60);
            W = new Spell.Skillshot(SpellSlot.W, 1050, SkillShotType.Linear, 250, 1600, 80)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Skillshot(SpellSlot.E, 475, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 6000, SkillShotType.Linear, 1000, 2000, 160)
            {
                AllowedCollisionCount = int.MaxValue
            };

            ComboDamages = Cache.Resolve<CustomCache<int, float>>(1000);

            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("EzrealQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("EzrealW", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("EzrealE", new ColorBGRA(255, 134, 0, 255));
            ColorPicker[3] = new ColorPicker("EzrealHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(ColorPicker[3].Color);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ChampionTracker.Initialize(ChampionTrackerFlags.LongCastTimeTracker);

            Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;

            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            ColorPicker[3].OnColorChange +=
                (a, b) =>
                {
                    DamageIndicator.Color = b.Color;
                };

            TearStacker.Initializer(new Dictionary<SpellSlot, float> {{SpellSlot.Q, 3000}, {SpellSlot.W, 4200}},
                () => (Player.Instance.CountEnemiesInRange(1000) == 0) && (Player.Instance.CountEnemyMinionsInRange(1000) == 0) && !HasAnyOrbwalkerFlags());

            Orbwalker.OnPreAttack += (a, b) =>
            {
                if (a.IsMe)
                    return;

                IsPreAttack = true;
            };

            Orbwalker.OnPostAttack += (a, b) =>
            {
                IsPreAttack = false;
                IsPostAttack = true;
            };

            Orbwalker.OnUnkillableMinion += (target, args) =>
            {
                if (FarmMode && Q.IsReady() && (Q.GetPrediction(target).Collision == false) && (Prediction.Health.GetPrediction(target,
                    (int)((target.DistanceCached(Player.Instance) + Q.CastDelay) / Q.Speed * 1000)) > 0))
                {
                    Q.Cast(target.ServerPosition);
                }
            };

            ChampionTracker.OnLongSpellCast += ChampionTracker_OnLongSpellCast;
            ChampionTracker.OnPostBasicAttack += ChampionTracker_OnPostBasicAttack;

            Obj_AI_Base.OnSpellCast += (sender, args) =>
            {
                if (Settings.Combo.WEComboKeybind && E.IsReady() && sender.IsMe && (args.Slot == SpellSlot.W))
                {
                    E.Cast(Player.Instance.Position.Extend(args.End, E.Range - 15).To3D());
                }
            };
        }
        
        private static void ChampionTracker_OnPostBasicAttack(object sender, PostBasicAttackArgs e)
        {
            if (!e.Sender.IsMe)
                return;

            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            if (!Q.IsReady() || !Settings.Combo.UseQ || Player.Instance.HasSheenBuff())
                return;

            var possibleTargets =
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x =>
                        x.IsValidTargetCached(Q.Range) &&
                        !StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(k =>
                            k.IsValidTargetCached(Q.Range - 80) &&
                            !k.HasSpellShield() &&
                            !k.HasUndyingBuffA() &&
                            (k.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(k, SpellSlot.Q))) &&
                        (Q.GetPrediction(x).HitChancePercent > 65) &&
                        !x.HasSpellShield() && !x.HasUndyingBuffA()).ToList();

            if (!possibleTargets.Any())
                return;

            var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

            if ((target != null) && !Player.Instance.HasSheenBuff() && !IsPreAttack)
            {
                Q.CastMinimumHitchance(target, 65);
            }
        }

        private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe || (sender.GetType() != typeof(AIHeroClient)) || sender.IsEnemy || !Settings.Misc.WToPushTowers)
                return;

            if (W.IsReady() && sender.IsValidTargetCached(W.Range) && (args.Target != null) && args.Target.IsValid &&
                ((args.Target.Type == GameObjectType.obj_AI_Turret) ||
                 (args.Target.Type == GameObjectType.obj_BarracksDampener) || (args.Target.Type == GameObjectType.obj_HQ)))
            {
                W.CastMinimumHitchance(sender, 85);
            }
        }

        private static void ChampionTracker_OnLongSpellCast(object sender, OnLongSpellCastEventArgs e)
        {
            if (e.IsTeleport)
                return;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Q.IsReady() && Settings.Combo.UseQ &&
                Settings.Combo.UseQOnImmobile && !Player.Instance.HasSheenBuff())
            {
                Q.CastMinimumHitchance(e.Sender, 65);
            }
            else if (Settings.Harass.IsAutoHarassEnabledFor(e.Sender) && Q.IsReady() && Settings.Harass.UseQ && (Player.Instance.ManaPercent >= Settings.Harass.MinManaQ) &&
                     !Player.Instance.HasSheenBuff())
            {
                Q.CastMinimumHitchance(e.Sender, 65);
            }
        }

        private static bool HasAnyOrbwalkerFlags()
        {
            return (Orbwalker.ActiveModesFlags & (Orbwalker.ActiveModes.Combo | Orbwalker.ActiveModes.Harass | Orbwalker.ActiveModes.LaneClear | Orbwalker.ActiveModes.LastHit | Orbwalker.ActiveModes.JungleClear | Orbwalker.ActiveModes.Flee)) != 0;
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawDamageIndicator)
            {
                return 0;
            }

            return unit.GetType() != typeof(AIHeroClient) ? 0 : GetComboDamage(unit);
        }

        protected static float GetComboDamage(Obj_AI_Base unit)
        {
            if (MenuManager.IsCacheEnabled && ComboDamages.Exist(unit.NetworkId))
            {
                return ComboDamages.Get(unit.NetworkId);
            }

            var damage = 0f;

            if (Q.IsReady() && unit.IsValidTargetCached(Q.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.Q);

            if (W.IsReady() && unit.IsValidTargetCached(W.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.W);

            if (E.IsReady() && unit.IsValidTargetCached(E.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.E);

            if (R.IsReady() && unit.IsValidTargetCached(R.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.R);

            if (Player.Instance.IsInAutoAttackRange(unit))
                damage += Player.Instance.GetAutoAttackDamageCached(unit, true);

            if (MenuManager.IsCacheEnabled)
            {
                ComboDamages.Add(unit.NetworkId, damage);
            }

            return damage;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Ezreal.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[2].Color, E.Range, Player.Instance);
            
            if (!R.IsReady() || !Settings.Drawings.DrawDamageIndicator)
                return;

            foreach (var source in EntityManager.Heroes.Enemies.Where(
                x => x.IsHPBarRendered && x.Position.IsOnScreen()))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30;
                var percentDamage = Math.Min(100, Damage.GetRDamage(source) / source.TotalHealthWithShields() * 100);

                Text.X = (int)(hpPosition.X - 80);
                Text.Y = (int)source.HPBarPosition.Y;
                Text.Color = new Misc.HsvColor(Misc.GetNumberInRangeFromProcent(percentDamage, 3, 110), 1, 1).ColorFromHsv();
                Text.TextValue = $"R : {percentDamage.ToString("F1")}%";
                Text.Draw();
            }
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
            ComboMenu.AddGroupLabel("EZ 连招 设置");

            ComboMenu.AddLabel("Q设置 :");
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.UseQOnImmobile", new CheckBox("自动Q不动的敌人"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("W设置 :");
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("E设置 :");
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(2);
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.WEComboKeybind", new KeyBind("W => E 连招", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("R设置 :");
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.UseRToKillsteal", new CheckBox("仅在能杀死的时候使用R"));
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.RMinEnemiesHit", new Slider("能打中 {0} 人的时候使用R", 3, 1, 5));
            ComboMenu.Add("Plugins.Ezreal.ComboMenu.RKeybind", new KeyBind("手动R 热键", false, KeyBind.BindTypes.HoldActive, 'T'));

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("EZ 骚扰 设置");

            HarassMenu.AddLabel("Q设置 :");
            HarassMenu.Add("Plugins.Ezreal.HarassMenu.UseQ",
                new KeyBind("自动骚扰热键", false, KeyBind.BindTypes.PressToggle, 'A')).OnValueChange
                +=
                (sender, args) =>
                {
                    AutoHarassItem.Value = args.NewValue;
                };

            HarassMenu.AddLabel("W设置 :");
            HarassMenu.Add("Plugins.Ezreal.HarassMenu.UseW",
                new KeyBind("Enable auto harass", false, KeyBind.BindTypes.PressToggle, 'H')).OnValueChange
                +=
                (sender, args) =>
                {
                    AutoHarassItem2.Value = args.NewValue;
                };
            HarassMenu.Add("Plugins.Ezreal.HarassMenu.MinManaQ", new Slider("最小蓝 百分比 ({0}%) 自动骚扰", 30, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("自动骚扰开启");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                HarassMenu.Add("Plugins.Ezreal.HarassMenu.UseQ." + enemy.Hero, new CheckBox(enemy.ChampionName == "MonkeyKing" ? "Wukong" : enemy.ChampionName));
            }

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("EZ 清线 设置");

            LaneClearMenu.AddLabel("基本设置 :");
            LaneClearMenu.Add("Plugins.Ezreal.LaneClearMenu.EnableLCIfNoEn", new CheckBox("只有附近没有敌人才能启用清线", false));
            var scanRange = LaneClearMenu.Add("Plugins.Ezreal.LaneClearMenu.ScanRange", new Slider("扫描敌人范围", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Ezreal.LaneClearMenu.AllowedEnemies", new Slider("敌人数量", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Q设置 :");
            LaneClearMenu.Add("Plugins.Ezreal.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Ezreal.LaneClearMenu.LaneClearQMode", new ComboBox("Q 清线模式: ", 0, "只补能杀死的", "推线模式"));
            LaneClearMenu.AddSeparator(5);
            LaneClearMenu.Add("Plugins.Ezreal.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Ezreal.LaneClearMenu.MinManaQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 50, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("EZ 其他 设置");
            MiscMenu.AddLabel("基本设置 :");
            MiscMenu.Add("Plugins.Ezreal.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(2);
            MiscMenu.Add("Plugins.Ezreal.MiscMenu.KeepPassiveStacks", new CheckBox("如果可以 保持被动成数"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("W设置 :");
            MiscMenu.Add("Plugins.Ezreal.MiscMenu.WToPushTowers", new CheckBox("W队友推塔"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("E设置 :");
            MiscMenu.Add("Plugins.Ezreal.MiscMenu.EAntiMelee", new CheckBox("使用E反突进"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("R设置 :");
            MiscMenu.Add("Plugins.Ezreal.MiscMenu.MaxRRangeKillsteal", new Slider("使用R击杀敌人的范围", 8000, 0, 20000));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Tear Stacker settings :");
            MiscMenu.Add("Plugins.Ezreal.MiscMenu.EnableTearStacker", new CheckBox("自动叠眼泪")).OnValueChange +=
                (a, b) =>
                {
                    TearStacker.Enabled = b.NewValue;
                };

            MiscMenu.Add("Plugins.Ezreal.MiscMenu.StackOnlyInFountain", new CheckBox("只有在泉水叠眼泪", false)).OnValueChange +=
                (a, b) =>
                {
                    TearStacker.OnlyInFountain = b.NewValue;
                };

            MiscMenu.Add("Plugins.Ezreal.MiscMenu.MinimalManaPercentTearStacker", new Slider("最小蓝 百分比 ({0}%) 自动叠眼泪",  50)).OnValueChange +=
                (a, b) =>
                {
                    TearStacker.MinimumManaPercent = b.NewValue;
                };

            MiscMenu.AddSeparator(5);
            
            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Ezreal addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Mystic Shot (Q) settings :");
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Essence Flux (W) settings :");
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DrawW", new CheckBox("Draw W range", false));
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DrawWColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };

            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Arcane Shift (E) settings :");
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DrawE", new CheckBox("Draw E range", false));
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };

            DrawingsMenu.AddLabel("Damage indicator settings :");
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DrawDamageIndicator", new CheckBox("Draw damage indicator")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Ezreal.DrawingsMenu.DamageIndicatorColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[3].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };

            TearStacker.Enabled = Settings.Misc.EnableTearStacker;
            TearStacker.OnlyInFountain = Settings.Misc.StackOnlyInFountain;
            TearStacker.MinimumManaPercent = Settings.Misc.MinimalManaPercentTearStacker;

            AutoHarassItem = MenuManager.PermaShow.AddItem("Ezreal.AutoHarass", new BoolItem("Auto harass with Q", Settings.Harass.UseQ));
            AutoHarassItem2 = MenuManager.PermaShow.AddItem("Ezreal.AutoHarassW", new BoolItem("Auto harass with W", Settings.Harass.UseW));
        }

        protected override void PermaActive()
        {
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.UseQ"];

                public static bool UseQOnImmobile => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.UseQOnImmobile"]; 

                public static bool UseW => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.UseE"];

                // ReSharper disable once InconsistentNaming
                public static bool WEComboKeybind => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.WEComboKeybind"];
                
                public static bool UseR => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.UseR"];

                public static bool UseROnlyToKillsteal => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.UseRToKillsteal"];

                public static int RMinEnemiesHit => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.RMinEnemiesHit", true];

                public static bool RKeybind => MenuManager.MenuValues["Plugins.Ezreal.ComboMenu.RKeybind"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Ezreal.HarassMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Ezreal.HarassMenu.UseW"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Ezreal.HarassMenu.MinManaQ", true];

                public static bool IsAutoHarassEnabledFor(AIHeroClient unit) => MenuManager.MenuValues["Plugins.Ezreal.HarassMenu.UseQ." + unit.Hero];

                public static bool IsAutoHarassEnabledFor(string championName) => MenuManager.MenuValues["Plugins.Ezreal.HarassMenu.UseQ." + championName];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Ezreal.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Ezreal.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Ezreal.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Ezreal.LaneClearMenu.UseQInLaneClear"];

                /// <summary>
                /// 0 - "Last Hit",
                /// 1 - "Push wave"
                /// </summary>
                public static int LaneClearQMode => MenuManager.MenuValues["Plugins.Ezreal.LaneClearMenu.LaneClearQMode", true];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Ezreal.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Ezreal.LaneClearMenu.MinManaQ", true];
            }

            internal static class Misc
            {
                public static bool EnableKillsteal => MenuManager.MenuValues["Plugins.Ezreal.MiscMenu.EnableKillsteal"];

                public static bool KeepPassiveStacks => MenuManager.MenuValues["Plugins.Ezreal.MiscMenu.KeepPassiveStacks"];

                public static bool EAntiMelee => MenuManager.MenuValues["Plugins.Ezreal.MiscMenu.EAntiMelee"];

                public static int MaxRRangeKillsteal => MenuManager.MenuValues["Plugins.Ezreal.MiscMenu.MaxRRangeKillsteal", true];

                public static bool EnableTearStacker => MenuManager.MenuValues["Plugins.Ezreal.MiscMenu.EnableTearStacker"];
                
                public static bool StackOnlyInFountain => MenuManager.MenuValues["Plugins.Ezreal.MiscMenu.StackOnlyInFountain"];

                public static bool WToPushTowers => MenuManager.MenuValues["Plugins.Ezreal.MiscMenu.WToPushTowers"];

                public static int MinimalManaPercentTearStacker => MenuManager.MenuValues["Plugins.Ezreal.MiscMenu.MinimalManaPercentTearStacker", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Ezreal.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.Ezreal.DrawingsMenu.DrawQ"];

                public static bool DrawW => MenuManager.MenuValues["Plugins.Ezreal.DrawingsMenu.DrawW"];

                public static bool DrawE => MenuManager.MenuValues["Plugins.Ezreal.DrawingsMenu.DrawE"];
                
                public static bool DrawDamageIndicator => MenuManager.MenuValues["Plugins.Ezreal.DrawingsMenu.DrawDamageIndicator"];
            }
        }

        protected static class Damage
        {
            private static CustomCache<int, float> RDamages => Cache.Resolve<CustomCache<int, float>>(1000);

            public static float GetRDamage(Obj_AI_Base target)
            {
                if (MenuManager.IsCacheEnabled && RDamages.Exist(target.NetworkId))
                {
                    return RDamages.Get(target.NetworkId);
                }

                var polygon = new Geometry.Polygon.Rectangle(Player.Instance.Position, target.Position, 160);
                var objects = ObjectManager
                        .Get<Obj_AI_Base>().Count(x => (x.NetworkId != target.NetworkId) && x.IsEnemy &&
                            x.IsValidTarget() &&
                            new Geometry.Polygon.Circle(Prediction.Position.PredictUnitPosition(x, 1000 + (int)(x.DistanceCached(Player.Instance) / R.Speed)*1000), x.BoundingRadius).Points.Any(
                                p => polygon.IsInside(p)));

                var damage = Player.Instance.GetSpellDamageCached(target, SpellSlot.R);
                var minDamage = damage * .3f;

                for (var i = 1; i <= objects; i++)
                {
                    damage *= .9f;
                }

                var finalDamage = damage >= minDamage ? damage : minDamage;

                if (MenuManager.IsCacheEnabled)
                {
                    RDamages.Add(target.NetworkId, finalDamage);
                }

                return finalDamage;
            }
        }
    }
}