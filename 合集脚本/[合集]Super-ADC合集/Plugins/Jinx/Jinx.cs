#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Jinx.cs" company="EloBuddy">
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
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Marksman_Master.Cache.Modules;
using Marksman_Master.Utils;
using SharpDX;
using Color = System.Drawing.Color;

namespace Marksman_Master.Plugins.Jinx
{
    internal class Jinx : ChampionPlugin
    {
        protected static Spell.Active Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu MiscMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;

        protected static int GetFirecanonStacks() =>
            !HasItemFirecanon ? 0 : Player.Instance.Buffs.Find(x => x.Name.ToLowerInvariant() == "itemstatikshankcharge").Count;

        protected static bool HasFirecanonStackedUp
            => Player.Instance.Buffs.Any(x => HasItemFirecanon && (x.Name.ToLowerInvariant() == "itemstatikshankcharge") && (x.Count == 100));

        protected static bool HasItemFirecanon
            => Player.Instance.InventoryItems.Any(x=>x.Id == ItemId.Rapid_Firecannon);

        protected static bool HasMinigun
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "jinxqicon");

        protected static int GetMinigunStacks
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "jinxqramp") ? Player.Instance.Buffs.Find(x => x.Name.ToLowerInvariant() == "jinxqramp").Count : 0;

        protected static bool HasRocketLauncher
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "jinxq");

        protected static float GetRealRocketLauncherRange()
        {
            var qRange = 700 + 25*(Q.Level - 1);
            var additionalRange = HasFirecanonStackedUp ? Math.Min(qRange*0.35f, 150) : 0;
            return qRange + additionalRange;
        }

        protected static float GetRealMinigunRange() => HasFirecanonStackedUp ? Math.Min(625 * 1.35f, 700 + 150) : 625;

        private static bool _changingRangeScan;
        private static bool _changingkeybindRange;

        protected static bool IsPreAttack { get; private set; }

        private static readonly Text Text;

        protected static Cache.Cache Cache => StaticCacheProvider.Cache;

        static Jinx()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1500, SkillShotType.Linear, 600, 3300, 60)
            {
                AllowedCollisionCount = 0
            };
            E = new Spell.Skillshot(SpellSlot.E, 900, SkillShotType.Circular, 950, int.MaxValue, 100);
            R = new Spell.Skillshot(SpellSlot.R, 30000, SkillShotType.Linear, 600, 1500, 140)
            {
                AllowedCollisionCount = -1
            };

            ColorPicker = new ColorPicker[2];

            ColorPicker[0] = new ColorPicker("JinxQ", new ColorBGRA(114, 171, 160, 255));
            ColorPicker[1] = new ColorPicker("JinxW", new ColorBGRA(255, 21, 95, 255));

            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Orbwalker.OnPostAttack += (target, args) => IsPreAttack = false;

            ChampionTracker.Initialize(ChampionTrackerFlags.LongCastTimeTracker);
            ChampionTracker.OnLongSpellCast += ChampionTracker_OnLongSpellCast;

            Spellbook.OnStopCast += (sender, args) =>
            {
                if (sender.IsMe && IsPreAttack)
                    IsPreAttack = false;
            };
        }

        private static void ChampionTracker_OnLongSpellCast(object sender, OnLongSpellCastEventArgs e)
        {
            if (!E.IsReady() || !Settings.Combo.AutoE)
                return;

            if (e.IsTeleport)
            {
                Core.DelayAction(() =>
                {
                    if (E.IsReady() && (e.EndPosition.DistanceCached(Player.Instance) <= E.Range))
                    {
                        E.Cast(e.EndPosition);
                    }
                }, 3500);
            }
            else if(e.Sender.IsValidTargetCached(E.Range))
            {
                E.Cast(e.Sender.ServerPosition);
            }
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            IsPreAttack = true;

            if ((Orbwalker.ForcedTarget != null) && !Orbwalker.ForcedTarget.IsValidTarget(Player.Instance.GetAutoAttackRange()))
                args.Process = false;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Jinx.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (_changingkeybindRange)
                Circle.Draw(SharpDX.Color.White, Settings.Combo.RRangeKeybind, Player.Instance);


            if (Settings.Drawings.DrawRocketsRange)
                Circle.Draw(HasRocketLauncher ? SharpDX.Color.AliceBlue.BGRAfromSharpDx() : ColorPicker[0].Color, HasRocketLauncher ? 625 : Q.Range, Player.Instance);

            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);

            if (!R.IsReady())
                return;

            foreach (var source in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                x => x.IsHPBarRendered && x.Position.IsOnScreen()))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30;
                var percentDamage = Math.Min(100, Damage.GetRDamage(source) / source.TotalHealthWithShields() * 100);

                Text.X = (int)(hpPosition.X - 50);
                Text.Y = (int)source.HPBarPosition.Y;
                Text.Color =
                    new Misc.HsvColor(Misc.GetNumberInRangeFromProcent(percentDamage, 3, 110), 1, 1).ColorFromHsv();
                Text.TextValue = percentDamage.ToString("F1") + "%";
                Text.Draw();
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!Settings.Misc.EnableInterrupter || !E.IsReady() || (args.End.Distance(Player.Instance) > 350) ||
                !sender.IsValidTarget(E.Range))
                return;

            if (args.Delay == 0)
            {
                E.Cast(E.GetPrediction(sender).CastPosition);
            }
            else Core.DelayAction(() => E.Cast(E.GetPrediction(sender).CastPosition), args.Delay);
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (!Settings.Misc.EnableAntiGapcloser || !E.IsReady() || (args.End.Distance(Player.Instance) > 350) ||
                !sender.IsValidTarget(E.Range))
                return;

            if (args.Delay == 0)
                E.Cast(args.End);
            else Core.DelayAction(() => E.Cast(args.End), args.Delay);
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("金克丝 连招 设置");

            ComboMenu.AddLabel("Q 设置 :");
            ComboMenu.Add("Plugins.Jinx.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("W 设置 :");
            ComboMenu.Add("Plugins.Jinx.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.Add("Plugins.Jinx.ComboMenu.WMinDistanceToTarget", new Slider("对目标最小距离", 800, 0, 1500));
            ComboMenu.AddLabel("使用W仅从玩家到目标的距离高于期望值");
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("E 设置 :");
            ComboMenu.Add("Plugins.Jinx.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.Add("Plugins.Jinx.ComboMenu.AutoE", new CheckBox("自动使用E打断技能"));
            ComboMenu.AddLabel("自动使用E 对敌人的传送 中亚.\n他也在寻找打断释放时间长的技能 像女警的R 和马尔扎哈的R");
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("R 设置 :");
            ComboMenu.Add("Plugins.Jinx.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Jinx.ComboMenu.RKeybind", new KeyBind("R 热键绑定", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddLabel("R的热键释放最好的目标是活跃范围内.");
            ComboMenu.AddSeparator(5);
            var keybindRange = ComboMenu.Add("Plugins.Jinx.ComboMenu.RRangeKeybind", new Slider("热键绑定的R 最大射程", 1100, 300, 5000));
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

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("金克丝 自动骚扰 设置");

            HarassMenu.AddLabel("Q 设置 :");
            HarassMenu.Add("Plugins.Jinx.HarassMenu.UseQ", new CheckBox("Use Q", false));
            HarassMenu.Add("Plugins.Jinx.HarassMenu.MinManaQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("W 设置 :");
            HarassMenu.Add("Plugins.Jinx.HarassMenu.UseW", new CheckBox("W自动骚扰"));
            HarassMenu.AddLabel("能自动W攻击敌人.");
            HarassMenu.Add("Plugins.Jinx.HarassMenu.MinManaW", new Slider("最小蓝 百分比 ({0}%) 使用W", 50, 1));
            HarassMenu.AddSeparator(5);
            HarassMenu.AddLabel("W 骚扰启用");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                HarassMenu.Add("Plugins.Jinx.HarassMenu.UseW." + enemy.Hero, new CheckBox(enemy.ChampionName == "MonkeyKing" ? "Wukong" : enemy.ChampionName));
            }

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("金克丝 清线 设置");

            LaneClearMenu.AddLabel("基本设置 :");
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.EnableLCIfNoEn",
                new CheckBox("只有附近没有敌人才能启用清线"));
            var scanRange = LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.ScanRange",
                new Slider("扫描敌人范围", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.AllowedEnemies", new Slider("敌人数量", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Switcheroo! (Q) settings :");
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.MinManaQ", new Slider("最小蓝 百分比 ({0}%) 使用Q", 50, 1));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("金克丝其他设置");
            MiscMenu.AddLabel("基本设置:");
            MiscMenu.Add("Plugins.Jinx.MiscMenu.EnableInterrupter", new CheckBox("使用E对可中断的技能", false));
            MiscMenu.Add("Plugins.Jinx.MiscMenu.EnableAntiGapcloser", new CheckBox("使用E反突进"));
            MiscMenu.Add("Plugins.Jinx.MiscMenu.WKillsteal", new CheckBox("使用W抢人头"));
            MiscMenu.Add("Plugins.Jinx.MiscMenu.RKillsteal", new CheckBox("使用R抢人头"));
            MiscMenu.Add("Plugins.Jinx.MiscMenu.RKillstealMaxRange", new Slider("使用R抢人头对敌人的最大范围", 8000, 0, 20000));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Jinx addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Switcheroo! (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawRocketsRange", new CheckBox("Draw Q rockets range"));
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawRocketsRangeColor", new CheckBox("Change Color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[0].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Zap! (W) drawing settings :");
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawW", new CheckBox("Draw W range"));
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawWColor", new CheckBox("Change Color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);
        }

        protected override void PermaActive()
        {
            Q.Range = (uint)GetRealRocketLauncherRange();

            if ((Orbwalker.ForcedTarget != null) && !Orbwalker.ForcedTarget.IsValidTarget(GetRealRocketLauncherRange()) && HasRocketLauncher)
            {
                Orbwalker.ForcedTarget = null;
            } else if ((Orbwalker.ForcedTarget != null) && !Orbwalker.ForcedTarget.IsValidTarget(GetRealMinigunRange()) && HasMinigun)
            {
                Orbwalker.ForcedTarget = null;
            }

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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Jinx.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Jinx.ComboMenu.UseW"];

                public static int WMinDistanceToTarget => MenuManager.MenuValues["Plugins.Jinx.ComboMenu.WMinDistanceToTarget", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Jinx.ComboMenu.UseE"];

                public static bool AutoE => MenuManager.MenuValues["Plugins.Jinx.ComboMenu.AutoE"];

                public static bool UseR => MenuManager.MenuValues["Plugins.Jinx.ComboMenu.UseR"];

                public static bool RKeybind => MenuManager.MenuValues["Plugins.Jinx.ComboMenu.RKeybind"];

                public static int RRangeKeybind => MenuManager.MenuValues["Plugins.Jinx.ComboMenu.RRangeKeybind", true];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Jinx.HarassMenu.UseQ"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Jinx.HarassMenu.MinManaQ", true];

                public static bool UseW => MenuManager.MenuValues["Plugins.Jinx.HarassMenu.UseW"];

                public static int MinManaW => MenuManager.MenuValues["Plugins.Jinx.HarassMenu.MinManaW", true];

                public static bool IsWHarassEnabledFor(AIHeroClient unit) => MenuManager.MenuValues["Plugins.Jinx.HarassMenu.UseW." + unit.Hero];

                public static bool IsWHarassEnabledFor(string championName) => MenuManager.MenuValues["Plugins.Jinx.HarassMenu.UseW." + championName];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Jinx.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Jinx.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Jinx.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Jinx.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Jinx.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Jinx.LaneClearMenu.MinManaQ", true];
            }

            internal static class Misc
            {
                public static bool EnableInterrupter => MenuManager.MenuValues["Plugins.Jinx.MiscMenu.EnableInterrupter"];

                public static bool EnableAntiGapcloser => MenuManager.MenuValues["Plugins.Jinx.MiscMenu.EnableAntiGapcloser"];

                public static bool WKillsteal => MenuManager.MenuValues["Plugins.Jinx.MiscMenu.WKillsteal"];

                public static bool RKillsteal => MenuManager.MenuValues["Plugins.Jinx.MiscMenu.RKillsteal"];

                public static int RKillstealMaxRange => MenuManager.MenuValues["Plugins.Jinx.MiscMenu.RKillstealMaxRange", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Jinx.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawRocketsRange => MenuManager.MenuValues["Plugins.Jinx.DrawingsMenu.DrawRocketsRange"];

                public static bool DrawW => MenuManager.MenuValues["Plugins.Jinx.DrawingsMenu.DrawW"];
            }
        }

        protected static class Damage
        {
            private static CustomCache<int, float> RDamages { get; } = Cache.Resolve<CustomCache<int, float>>(500);

            public static int[] RMinimalDamage { get; } = {0, 25, 35, 45};
            public static float RBonusAdDamageMod { get; } = 0.15f;
            public static float[] RMissingHealthBonusDamage { get; } = {0, 0.25f, 0.3f, 0.35f};

            public static float GetRDamage(Obj_AI_Base target, Vector3? customPosition = null)
            {
                if (MenuManager.IsCacheEnabled && RDamages.Exist(target.NetworkId))
                {
                    return RDamages.Get(target.NetworkId);
                }

                var distance = Player.Instance.DistanceCached(customPosition ?? target.Position) > 1500 ? 1499 : Player.Instance.DistanceCached(customPosition ?? target.Position);
                distance = distance < 100 ? 100 : distance;

                var baseDamage = Misc.GetNumberInRangeFromProcent(Misc.GetProcentFromNumberRange(distance, 100, 1505),
                    RMinimalDamage[R.Level],
                    RMinimalDamage[R.Level] * 10);
                var bonusAd = Misc.GetNumberInRangeFromProcent(Misc.GetProcentFromNumberRange(distance, 100, 1505),
                    RBonusAdDamageMod,
                    RBonusAdDamageMod * 10);
                var percentDamage = (target.MaxHealth - target.Health) * RMissingHealthBonusDamage[R.Level];

                var finalDamage = Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                    (float) (baseDamage + percentDamage + Player.Instance.FlatPhysicalDamageMod*bonusAd));

                if (MenuManager.IsCacheEnabled)
                {
                    RDamages.Add(target.NetworkId, finalDamage);
                }

                return finalDamage;
            }
        }
    }
}
