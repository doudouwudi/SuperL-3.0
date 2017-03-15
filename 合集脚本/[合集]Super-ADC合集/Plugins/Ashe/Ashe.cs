#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Ashe.cs" company="EloBuddy">
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
using EloBuddy.SDK.Spells;
using Marksman_Master.Cache.Modules;
using Marksman_Master.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using ColorPicker = Marksman_Master.Utils.ColorPicker;

namespace Marksman_Master.Plugins.Ashe
{
    internal class Ashe : ChampionPlugin
    {
        protected static Spell.Active Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;
        private static bool _changingRangeScan;
        protected static bool IsPreAttack { get; private set; }
        protected static bool IsAfterAttack { get; private set; }

        private static CustomCache<int, PredictionResult> CachedWPrediction { get; set; }

        static Ashe()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1250, SkillShotType.Cone)
            {
                AllowedCollisionCount = 0,
                CastDelay = 250,
                ConeAngleDegrees = (int) (Math.PI/180*40),
                Speed = 1500,
                Range = 1250,
                Width = 20
            };
            E = new Spell.Skillshot(SpellSlot.E, 30000, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 30000, SkillShotType.Linear, 250, 1600, 130)
            {
                AllowedCollisionCount = 0
            };

            CachedWPrediction = StaticCacheProvider.Cache.Resolve<CustomCache<int, PredictionResult>>();
            CachedWPrediction.RefreshRate = 500;

            ColorPicker = new ColorPicker[2];

            ColorPicker[0] = new ColorPicker("AsheW", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("AsheR", new ColorBGRA(177, 67, 191, 255));

            ChampionTracker.Initialize(ChampionTrackerFlags.VisibilityTracker);

            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
            {
                if(sender.IsMe && args.Slot == SpellSlot.Q)
                    Orbwalker.ResetAutoAttack();
            };

            Orbwalker.OnPreAttack += (a,b) => IsPreAttack = true;
            Orbwalker.OnPostAttack += (a, b) => { IsPreAttack = false; IsAfterAttack = true; };
            Game.OnPostTick += args => IsAfterAttack = false;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Ashe.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[0].Color, W.Range, Player.Instance);

            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[1].Color, Settings.Combo.RMaximumRange, Player.Instance);
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!R.IsReady() || (args.DangerLevel != DangerLevel.Medium && args.DangerLevel != DangerLevel.High) ||
                !(Player.Instance.Mana > 200) || !sender.IsValidTargetCached(Settings.Misc.MaxInterrupterRange))
                return;

            var rPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
            {
                CollisionTypes = new HashSet<CollisionType> { Prediction.Manager.PredictionSelected == "ICPrediction" ? CollisionType.AiHeroClient : CollisionType.ObjAiMinion },
                Delay = .25f,
                From = Player.Instance.Position,
                Radius = 130,
                Range = Settings.Combo.RMaximumRange,
                RangeCheckFrom = Player.Instance.Position,
                Speed = R.Speed,
                Target = sender,
                Type = SkillShotType.Linear
            });

            if (rPrediction.HitChance < HitChance.High)
                return;

            R.Cast(rPrediction.CastPosition);
            
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (R.IsReady() && (args.End.DistanceCached(Player.Instance.Position) < 400))
            {
                R.CastMinimumHitchance(sender, 65);
            }
        }

        public static PredictionResult GetWPrediction(Obj_AI_Base unit)
        {
            if (MenuManager.IsCacheEnabled && CachedWPrediction.Exist(unit.NetworkId))
            {
                return CachedWPrediction.Get(unit.NetworkId);
            }

            var poly = new Geometry.Polygon.Sector(Player.Instance.Position, Game.CursorPos,
                (float) (Math.PI/180*40), 1250, 9).Points.ToArray();

            for (var i = 1; i < 10; i++)
            {
                var qPred = Prediction.Position.PredictLinearMissile(unit, 1250, 20, 250, 1500, 0,
                    Player.Instance.Position.Extend(poly[i], 20).To3D());

                if (qPred.CollisionObjects.Any() || (qPred.HitChance < HitChance.High))
                    continue;

                if (MenuManager.IsCacheEnabled)
                {
                    CachedWPrediction.Add(unit.NetworkId, qPred);
                }

                return qPred;
            }
            return null;
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("寒冰 连招 设置");

            ComboMenu.AddLabel("Q设置 :");
            ComboMenu.Add("Plugins.Ashe.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("W设置 :");
            ComboMenu.Add("Plugins.Ashe.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("E设置 :");
            ComboMenu.Add("Plugins.Ashe.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("R设置 :");
            ComboMenu.Add("Plugins.Ashe.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Ashe.ComboMenu.RMinimumRange", new Slider("R最小射程", 350, 100, 700));
            ComboMenu.Add("Plugins.Ashe.ComboMenu.RMaximumRange", new Slider("R最大射程", 2500, 700, 3000));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("寒冰 骚扰 设置");

            HarassMenu.AddLabel("W设置 :");
            HarassMenu.Add("Plugins.Ashe.HarassMenu.UseW", new CheckBox("Use W"));
            HarassMenu.Add("Plugins.Ashe.HarassMenu.MinManaForW", new Slider("最小蓝 百分比 ({0}%) 使用W", 60, 1));
            HarassMenu.AddSeparator(5);

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("寒冰 清线 设置");

            LaneClearMenu.AddLabel("基本设置 :");
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.EnableLCIfNoEn", new CheckBox("只有附近没有敌人才能启用清线"));
            var scanRange = LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.ScanRange", new Slider("扫描敌人范围", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.AllowedEnemies", new Slider("敌人数量", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Q设置 :");
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.MinManaQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("W设置 :");
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.UseWInLaneClear", new CheckBox("Use W in Lane Clear"));
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.UseWInJungleClear", new CheckBox("Use W in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.MinManaW", new Slider("最小蓝 百分比 ({0}%) 使用W", 80, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("寒冰 其他 设置");
            MiscMenu.Add("Plugins.Ashe.MiscMenu.MaxInterrupterRange",
                new Slider("R中断技能释放最大范围", 1500, 0, 2500));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawing settings for Ashe addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawW", new CheckBox("Draw W range"));
            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawWColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) => {
                    if (!b.NewValue)
                        return;

                    ColorPicker[0].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };

            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawRColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) => {
                    if (!b.NewValue)
                        return;

                    ColorPicker[1].Initialize(Color.Aquamarine);
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Ashe.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Ashe.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Ashe.ComboMenu.UseE"];

                public static bool UseR => MenuManager.MenuValues["Plugins.Ashe.ComboMenu.UseR"];

                public static int RMinimumRange => MenuManager.MenuValues["Plugins.Ashe.ComboMenu.RMinimumRange", true];

                public static int RMaximumRange => MenuManager.MenuValues["Plugins.Ashe.ComboMenu.RMaximumRange", true];
            }

            internal static class Harass
            {
                public static bool UseW => MenuManager.MenuValues["Plugins.Ashe.HarassMenu.UseW"];

                public static int MinManaForW => MenuManager.MenuValues["Plugins.Ashe.HarassMenu.MinManaForW", true];
            }
            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.MinManaQ", true];

                public static bool UseWInLaneClear => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.UseWInLaneClear"];

                public static bool UseWInJungleClear => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.UseWInJungleClear"];

                public static int MinManaW => MenuManager.MenuValues["Plugins.Ashe.LaneClearMenu.MinManaW", true];
            }

            internal static class Misc
            {
                public static int MaxInterrupterRange => MenuManager.MenuValues["Plugins.Ashe.MiscMenu.MaxInterrupterRange", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Ashe.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawW => MenuManager.MenuValues["Plugins.Ashe.DrawingsMenu.DrawW"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.Ashe.DrawingsMenu.DrawR"];
            }
        }
    }
}