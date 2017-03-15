#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Caitlyn.cs" company="EloBuddy">
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
using EloBuddy.SDK.Rendering;
using SharpDX;
using System.Drawing;
using EloBuddy.SDK.Menu.Values;
using Marksman_Master.Cache.Modules;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Caitlyn
{
    internal class Caitlyn : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Targeted R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu MiscMenu { get; set; }

        private static ColorPicker[] ColorPicker { get; }

        private static bool _changingRangeScan;

        protected static bool IsUnitNetted(AIHeroClient unit) => unit.Buffs.Any(x => x.Name == "caitlynyordletrapinternal");
        protected static bool IsUnitImmobilizedByTrap(AIHeroClient unit) => unit.Buffs.Any(x => x.Name == "caitlynyordletrapdebuff");
        protected static bool HasAutoAttackRangeBuff => Player.Instance.Buffs.Any(x => x.Name == "caitlynheadshotrangecheck");
        protected static bool HasAutoAttackRangeBuffOnChamp => Player.Instance.Buffs.Any(x => x.Name == "caitlynheadshotrangecheck") && EntityManager.Heroes.Enemies.Any(x=> x.IsValidTarget(1350) && IsUnitNetted(x));

        private static readonly Text Text;

        protected static bool HasItemFirecanon
            => Player.Instance.InventoryItems.Any(x => x.Id == ItemId.Rapid_Firecannon);

        protected static bool HasFirecanonStackedUp
            => Player.Instance.Buffs.Any(x => HasItemFirecanon && x.Name.ToLowerInvariant() == "itemstatikshankcharge" && x.Count == 100);

        protected static float BasicAttackRange => HasFirecanonStackedUp ? 900 : 750;

        protected static bool IsPreAttack { get; private set; }

        protected static Cache.Cache Cache => StaticCacheProvider.Cache;

        protected static bool IsValidWCast(Vector3 castPosition, float minRange = 200, int time = 2000)
            => (!GetTrapsInRange(castPosition, minRange).Any() && (Core.GameTickCount - _lastWCastTime >= time));

        static Caitlyn()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1300, SkillShotType.Linear, 625, 2200, 90)
            {
                AllowedCollisionCount = -1
            };
            W = new Spell.Skillshot(SpellSlot.W, 800, SkillShotType.Circular, 1600)
            {
                Width = 20
            };
            E = new Spell.Skillshot(SpellSlot.E, 800, SkillShotType.Linear, 150, 1600, 80)
            {
                AllowedCollisionCount = 0
            };
            R = new Spell.Targeted(SpellSlot.R, 2000);

            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("CaitlynQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("CaitlynE", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("CaitlynR", new ColorBGRA(255, 134, 0, 255));
            ColorPicker[3] = new ColorPicker("CaitlynHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(ColorPicker[3].Color, (int)R.Range);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ChampionTracker.Initialize(ChampionTrackerFlags.LongCastTimeTracker);

            ColorPicker[3].OnColorChange +=
                (a, b) =>
                {
                    DamageIndicator.Color = b.Color;
                };

            Orbwalker.OnPostAttack += (sender, args) =>
            {
                IsPreAttack = false;

                if(Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    Modes.Combo.Execute();
            };

            Orbwalker.OnPreAttack += (target, args) => IsPreAttack = true;

            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            ChampionTracker.OnLongSpellCast += ChampionTracker_OnLongSpellCast;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static float _lastWCastTime;

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && args.SData.Name.Equals("SummonerFlash", StringComparison.CurrentCultureIgnoreCase) && (sender.Position.Extend(args.End, sender.Distance(args.End) >= 475 ? 475 : sender.Distance(args.End)).Distance(Player.Instance) <= 300))
            {
                E.Cast(
                    sender.Position.Extend(args.End, sender.Distance(args.End) >= 475 ? 475 : sender.Distance(args.End))
                        .To3D());
            }

            if (!sender.IsMe)
                return;

            if (args.Slot == SpellSlot.W)
            {
                _lastWCastTime = Core.GameTickCount;
            }
        }

        private static void ChampionTracker_OnLongSpellCast(object sender, OnLongSpellCastEventArgs e)
        {
            if (!W.IsReady() || !Settings.Combo.UseWOnImmobile)
                return;

            if (e.IsTeleport && W.IsInRange(e.EndPosition) && IsValidWCast(e.EndPosition))
            {
                W.Cast(e.EndPosition);
            }
            else if(e.Sender.IsValidTarget(W.Range) && IsValidWCast(e.Sender.ServerPosition))
            {
                W.Cast(e.Sender.ServerPosition);
            }
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
            var damage = 0f;

            if (unit.IsValidTargetCached(Q.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.Q);

            if (unit.IsValidTargetCached(W.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.W);

            if (unit.IsValidTargetCached(E.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.E);

            if (unit.IsValidTargetCached(R.Range))
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.R);

            if (Player.Instance.IsInAutoAttackRange(unit))
                damage += Player.Instance.GetAutoAttackDamageCached(unit);

            return damage;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Caitlyn.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[1].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);

            if (!Settings.Drawings.DrawDamageIndicator || !R.IsReady())
                return;

            foreach (var source in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                x => x.IsHPBarRendered && x.Position.IsOnScreen() && x.IsInRangeCached(Player.Instance, R.Range)))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30;
                var percentDamage = Math.Min(100, Player.Instance.GetSpellDamageCached(source, SpellSlot.R) / source.TotalHealthWithShields() * 100);

                Text.X = (int) (hpPosition.X - 50);
                Text.Y = (int) source.HPBarPosition.Y;
                Text.Color = new Misc.HsvColor(Misc.GetNumberInRangeFromProcent(percentDamage, 3, 110), 1, 1).ColorFromHsv();
                Text.TextValue = percentDamage.ToString("F1");
                Text.Draw();
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (Settings.Misc.WAgainstGapclosers && W.IsReady() && W.IsInRange(args.End))
            {
                W.Cast(args.End);
            }

            if (args.GapcloserType != GapcloserTypes.Targeted || !E.IsReady() || !Settings.Misc.EAgainstGapclosers)
                return;

            var ePrediction = E.GetPrediction(sender);

            if (ePrediction.HitChancePercent >= 65 && !GetDashEndPosition(ePrediction.CastPosition).IsVectorUnderEnemyTower())
            {
                Core.DelayAction(() => E.Cast(sender), args.Delay);
            }
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("女警 连招 设置");

            ComboMenu.AddLabel("Q设置 :");
            ComboMenu.Add("Plugins.Caitlyn.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("W设置 :");
            ComboMenu.Add("Plugins.Caitlyn.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.Add("Plugins.Caitlyn.ComboMenu.UseWOnImmobile", new CheckBox("Use W on immobile"));
            ComboMenu.Add("Plugins.Caitlyn.ComboMenu.WHitChancePercent", new Slider("W 命中率 : {0}", 85));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("E设置 :");
            ComboMenu.Add("Plugins.Caitlyn.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.Add("Plugins.Caitlyn.ComboMenu.EHitChancePercent", new Slider("E 命中率 : {0}", 65));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("R设置 :");
            ComboMenu.Add("Plugins.Caitlyn.ComboMenu.UseR", new CheckBox("Use R", false));

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("女警 骚扰 设置");

            HarassMenu.AddLabel("Q设置 :");
            HarassMenu.Add("Plugins.Caitlyn.HarassMenu.UseQ", new CheckBox("Use Q", false));
            HarassMenu.Add("Plugins.Caitlyn.HarassMenu.MinManaQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 75, 1));

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("女警 清线 设置");

            LaneClearMenu.AddLabel("基本设置 :");
            LaneClearMenu.Add("Plugins.Caitlyn.LaneClearMenu.EnableLCIfNoEn", new CheckBox("只有附近没有敌人才能启用清线", false));
            var scanRange = LaneClearMenu.Add("Plugins.Caitlyn.LaneClearMenu.ScanRange", new Slider("扫描敌人范围", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Caitlyn.LaneClearMenu.AllowedEnemies", new Slider("敌人数量", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Q设置 :");
            LaneClearMenu.Add("Plugins.Caitlyn.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane clear"));
            LaneClearMenu.Add("Plugins.Caitlyn.LaneClearMenu.MinMinionsKilledForQ", new Slider("最少杀死使用Q", 3, 1, 6));
            LaneClearMenu.AddSeparator(5);
            LaneClearMenu.Add("Plugins.Caitlyn.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle clear"));
            LaneClearMenu.Add("Plugins.Caitlyn.LaneClearMenu.MinManaQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 50, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("女警 其他 设置");
            MiscMenu.AddLabel("基本设置 :");
            MiscMenu.Add("Plugins.Caitlyn.MiscMenu.EnableKillsteal", new CheckBox("反突进"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Yordle Snap Trap (W) settings :");
            MiscMenu.Add("Plugins.Caitlyn.MiscMenu.WAgainstGapclosers", new CheckBox("使用W反突进"));
            
            MiscMenu.AddLabel("90 Caliber Net (E) settings :");
            MiscMenu.Add("Plugins.Caitlyn.MiscMenu.EAgainstGapclosers", new CheckBox("使用E反突进"));

            MenuManager.BuildAntiGapcloserMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Caitlyn addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Piltover Peacemaker (Q) settings :");
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("90 Caliber Net (E) settings :");
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DrawE", new CheckBox("Draw E range", false));
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Ace in the Hole (R) settings :");
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DrawR", new CheckBox("Draw R range", false));
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };

            DrawingsMenu.AddLabel("Damage indicator settings :");
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DrawDamageIndicator", new CheckBox("Draw damage indicator")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Caitlyn.DrawingsMenu.DamageIndicatorColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[3].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
        }

        protected static IEnumerable<Obj_GeneralParticleEmitter> GetTrapsInRange(Vector3 position, float range)
        {
            return
                ObjectManager.Get<Obj_GeneralParticleEmitter>()
                    .Where(
                        x => x.Name.Equals("Caitlyn_Base_W_Indicator_SizeRing.troy", StringComparison.InvariantCultureIgnoreCase) && (x.DistanceCached(position) < range));
        }

        protected static Vector3 GetDashEndPosition(Vector3 castPosition)
        {
            return Player.Instance.Position.Extend(castPosition, -400).To3D();
        }

        protected override void PermaActive()
        {
            R.Range = 2000 + (uint)(500*(R.Level - 1));

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

        protected internal static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Caitlyn.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Caitlyn.ComboMenu.UseW"];

                public static bool UseWOnImmobile => MenuManager.MenuValues["Plugins.Caitlyn.ComboMenu.UseWOnImmobile"];

                public static int WHitChancePercent => MenuManager.MenuValues["Plugins.Caitlyn.ComboMenu.WHitChancePercent", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Caitlyn.ComboMenu.UseE"];

                public static int EHitChancePercent => MenuManager.MenuValues["Plugins.Caitlyn.ComboMenu.EHitChancePercent", true];

                public static bool UseR => MenuManager.MenuValues["Plugins.Caitlyn.ComboMenu.UseR"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Caitlyn.HarassMenu.UseQ"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Caitlyn.HarassMenu.MinManaQ", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Caitlyn.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Caitlyn.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Caitlyn.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Caitlyn.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Caitlyn.LaneClearMenu.UseQInJungleClear"];

                public static int MinMinionsKilledForQ => MenuManager.MenuValues["Plugins.Caitlyn.LaneClearMenu.MinMinionsKilledForQ", true];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Caitlyn.LaneClearMenu.MinManaQ", true];
            }

            internal static class Misc
            {
                public static bool EnableKillsteal => MenuManager.MenuValues["Plugins.Caitlyn.MiscMenu.EnableKillsteal"];

                public static bool WAgainstGapclosers => MenuManager.MenuValues["Plugins.Caitlyn.MiscMenu.WAgainstGapclosers"];

                public static bool EAgainstGapclosers => MenuManager.MenuValues["Plugins.Caitlyn.MiscMenu.EAgainstGapclosers"];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Caitlyn.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.Caitlyn.DrawingsMenu.DrawQ"];

                public static bool DrawE => MenuManager.MenuValues["Plugins.Caitlyn.DrawingsMenu.DrawE"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.Caitlyn.DrawingsMenu.DrawR"];

                public static bool DrawDamageIndicator => MenuManager.MenuValues["Plugins.Caitlyn.DrawingsMenu.DrawDamageIndicator"];
            }
        }

        protected internal static class Damage
        {
            private static CustomCache<int, float> HeadShotDamages => Cache.Resolve<CustomCache<int, float>>(1000);
            private static CustomCache<int, float> RDamages => Cache.Resolve<CustomCache<int, float>>(1000);

            public static float GetHeadShotDamage(AIHeroClient unit)
            {
                if (MenuManager.IsCacheEnabled && HeadShotDamages.Exist(unit.NetworkId))
                {
                    return HeadShotDamages.Get(unit.NetworkId);
                }

                var damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, Player.Instance.TotalAttackDamage * (1 + (0.5f + Player.Instance.FlatCritChanceMod * (1 + 0.5f * (Player.Instance.HasItem(ItemId.Infinity_Edge) ? 0.5f : 0)))), false, true) + (IsUnitImmobilizedByTrap(unit) ? GetTrapAdditionalHeadShotDamage(unit) : 0);

                if (MenuManager.IsCacheEnabled)
                {
                    HeadShotDamages.Add(unit.NetworkId, damage);
                }

                return damage;
            }

            public static float GetTrapAdditionalHeadShotDamage(AIHeroClient unit)
            {
                if (MenuManager.IsCacheEnabled && RDamages.Exist(unit.NetworkId))
                {
                    return RDamages.Get(unit.NetworkId);
                }

                int[] additionalDamage = {0, 30, 70, 110, 150, 190};

                var damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, additionalDamage[W.Level] + Player.Instance.TotalAttackDamage * 0.7f);

                if (MenuManager.IsCacheEnabled)
                {
                    RDamages.Add(unit.NetworkId, damage);
                }

                return damage;
            }
        }
    }
}