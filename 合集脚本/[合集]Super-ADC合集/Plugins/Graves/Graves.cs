#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Graves.cs" company="EloBuddy">
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
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using EloBuddy.SDK.Rendering;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Graves
{
    internal class Graves : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }
        protected static Spell.Skillshot RCone { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu MiscMenu { get; set; }

        private static ColorPicker[] ColorPicker { get; }

        protected static int[] QMana { get; } = {0, 60, 70, 80, 90, 100};
        protected static int[] WMana { get; } = {0, 70, 75, 80, 85, 90};
        protected static int EMana { get; } = 40;
        protected static int RMana { get; } = 100;

        private static bool _changingRangeScan;

        protected static bool IsReloading
            => !Player.Instance.Buffs.Any(b => b.IsActive && b.Name.ToLowerInvariant() == "gravesbasicattackammo1");

        protected static int GetAmmoCount
            => IsReloading
                ? 0
                : Player.Instance.Buffs.Any(b => b.IsActive && b.Name.ToLowerInvariant() == "gravesbasicattackammo2")
                    ? 2
                    : 1;

        private static readonly Dictionary<int, Dictionary<float, float>> Damages =
            new Dictionary<int, Dictionary<float, float>>();

        static Graves()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 825, SkillShotType.Linear, 250, 3000, 40)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 250, 1500, 225);
            E = new Spell.Skillshot(SpellSlot.E, 440, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 1100, SkillShotType.Linear, 250, 2100, 100)
            {
                AllowedCollisionCount = -1
            };
            RCone = new Spell.Skillshot(SpellSlot.R, 700, SkillShotType.Cone, 0, 2000, 110)
            {
                ConeAngleDegrees = (int) (Math.PI/180*70)
            };

            ColorPicker = new ColorPicker[3];

            ColorPicker[0] = new ColorPicker("GravesQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("GravesR", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("GravesHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(ColorPicker[2].Color);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[2].OnColorChange +=
                (a, b) =>
                {
                    DamageIndicator.Color = b.Color;
                };

            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            ChampionTracker.Initialize(ChampionTrackerFlags.LongCastTimeTracker);
            ChampionTracker.OnLongSpellCast += ChampionTracker_OnLongSpellCast;

            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;

            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
        }


        private static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (args.Animation == "Spell4")
            {
                E.Cast(Game.CursorPos.Distance(Player.Instance) > E.Range
                           ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                           : Game.CursorPos);
            }
        }

        private static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;

            var rTarget = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            if (rTarget != null && args.IsAutoAttack() && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (E.IsReady() && R.IsReady() && (Player.Instance.Mana - EMana - RMana > 0) &&
                    rTarget.CountEnemiesInRange(600) <= 2 &&
                    !rTarget.HasUndyingBuffA() && !rTarget.Position.IsVectorUnderEnemyTower())
                {
                    var damage = GetComboDamage(rTarget, 2);

                    if (damage >= rTarget.TotalHealthWithShields())
                    {
                        R.AllowedCollisionCount = int.MaxValue;

                        var rPred = R.GetPrediction(rTarget);

                        if (rPred.HitChancePercent >= 65)
                        {
                            R.Cast(rPred.CastPosition);

                            R.AllowedCollisionCount = -1;
                        }
                    }
                }
            }
        }

        private static void ChampionTracker_OnLongSpellCast(object sender, OnLongSpellCastEventArgs e)
        {
            if (!W.IsReady() || !(Player.Instance.Mana - WMana[W.Level] > EMana + RMana) || !Settings.Combo.UseW || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            if (e.IsTeleport)
            {
                Core.DelayAction(() =>
                {
                    if (W.IsReady() && e.EndPosition.Distance(Player.Instance) < W.Range)
                    {
                        W.Cast(e.EndPosition);
                    }
                }, 4000);
            }
            else if (!e.IsTeleport && e.Sender.IsValidTarget(W.Range))
            {
                var wPrediction = W.GetPrediction(e.Sender);
                if (wPrediction.HitChancePercent >= 60)
                {
                    W.Cast(e.Sender);
                }
            }
        }

        protected static IEnumerable<AIHeroClient> GetRSplashHits(Obj_AI_Base targetToCheckFrom)
        {
            var coneApexPoint = targetToCheckFrom.Position;
            var conePolygon = new Geometry.Polygon.Sector(coneApexPoint, (coneApexPoint-Player.Instance.Position).Normalized(), (float)(Math.PI / 180 * 70), RCone.Range);

            return EntityManager.Heroes.Enemies.Where(x => !x.IsDead && !x.HasUndyingBuffA() && new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).Points.Any(p => conePolygon.IsInside(p)));
        }

        protected static IEnumerable<AIHeroClient> GetRSplashHits(Vector3 positionToCheckFrom)
        {
            var conePolygon = new Geometry.Polygon.Sector(positionToCheckFrom, (positionToCheckFrom - Player.Instance.Position).Normalized(), (float)(Math.PI / 180 * 70), RCone.Range);

            return EntityManager.Heroes.Enemies.Where(x => !x.IsDead && !x.HasUndyingBuffA() && new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).Points.Any(p => conePolygon.IsInside(p)));
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawDamageIndicator)
            {
                return 0;
            }

            return unit.GetType() != typeof (AIHeroClient) ? 0 : GetComboDamage(unit);
        }

        private static float GetComboDamage(Obj_AI_Base unit, int autoAttacks = 1)
        {
            if (Damages.ContainsKey(unit.NetworkId) &&
                !Damages.Any(x => x.Key == unit.NetworkId && x.Value.Any(k => Game.Time*1000 - k.Key > 200)))
                return Damages[unit.NetworkId].Values.FirstOrDefault();

            var damage = 0f;

            if (unit.IsValidTarget(Q.Range) && Q.IsReady())
                damage += Damage.GetQDamage(unit, true);

            if (unit.IsValidTarget(W.Range) && R.IsReady())
                damage += Damage.GetWDamage(unit);

            if (unit.IsValidTarget(R.Range) && R.IsReady())
                damage += Damage.GetRDamage(unit);

            if (Player.Instance.IsInAutoAttackRange(unit))
                damage += Player.Instance.GetAutoAttackDamage(unit) * autoAttacks;

            Damages[unit.NetworkId] = new Dictionary<float, float> { { Game.Time * 1000, damage } };

            return damage;
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (target.GetType() != typeof(AIHeroClient) || target.IsMe || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))//no idea why it invokes twice
                return;

            if (!E.IsReady() || !Settings.Combo.UseE || Settings.Misc.EUsageMode != 1 || Settings.Combo.UseEOnlyToDardoch || GetAmmoCount > 1)
                return;

            var heroClient = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 425, DamageType.Physical);
            var position = Vector3.Zero;

            if (heroClient == null)
                return;

            var dmg = Player.Instance.GetAutoAttackDamage(heroClient, true) * 2;

            if (Q.IsReady())
                dmg += Player.Instance.GetSpellDamage(heroClient, SpellSlot.Q);
            if (W.IsReady())
                dmg += Player.Instance.GetSpellDamage(heroClient, SpellSlot.W);
            if (R.IsReady())
                dmg += Player.Instance.GetSpellDamage(heroClient, SpellSlot.R);

            if (!((dmg < heroClient.TotalHealthWithShields()) || (Q.IsReady() && W.IsReady())))
                return;

            if (Settings.Misc.EMode == 0)
            {
                if (Player.Instance.HealthPercent > heroClient.HealthPercent + 5 && heroClient.CountEnemiesInRange(600) <= 2)
                {
                    if (!Player.Instance.Position.Extend(Game.CursorPos, 420)
                        .To3D()
                        .IsVectorUnderEnemyTower() &&
                        (!heroClient.IsMelee ||
                         Player.Instance.Position.Extend(Game.CursorPos, 420)
                             .IsInRange(heroClient, heroClient.GetAutoAttackRange() * 1.5f)))
                    {
                        Misc.PrintDebugMessage("1v1 Game.CursorPos");
                        position = Game.CursorPos.Distance(Player.Instance) > E.Range
                            ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                            : Game.CursorPos;
                    }
                }
                else
                {
                    var closest =
                        EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1300))
                            .OrderBy(x => x.Distance(Player.Instance)).ToArray()[0];

                    var list =
                        SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900,
                            1300,
                            heroClient.IsMelee ? heroClient.GetAutoAttackRange() * 2 : heroClient.GetAutoAttackRange())
                            .Where(
                                x =>
                                    !x.Key.To3D().IsVectorUnderEnemyTower() &&
                                    x.Key.IsInRange(Prediction.Position.PredictUnitPosition(closest, 850),
                                        Player.Instance.GetAutoAttackRange() - 50))
                            .Select(source => source.Key)
                            .ToList();

                    if (list.Any())
                    {
                        var paths =
                            EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1300))
                                .Select(x => x.Path)
                                .Count(result => result != null && result.Last().Distance(Player.Instance) < 300);

                        var asc = Misc.SortVectorsByDistance(list, heroClient.Position.To2D())[0].To3D();
                        if (Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange()) == 0 &&
                            !EntityManager.Heroes.Enemies.Where(x => x.Distance(Player.Instance) < 1000).Any(
                                x => Prediction.Position.PredictUnitPosition(x, 800)
                                    .IsInRange(asc,
                                        x.IsMelee ? x.GetAutoAttackRange() * 2 : x.GetAutoAttackRange())))
                        {
                            position = asc;

                            Misc.PrintDebugMessage("Paths low sorting Ascending");
                        }
                        else if (Player.Instance.CountEnemiesInRange(1000) <= 2 && (paths == 0 || paths == 1) &&
                                 ((closest.Health < Player.Instance.GetAutoAttackDamage(closest, true) * 2) ||
                                  (Orbwalker.LastTarget is AIHeroClient &&
                                   Orbwalker.LastTarget.Health <
                                   Player.Instance.GetAutoAttackDamage(closest, true) * 2)))
                        {
                            position = asc;
                        }
                        else
                        {
                            position =
                                Misc.SortVectorsByDistanceDescending(list, heroClient.Position.To2D())[0].To3D();
                            Misc.PrintDebugMessage("Paths high sorting Descending");
                        }
                    }
                    else Misc.PrintDebugMessage("1v1 not found positions...");
                }

                if (position != Vector3.Zero && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(900)))
                {
                    E.Cast(position.Distance(Player.Instance) > E.Range ? Player.Instance.Position.Extend(position, E.Range).To3D() : position);
                }
            }
            else if (Settings.Misc.EMode == 1)
            {
                var enemies = Player.Instance.CountEnemiesInRange(1300);
                var pos = Game.CursorPos.Distance(Player.Instance) > E.Range
                    ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                    : Game.CursorPos;

                if (!pos.IsVectorUnderEnemyTower())
                {
                    if (heroClient.IsMelee &&
                        !pos.IsInRange(Prediction.Position.PredictUnitPosition(heroClient, 850),
                            heroClient.GetAutoAttackRange() + 150))
                    {
                        E.Cast(pos);
                        return;
                    }
                    if (!heroClient.IsMelee)
                    {
                        E.Cast(pos);
                    }
                }
                else if (enemies == 2 && Player.Instance.CountAlliesInRange(850) >= 1)
                {
                    E.Cast(pos);
                }
                else if (enemies >= 2)
                {
                    if (
                        !EntityManager.Heroes.Enemies.Any(
                            x =>
                                pos.IsInRange(Prediction.Position.PredictUnitPosition(x, 400),
                                    x.IsMelee ? x.GetAutoAttackRange() + 150 : x.GetAutoAttackRange())))
                    {
                        E.Cast(pos);
                    }
                }
            }
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Graves.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[1].Color, R.Range, Player.Instance);
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
            ComboMenu.AddGroupLabel("Combo mode settings for Graves addon");

            ComboMenu.AddLabel("End of the Line	(Q) settings :");
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Smoke Screen (W) settings :");
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Quickdraw (E) settings :");
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseEOnlyToDardoch", new CheckBox("Use E only if can perform Dardoch trick", false));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Collateral Damage (R) settings :");
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Graves.ComboMenu.RMinEnemiesHit", new Slider("Use R only if will hit {0} enemies", 0, 0, 5));
            ComboMenu.AddLabel("If set to 0 this setting will be ignored.");
            ComboMenu.Add("Plugins.Graves.ComboMenu.RKeybind", new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Graves addon");

            HarassMenu.AddLabel("End of the Line (Q) settings :");
            HarassMenu.Add("Plugins.Graves.HarassMenu.UseQ", new CheckBox("Use Q", false));
            HarassMenu.Add("Plugins.Graves.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Graves addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("End of the Line (Q) settings :");
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.MinMinionsHitQ", new Slider("Min minions hit to use Q", 3, 1, 8));
            LaneClearMenu.AddSeparator(5);
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.UseEInJungleClear", new CheckBox("Use E in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.MinManaE", new Slider("Min mana percentage ({0}%) to use E", 50, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Graves addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Graves.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Quickdraw (E) settings :");
            MiscMenu.Add("Plugins.Graves.MiscMenu.EMode", new ComboBox("E mode", 0, "Auto", "Cursor Pos"));
            MiscMenu.Add("Plugins.Graves.MiscMenu.EUsageMode", new ComboBox("E usage", 1, "Always", "After autoattack only"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Graves addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("End of the Line (Q) settings :");
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Collateral Damage (R) settings :");
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };

            DrawingsMenu.AddLabel("Damage indicator settings :");
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawDamageIndicator", new CheckBox("Draw damage indicator")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DamageIndicatorColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Graves.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Graves.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Graves.ComboMenu.UseE"];

                public static bool UseEOnlyToDardoch => MenuManager.MenuValues["Plugins.Graves.ComboMenu.UseEOnlyToDardoch"];

                public static bool UseR => MenuManager.MenuValues["Plugins.Graves.ComboMenu.UseR"];

                public static int RMinEnemiesHit => MenuManager.MenuValues["Plugins.Graves.ComboMenu.RMinEnemiesHit", true];

                public static bool RKeybind => MenuManager.MenuValues["Plugins.Graves.ComboMenu.RKeybind"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Graves.HarassMenu.UseQ"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Graves.HarassMenu.MinManaQ", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.UseQInJungleClear"];

                public static bool UseEInJungleClear => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.UseEInJungleClear"];

                public static int MinManaE => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.MinManaE", true];

                public static int MinMinionsHitQ => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.MinMinionsHitQ", true];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Graves.LaneClearMenu.MinManaQ", true];
            }

            internal static class Misc
            {
                public static bool EnableKillsteal => MenuManager.MenuValues["Plugins.Graves.MiscMenu.EnableKillsteal"];

                /// <summary>
                /// 0 - "Auto"
                /// 1 - "Cursor Pos"
                /// </summary>
                public static int EMode => MenuManager.MenuValues["Plugins.Graves.MiscMenu.EMode", true];

                /// <summary>
                /// 0 - "Always"
                /// 1 - "After autoattack only"
                /// </summary>
                public static int EUsageMode => MenuManager.MenuValues["Plugins.Graves.MiscMenu.EUsageMode", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Graves.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.Graves.DrawingsMenu.DrawQ"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.Graves.DrawingsMenu.DrawR"];

                public static bool DrawDamageIndicator => MenuManager.MenuValues["Plugins.Graves.DrawingsMenu.DrawDamageIndicator"];
            }
        }

        protected static class Damage
        {
            public static int[] QDamage { get; } = { 0, 55, 70, 85, 100, 115 };
            public static float QDamageBonusAdMod { get; } = 0.75f;
            public static int[] QExplosionDamage { get; } = { 0, 80, 125, 170, 215, 260 };
            public static float[] QExplosionDamageBonusAdMod { get; } = { 0, 0.4f, 0.6f, 0.8f, 1, 1.2f };

            public static int[] WDamage { get; } = { 0, 60, 110, 160, 210, 260 };
            public static float WDamageApMod { get; } = 0.6f;

            public static int[] RDamage { get; } = { 0, 250, 400, 550 };
            public static float RDamageBonusAdMod { get; } = 1.5f;

            public static float GetQDamage(Obj_AI_Base unit, bool includeExplosionDamage = false)
            {
                var damage = QDamage[Q.Level] + Player.Instance.FlatPhysicalDamageMod*QDamageBonusAdMod;

                var explosionDamage = QExplosionDamage[Q.Level] + Player.Instance.FlatPhysicalDamageMod * QExplosionDamageBonusAdMod[Q.Level];

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    includeExplosionDamage ? damage + explosionDamage : damage);
            }

            public static float GetWDamage(Obj_AI_Base unit)
            {
                var damage = WDamage[W.Level] + Player.Instance.FlatMagicDamageMod * WDamageApMod;

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);
            }

            public static float GetRDamage(Obj_AI_Base unit, bool includeSplashDamage = false)
            {
                var damage = RDamage[R.Level] + Player.Instance.FlatPhysicalDamageMod * RDamageBonusAdMod;

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, includeSplashDamage ? damage*1.8f : damage);
            }
        }
    }
}
