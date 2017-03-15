#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Draven.cs" company="EloBuddy">
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
namespace Marksman_Master.Plugins.Draven
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Rendering;
    using SharpDX;
    using Utils;
    using Color = System.Drawing.Color;
    using ColorPicker = Utils.ColorPicker;

    internal class Draven : ChampionPlugin
    {
        protected static Spell.Active Q { get; }
        protected static Spell.Active W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu AxeSettingsMenu { get; set; }

        private static readonly List<AxeObjectData> AxeObjects = new List<AxeObjectData>();
        private static readonly Text Text;
        private static readonly ColorPicker[] ColorPicker;

        protected static float[] WAdditionalMovementSpeed { get; } = {0, 1.4f, 1.45f, 1.5f, 1.55f, 1.6f};

        protected static bool HasSpinningAxeBuff
            => Player.Instance.Buffs.Any(x => x.Name.Equals("dravenspinningattack", StringComparison.CurrentCultureIgnoreCase));

        protected static BuffInstance GetSpinningAxeBuff
            => Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals("dravenspinningattack", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasMoveSpeedFuryBuff
            => Player.Instance.Buffs.Any(x => x.Name.Equals("dravenfury", StringComparison.CurrentCultureIgnoreCase));

        protected static BuffInstance GetMoveSpeedFuryBuff
            => Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals("dravenfury", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasAttackSpeedFuryBuff
            => Player.Instance.Buffs.Any(x => x.Name.Equals("dravenfurybuff", StringComparison.CurrentCultureIgnoreCase));

        protected static BuffInstance GetAttackSpeedFuryBuff
            => Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals("dravenfurybuff", StringComparison.CurrentCultureIgnoreCase));

        private static bool _changingRangeScan;

        private static bool _changingkeybindRange;

        protected static MissileClient DravenRMissile { get; private set; }

        protected static bool IsPreAttack { get; private set; }
        protected static bool IsAfterAttack { get; private set; }

        protected static bool ValidOrbwalkerMode
            => (Orbwalker.ActiveModesFlags &
                 (Orbwalker.ActiveModes.Combo | Orbwalker.ActiveModes.Harass | Orbwalker.ActiveModes.LaneClear | Orbwalker.ActiveModes.LastHit |
                  Orbwalker.ActiveModes.JungleClear)) != 0;

        static Draven()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 950, SkillShotType.Linear, 250, 1400, 130)
            {
                AllowedCollisionCount = -1
            };
            R = new Spell.Skillshot(SpellSlot.R, 30000, SkillShotType.Linear, 500, 2000, 160)
            {
                AllowedCollisionCount = int.MaxValue
            };

            ColorPicker = new ColorPicker[2];
            ColorPicker[0] = new ColorPicker("DravenE", new ColorBGRA(114, 171, 160, 255));
            ColorPicker[1] = new ColorPicker("DravenCatchRange", new ColorBGRA(231, 237, 160, 255));

            Text = new Text(string.Empty, new SharpDX.Direct3D9.FontDescription
            {
                FaceName = "Verdana",
                Weight = SharpDX.Direct3D9.FontWeight.Regular,
                Quality = SharpDX.Direct3D9.FontQuality.NonAntialiased,
                OutputPrecision = SharpDX.Direct3D9.FontPrecision.String,
                Height = 31,
                MipLevels = 1
            });

            ChampionTracker.Initialize(ChampionTrackerFlags.PostBasicAttackTracker);
            ChampionTracker.OnPostBasicAttack += (sender, args) =>
            {
                IsPreAttack = false;
                IsAfterAttack = true;
            };

            Game.OnTick += Game_OnTick;

            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;

            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Orbwalker.OverrideOrbwalkPosition += OverrideOrbwalkPosition;
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if ((target?.GetType() != typeof(AIHeroClient)) || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            if (Q.IsReady() && (GetAxesCount() != 0) && (GetAxesCount() < Settings.Combo.MaxAxesAmount))
                Q.Cast();
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            IsPreAttack = true;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                var jungleMinions = StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTargetCached(Player.Instance.GetAutoAttackRange())).ToList();
                var laneMinions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTargetCached(Player.Instance.GetAutoAttackRange())).ToList();

                if (jungleMinions.Any())
                {
                    if (Settings.LaneClear.UseQInJungleClear && Q.IsReady() && (GetAxesCount() == 0) &&
                        (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ))
                    {
                        Q.Cast();
                    }

                    if (Settings.LaneClear.UseWInJungleClear && W.IsReady() && (jungleMinions.Count > 1) &&
                        !HasAttackSpeedFuryBuff && (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW))
                    {
                        W.Cast();
                    }
                    return;
                }
                if (laneMinions.Any() && Modes.LaneClear.CanILaneClear())
                {
                    if (Settings.LaneClear.UseQInLaneClear && Q.IsReady() && (GetAxesCount() == 0) &&
                        (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ))
                    {
                        Q.Cast();
                    }

                    if (Settings.LaneClear.UseWInLaneClear && W.IsReady() && (laneMinions.Count > 3) &&
                        !HasAttackSpeedFuryBuff && (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW))
                    {
                        W.Cast();
                    }
                    return;
                }
            }

            if ((target.GetType() != typeof(AIHeroClient)) || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            if (Q.IsReady() && (GetAxesCount() == 0))
                Q.Cast();

            if (!W.IsReady() || !Settings.Combo.UseW || HasAttackSpeedFuryBuff || (Player.Instance.Mana - 40 < 145))
                return;

            W.Cast();
        }
        
        private static void Game_OnTick(EventArgs args)
        {
            if (Player.Instance.HasBuffOfType(BuffType.Slow) && W.IsReady() && (Player.Instance.Mana > 85) &&
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(Player.Instance.GetAutoAttackRange())).Any())
            {
                W.Cast();
            }

            AxeObjects.RemoveAll(x => x.EndTick - Game.Time * 1000 <= 0);
        }

        private static Vector3? OverrideOrbwalkPosition()
        {
            if (!Settings.Axe.CatchAxes || !AxeObjects.Any() || (GetAxesCount() == 0))
            {
                return null;
            }
            
            foreach (
                var axeObjectData in
                    AxeObjects.Where(
                        x => 
                            Settings.Axe.CatchAxesMode == 2 ? Player.Instance.IsInRange(x.EndPosition, Settings.Axe.AxeCatchRange) : Game.CursorPos.IsInRange(x.EndPosition, Settings.Axe.AxeCatchRange) &&
                            CanPlayerCatchAxe(x)).OrderBy(x => x.EndPosition.DistanceCached(Player.Instance)))
            {
                var isOutside = !Player.Instance.Position.IsInRange(axeObjectData.EndPosition, 120);
                var isInside = Player.Instance.Position.IsInRange(axeObjectData.EndPosition, 120);

                if ((Settings.Axe.CatchAxesMode == 0) && !Player.Instance.Position.IsInRange(axeObjectData.EndPosition, 250))
                    return null;

                if (isOutside)
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 350,DamageType.Physical);

                        if ((target != null) && (target.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamageCached(target) * 2))
                        {
                            var pos = Prediction.Position.PredictUnitPosition(target, (int)(GetEta(axeObjectData, Player.Instance.MoveSpeed) * 1000));

                            if (!axeObjectData.EndPosition.IsInRangeCached(pos, Player.Instance.GetAutoAttackRange()))
                            {
                                return null;
                            }
                        }
                    }

                    if ((axeObjectData.EndTick - Game.Time < GetEta(axeObjectData, Player.Instance.MoveSpeed)) &&
                        !HasMoveSpeedFuryBuff &&
                        (GetEta(axeObjectData, Player.Instance.MoveSpeed * WAdditionalMovementSpeed[W.Level]) >
                         axeObjectData.EndTick - Game.Time) &&
                        W.IsReady() && Settings.Axe.UseWToCatch)
                    {
                        W.Cast();
                    }

                    return axeObjectData.EndPosition;
                }

                if (isInside)
                {
                    if (!CanPlayerLeaveAxeRangeInDesiredTime(axeObjectData.EndPosition, (axeObjectData.EndTick - Core.GameTickCount) / 1000f))
                    {
                        return null;
                    }

                    var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 100, DamageType.Physical);

                    if ((target != null) &&
                        (target.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamageCached(target, true) * 2) &&
                        !Prediction.Position.PredictUnitPosition(target, (int)(axeObjectData.EndTick - Core.GameTickCount))
                            .IsInRangeCached(Player.Instance, Player.Instance.GetAutoAttackRange()))
                    {
                        return null;
                    }

                    var position =
                        new Geometry.Polygon.Circle(axeObjectData.EndPosition, 110).Points.Where(
                            x =>
                                axeObjectData.EndPosition.To2D()
                                    .ProjectOn(axeObjectData.EndPosition.To2D(), Game.CursorPos.To2D())
                                    .IsOnSegment).OrderBy(x => x.DistanceCached(Game.CursorPos)).FirstOrDefault();

                    if (position == default(Vector2))
                        return null;

                    return position.To3D();
                }
            }
            return null;
        }

        private static bool CanPlayerCatchAxe(AxeObjectData axe)
        {
            if (!Settings.Axe.CatchAxes || !ValidOrbwalkerMode)
            {
                return false;
            }

            if (!Settings.Axe.CatchAxesUnderTower && axe.EndPosition.IsVectorUnderEnemyTower())
                return false;

            return Settings.Axe.CatchAxesNearEnemies || (axe.EndPosition.CountEnemiesInRange(550) <= 2);
        }

        private static float GetEta(AxeObjectData axe, float movespeed)
        {
            return Player.Instance.DistanceCached(axe.EndPosition) / movespeed;
        }

        private static bool CanPlayerLeaveAxeRangeInDesiredTime(Vector3 axeCenterPosition, float time)
        {
            var axePolygon = new Geometry.Polygon.Circle(axeCenterPosition, 90);
            var playerPosition = Player.Instance.ServerPosition;
            var playerLastWaypoint = Player.Instance.Path.LastOrDefault();
            var cloestPoint = axePolygon.Points.OrderBy(x => x.Distance(playerLastWaypoint)).FirstOrDefault();
            var distanceFromPoint = cloestPoint.Distance(playerPosition);
            var distanceInTime = Player.Instance.MoveSpeed * time;

            return distanceInTime > distanceFromPoint;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            if (sender.Name.Contains("Q_reticle_self"))
            {
                AxeObjects.Add(new AxeObjectData
                {
                    EndPosition = sender.Position,
                    EndTick = Core.GameTickCount + 1227.1f,
                    NetworkId = sender.NetworkId,
                    Owner = Player.Instance,
                    StartTick = Core.GameTickCount
                });
            }


            var missile = sender as MissileClient;

            if ((missile == null) || !missile.IsValidMissile())
                return;

            if (missile.SData.Name.Equals("dravenr", StringComparison.CurrentCultureIgnoreCase) && missile.SpellCaster.IsMe)
            {
                DravenRMissile = missile;
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Q_reticle_self"))
            {
                AxeObjects.Remove(AxeObjects.Find(data => data.NetworkId == sender.NetworkId));
            }
            
            var missile = sender as MissileClient;

            if (missile == null)
                return;

            if (missile.SData.Name.Equals("dravenr", StringComparison.CurrentCultureIgnoreCase) && missile.SpellCaster.IsMe)
            {
                DravenRMissile = null;
            }
        }

        protected static int GetAxesCount()
        {
            if (!HasSpinningAxeBuff && (AxeObjects == null))
                return 0;

            if (!HasSpinningAxeBuff && (AxeObjects?.Count > 0))
                return AxeObjects.Count;

            if ((GetSpinningAxeBuff?.Count == 0) && (AxeObjects?.Count > 0))
                return AxeObjects.Count;

            if ((GetSpinningAxeBuff?.Count > 0) && (AxeObjects?.Count == 0))
                return GetSpinningAxeBuff.Count;

            if ((GetSpinningAxeBuff?.Count > 0) && (AxeObjects?.Count > 0))
                return GetSpinningAxeBuff.Count + AxeObjects.Count;

            return 0;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Draven.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (_changingkeybindRange)
                Circle.Draw(SharpDX.Color.White, Settings.Combo.RRangeKeybind, Player.Instance);

            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[0].Color, E.Range, Player.Instance);

            if (Settings.Drawings.DrawAxesCatchRange && (Settings.Axe.CatchAxesMode != 2))
                Circle.Draw(ColorPicker[1].Color, Settings.Axe.AxeCatchRange, Game.CursorPos);

            foreach (var axeObjectData in AxeObjects)
            {
                if (Settings.Drawings.DrawAxes)
                {
                    Circle.Draw(Player.Instance.Position.IsInRangeCached(axeObjectData.EndPosition, 120)
                            ? new ColorBGRA(0, 255, 0, 255)
                            : new ColorBGRA(255, 0, 0, 255), 120, axeObjectData.EndPosition);
                }

                if (!Settings.Drawings.DrawAxesTimer)
                    continue;

                var timeLeft = axeObjectData.EndTick / 1000 - Game.Time;
                var degree = Misc.GetNumberInRangeFromProcent(timeLeft * 1000d / 1227.1 * 100d, 3, 110);
                
                Text.Color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();
                Text.Position = Drawing.WorldToScreen(new Vector3(axeObjectData.EndPosition.X - 80, axeObjectData.EndPosition.Y - 110, axeObjectData.EndPosition.Z));
                Text.TextValue = $"{((axeObjectData.EndTick - Core.GameTickCount)/1000f).ToString("F1")} s";
                Text.Draw();
            }

            var buff =
                Player.Instance.Buffs.Where(
                    x => x.Name.Equals("dravenspinningattack", StringComparison.CurrentCultureIgnoreCase))
                    .OrderByDescending(x => x.EndTime)
                    .FirstOrDefault();

            if (buff == null)
                return;

            {
                var timeLeft = Math.Max(0, buff.EndTime - Game.Time);
                Text.Color = new Misc.HsvColor(169, 0.96, 0.78).ColorFromHsv();
                Text.Position = Drawing.WorldToScreen(new Vector3(Player.Instance.Position.X + 50, Player.Instance.Position.Y, Player.Instance.Position.Z));
                Text.TextValue = $"Q remaining time : {timeLeft.ToString("F1")} s";
                Text.Draw();
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!Settings.Misc.EnableInterrupter || !E.IsReady() || !sender.IsValidTarget(E.Range))
                return;

            if (args.Delay == 0)
                E.Cast(sender);
            else Core.DelayAction(() => E.Cast(sender), args.Delay);
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (!Settings.Misc.EnableAntiGapcloser || (args.End.Distance(Player.Instance) > 350) || !E.IsReady() || !sender.IsValidTarget(E.Range))
                return;

            if(args.Delay == 0)
                E.Cast(sender);
            else Core.DelayAction(() => E.Cast(sender), args.Delay);
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Draven addon");

            ComboMenu.AddLabel("Spinning Axe (Q) settings :");
            ComboMenu.Add("Plugins.Draven.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.Add("Plugins.Draven.ComboMenu.MaxAxesAmount", new Slider("Maximum axes amount", 2, 1, 3));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Blood Rush (W) settings :");
            ComboMenu.Add("Plugins.Draven.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Stand Aside (E) settings :");
            ComboMenu.Add("Plugins.Draven.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Whirling Death (R) settings :");
            ComboMenu.Add("Plugins.Draven.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Draven.ComboMenu.RKeybind",
                new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddLabel("Fires R on best target in range when keybind is active.");
            ComboMenu.AddSeparator(5);
            var keybindRange = ComboMenu.Add("Plugins.Draven.ComboMenu.RRangeKeybind",
                new Slider("Maximum range to enemy to cast R while keybind is active", 1100, 300, 2500));
            keybindRange.OnValueChange += (a, b) =>
            {
                _changingkeybindRange = true;
                Core.DelayAction(() =>
                {
                    if (!keybindRange.IsLeftMouseDown && !keybindRange.IsMouseInside)
                    {
                        _changingkeybindRange = false;
                    }
                }, 2000);
            };

            AxeSettingsMenu = MenuManager.Menu.AddSubMenu("Axe Settings");
            AxeSettingsMenu.AddGroupLabel("Axe settings for Draven addon");
            AxeSettingsMenu.AddLabel("Basic settings :");
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxes", new CheckBox("Catch Axes"));
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.UseWToCatch", new CheckBox("Cast W if axe is uncatchable"));
            AxeSettingsMenu.AddSeparator(5);

            AxeSettingsMenu.AddLabel("Catching settings :");
            var axeMode = AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesMode",
                new ComboBox("Catch mode", 0, "Default", "Brutal", "Yorik"));

            AxeSettingsMenu.AddSeparator(2);
            AxeSettingsMenu.AddLabel("Default mode only tries to catch axe if distance to from player to axe is less than 250.\nBrutal catches all axes within range of desired catch radius.\n" +
                                     "Yorik mode catches axes around player insead of catching axes inside circle around your mouse");
            AxeSettingsMenu.AddSeparator(5);

            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.AxeCatchRange", new Slider("Axe Catch Range", 450, 200, 1000));
            AxeSettingsMenu.AddSeparator(2);

            var label = AxeSettingsMenu.Add("YorikMode",
                new Label(
                    "This sets the range around your player within you will catch the axe.\nDon't set this too high."));

            label.IsVisible = axeMode.CurrentValue == 2;

            axeMode.OnValueChange += (sender, args) =>
            {
                label.IsVisible = args.NewValue == 2;
            };

            AxeSettingsMenu.AddSeparator();

            AxeSettingsMenu.AddLabel("Additional settings :");
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesUnderTower",
                new CheckBox("Catch Axes that are under enemy tower", false));
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesNearEnemies",
                new CheckBox("Catch Axes that are near enemies", false));

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Draven addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.EnableLCIfNoEn",
                new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.ScanRange",
                new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.AllowedEnemies",
                new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Spinning Axe (Q) settings :");
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.MinManaQ",
                new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Blood Rush (W) settings :");
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.UseWInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.UseWInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.MinManaW",
                new Slider("Min mana percentage ({0}%) to use W", 75, 1));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Draven addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Draven.MiscMenu.EnableInterrupter", new CheckBox("Enable Interrupter"));
            MiscMenu.Add("Plugins.Draven.MiscMenu.EnableAntiGapcloser", new CheckBox("Enable Anti-Gapcloser"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Draven addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Spinning Axe (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxes", new CheckBox("Draw Axes"));
            DrawingsMenu.AddSeparator(1);
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesTimer", new CheckBox("Draw Axes timer"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesCatchRange", new CheckBox("Draw Axe's catch range"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesCatchRangeColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[1].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Stand Aside (E) drawing settings :");
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawEColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[0].Initialize(Color.Aquamarine);
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Draven.ComboMenu.UseQ"];

                public static int MaxAxesAmount => MenuManager.MenuValues["Plugins.Draven.ComboMenu.MaxAxesAmount", true];

                public static bool UseW => MenuManager.MenuValues["Plugins.Draven.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Draven.ComboMenu.UseE"];

                public static bool UseR => MenuManager.MenuValues["Plugins.Draven.ComboMenu.UseR"];

                public static bool RKeybind => MenuManager.MenuValues["Plugins.Draven.ComboMenu.RKeybind"];

                public static int RRangeKeybind => MenuManager.MenuValues["Plugins.Draven.ComboMenu.RRangeKeybind", true];
            }

            internal static class Axe
            {
                public static bool CatchAxes => MenuManager.MenuValues["Plugins.Draven.AxeSettingsMenu.CatchAxes"];

                public static bool UseWToCatch => MenuManager.MenuValues["Plugins.Draven.AxeSettingsMenu.UseWToCatch"];
                
                /// <summary>
                /// 0 - Default
                /// 1 - Brutal
                /// 2 - Yorik
                /// </summary>
                public static int CatchAxesMode => MenuManager.MenuValues["Plugins.Draven.AxeSettingsMenu.CatchAxesMode", true];

                public static int AxeCatchRange => MenuManager.MenuValues["Plugins.Draven.AxeSettingsMenu.AxeCatchRange", true];

                public static bool CatchAxesUnderTower => MenuManager.MenuValues["Plugins.Draven.AxeSettingsMenu.CatchAxesUnderTower"];

                public static bool CatchAxesNearEnemies => MenuManager.MenuValues["Plugins.Draven.AxeSettingsMenu.CatchAxesNearEnemies"];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.MinManaQ", true];

                public static bool UseWInLaneClear => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.UseWInLaneClear"];

                public static bool UseWInJungleClear => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.UseWInJungleClear"];

                public static int MinManaW => MenuManager.MenuValues["Plugins.Draven.LaneClearMenu.MinManaW", true];
            }

            internal static class Misc
            {
                public static bool EnableInterrupter => MenuManager.MenuValues["Plugins.Draven.MiscMenu.EnableInterrupter"];

                public static bool EnableAntiGapcloser => MenuManager.MenuValues["Plugins.Draven.MiscMenu.EnableAntiGapcloser"];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Draven.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawAxes => MenuManager.MenuValues["Plugins.Draven.DrawingsMenu.DrawAxes"];

                public static bool DrawAxesTimer => MenuManager.MenuValues["Plugins.Draven.DrawingsMenu.DrawAxesTimer"];

                public static bool DrawAxesCatchRange => MenuManager.MenuValues["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"];

                public static bool DrawE => MenuManager.MenuValues["Plugins.Draven.DrawingsMenu.DrawE"];
            }
        }
    }
}