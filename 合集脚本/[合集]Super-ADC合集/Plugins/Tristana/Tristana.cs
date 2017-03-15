#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Tristana.cs" company="EloBuddy">
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
using Marksman_Master.Utils;
using Color = SharpDX.Color;


namespace Marksman_Master.Plugins.Tristana
{
    internal class Tristana : ChampionPlugin
    {
        protected static Spell.Active Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Targeted E { get; }
        protected static Spell.Targeted R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;
        private static bool _changingRangeScan;
        private static readonly Text Text;

        protected static bool IsPreAttack { get; private set; }
        protected static bool IsCatingW {get; set; }
        protected static Vector3 WStartPos { get; set; }

        protected static Cache.Cache Cache => StaticCacheProvider.Cache;

        protected static CustomCache<int, float> ComboDamages { get; }

        protected static bool HasExplosiveChargeBuff(Obj_AI_Base unit) => unit.Buffs.Any(x => x.Name.Equals("tristanaechargesound", StringComparison.CurrentCultureIgnoreCase));

        protected static int CountEStacks(Obj_AI_Base unit) => unit.Buffs.Any(x => x.Name.Equals("tristanaecharge", StringComparison.CurrentCultureIgnoreCase)) ? unit.Buffs.First(x => x.Name.Equals("tristanaecharge", StringComparison.CurrentCultureIgnoreCase)).Count : 0;
        
        protected static BuffInstance GetExplosiveChargeBuff(Obj_AI_Base unit) => unit.Buffs.FirstOrDefault(x => x.Name.Equals("tristanaecharge", StringComparison.CurrentCultureIgnoreCase));
        protected static AIHeroClient WTarget { get; set; }

        private static AIHeroClient Wtarg { get; set; }
        private static bool Checkw { get; set; }

        static Tristana()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 400, 1400, 150);
            E = new Spell.Targeted(SpellSlot.E, 600);
            R = new Spell.Targeted(SpellSlot.R, 600);

            ColorPicker = new ColorPicker[2];

            ColorPicker[0] = new ColorPicker("TristanaW", new ColorBGRA(243, 109, 160, 255));
            ColorPicker[1] = new ColorPicker("TristanaHpBar", new ColorBGRA(255, 134, 0, 255));
            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));
            
            ComboDamages = Cache.Resolve<CustomCache<int, float>>(1000);

            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;

            Orbwalker.OnPostAttack += (sender, args) =>
            {
                IsPreAttack = false;

                if (!W.IsReady() || !Settings.Combo.UseW || !Settings.Combo.DoubleWKeybind)
                    return;

                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => x.IsValidTarget(W.Range) && HasExplosiveChargeBuff(x) && (CountEStacks(x) == 2));

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if ((target == null) || target.Position.IsVectorUnderEnemyTower())
                    return;
                
                    var buff = target.Buffs.Find(x => x.Name.ToLowerInvariant() == "tristanaechargesound").EndTime;

                if (buff - Game.Time < Player.Instance.Distance(target)/1300 + 0.5)
                    return;

                var wPrediction = W.GetPrediction(target);

                if (wPrediction.HitChance < HitChance.Medium)
                    return;

                Wtarg = target;
                Checkw = true;

                W.Cast(wPrediction.CastPosition);
                Core.DelayAction(() => WTarget = null, 3000);
            };
            
            DamageIndicator.Initalize(ColorPicker[1].Color, 1300);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ChampionTracker.Initialize(ChampionTrackerFlags.PostBasicAttackTracker);
            ChampionTracker.OnPostBasicAttack += (sender, args) =>
            {
                var target = args.Target as AIHeroClient;

                if ((target != null) && args.Sender.IsMe && (CountEStacks(target) == 2) && (target.TotalHealthWithShields() <= Damage.GetEPhysicalDamage(target, 3) + Damage.GetRDamage(target)))
                {
                    R.Cast(target);
                }
            };

            ColorPicker[1].OnColorChange += (a, b) => { DamageIndicator.Color = b.Color; };

            GameObject.OnCreate += GameObject_OnCreate;
            Messages.OnMessage += Messages_OnMessage;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!Checkw)
                return;

            var particle = sender as Obj_GeneralParticleEmitter;

            if ((particle == null) ||
                !particle.Name.Equals("Tristana_Base_W_launch.troy", StringComparison.CurrentCultureIgnoreCase))
                return;

            WTarget = Wtarg;
            Wtarg = null;
            Checkw = false;
        }

        private static void Messages_OnMessage(Messages.WindowMessage args)
        {
            if(Keybind?.Keys == null)
                return;

            if (args.Message == WindowMessages.KeyDown)
            {
                if ((args.Handle.WParam == Keybind.Keys.Item1) || (args.Handle.WParam == Keybind.Keys.Item2))
                {
                    Orbwalker.ActiveModesFlags |= Orbwalker.ActiveModes.Combo;
                }
            }

            if (args.Message != WindowMessages.KeyUp)
                return;

            if ((args.Handle.WParam == Keybind.Keys.Item1) || (args.Handle.WParam == Keybind.Keys.Item2))
            {
                Orbwalker.ActiveModesFlags = Orbwalker.ActiveModes.None;
            }
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            IsPreAttack = true;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Settings.Combo.FocusE)
            {
                var hero = args.Target as AIHeroClient;

                if (((hero != null) && (hero.TotalHealthWithShields() >= GetComboDamage(hero))) || ((TargetSelector.SelectedTarget != null) && TargetSelector.SelectedEnabled))
                {
                    foreach (var enemy in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x => x.IsValidTarget(Player.Instance.GetAutoAttackRange()) && (TargetSelector.GetPriority(hero) - TargetSelector.GetPriority(x) < 2) && HasExplosiveChargeBuff(x))
                        .OrderByDescending(CountEStacks))
                    {
                        Orbwalker.ForcedTarget = enemy;
                        return;
                    }
                }
            }
            
            if (!Settings.Combo.FocusE || !StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(
                         x => x.IsValidTarget(Player.Instance.GetAutoAttackRange())) || (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && (Orbwalker.ForcedTarget?.GetType() != typeof(AIHeroClient))))
            {
                Orbwalker.ForcedTarget = null;
            }

            if (!Settings.LaneClear.UseEOnTowers || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || !E.IsReady())
                return;

            if ((args.Target.GetType() != typeof (Obj_AI_Turret)) || !args.Target.IsValidTargetCached() ||
                (args.Target.Health < Player.Instance.GetAutoAttackDamageCached(args.Target as Obj_AI_Base)*2.5f))
                return;

            if (Q.IsReady())
            {
                Q.Cast();
                return;
            }

            E.Cast(args.Target as Obj_AI_Base);
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawInfo)
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

            if (R.IsReady() && unit.IsValidTarget(R.Range))
                damage += Damage.GetRDamage(unit);
            if (HasExplosiveChargeBuff(unit))
                damage += Damage.GetEPhysicalDamage(unit);

            if (unit.IsValidTarget(Player.Instance.GetAutoAttackRange()))
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
                    LaneClearMenu["Plugins.Tristana.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[0].Color, W.Range, Player.Instance);

            if (!Settings.Drawings.DrawInfo)
                return;

            foreach (var source in EntityManager.Heroes.Enemies.Where(x => x.IsVisible && x.IsHPBarRendered && x.Position.IsOnScreen() && HasExplosiveChargeBuff(x)))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30; // tracker friendly.
                var timeLeft = source.Buffs.Find(x => x.Name.Equals("tristanaechargesound", StringComparison.CurrentCultureIgnoreCase)).EndTime - Game.Time;
                var endPos = timeLeft * 0x3e8 / 0x25;

                var degree = Misc.GetNumberInRangeFromProcent(timeLeft * 1000d / 4000d * 100d, 3, 110);
                var color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();

                Text.X = (int)(hpPosition.X + endPos);
                Text.Y = (int)hpPosition.Y + 15; // + text size 
                Text.Color = color;
                Text.TextValue = timeLeft.ToString("F1");
                Text.Draw();

                var percentDamage = Math.Min(100, Damage.GetEPhysicalDamage(source) / source.TotalHealthWithShields() * 100);

                Text.X = (int)(hpPosition.X - 50);
                Text.Y = (int)source.HPBarPosition.Y;
                Text.Color = new Misc.HsvColor(Misc.GetNumberInRangeFromProcent(percentDamage, 3, 110), 1, 1).ColorFromHsv();
                Text.TextValue = percentDamage.ToString("F1");
                Text.Draw();

                Drawing.DrawLine(hpPosition.X + endPos, hpPosition.Y, hpPosition.X, hpPosition.Y, 1, color);
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!R.IsReady() || !sender.IsValidTarget(R.Range) || !Settings.Combo.UseRVsInterruptible)
                return;

            if (args.Delay == 0)
                R.Cast(sender);
            else Core.DelayAction(() => R.Cast(sender), args.Delay);
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (Settings.Combo.UseWVsGapclosers && W.IsReady() && (args.End.Distance(Player.Instance) < 350))
            {
                var pos =
                    SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 880, 1200, 400)
                        .Where(x => x.Value <= 1)
                        .Select(x => x.Key)
                        .ToList();
                if (pos.Any())
                {
                    var position =
                        Player.Instance.Position.Extend(Misc.SortVectorsByDistanceDescending(pos, args.End.To2D())[0],
                            880).To3D();

                    if (!position.IsVectorUnderEnemyTower() &&
                        (position.CountEnemiesInRangeCached(500) < Player.Instance.CountEnemiesInRangeCached(500)))
                    {
                        W.Cast();
                    }
                }
            }

            if (!Settings.Combo.UseRVsGapclosers || !R.IsReady() || !sender.IsValidTarget(R.Range) || (args.End.Distance(Player.Instance) > 350))
                return;

            if (args.Delay == 0)
                R.Cast(sender);
            else Core.DelayAction(() => R.Cast(sender), args.Delay);
        }

        private static KeyBind Keybind { get; set; }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("小炮 连招 设置");

            ComboMenu.AddLabel("Q 设置：");
            ComboMenu.Add("Plugins.Tristana.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("W 设置");
            ComboMenu.Add("Plugins.Tristana.ComboMenu.UseW", new CheckBox("Use W", false));
            ComboMenu.AddLabel("如果使用 W E R 可以击杀才使用W");
            ComboMenu.AddSeparator(2);
            ComboMenu.Add("Plugins.Tristana.ComboMenu.UseWVsGapclosers", new CheckBox("使用W反突进"));
            Keybind = ComboMenu.Add("Plugins.Tristana.ComboMenu.DoubleWKeybind",
                new KeyBind("使用 双W 连招", false, KeyBind.BindTypes.HoldActive, 'A'));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("E 设置");
            ComboMenu.Add("Plugins.Tristana.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(2);
            ComboMenu.Add("Plugins.Tristana.ComboMenu.FocusE", new CheckBox("集火攻击E目标"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("敌方英雄：");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                ComboMenu.Add("Plugins.Tristana.ComboMenu.UseEOn."+enemy.Hero, new CheckBox(enemy.Hero == Champion.MonkeyKing ? "Wukong" : enemy.ChampionName));
            }

            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("R 设置:");
            ComboMenu.Add("Plugins.Tristana.ComboMenu.UseR", new CheckBox("使用 R 抢人头"));
            ComboMenu.Add("Plugins.Tristana.ComboMenu.UseRVsMelees", new CheckBox("使用 R 对近战保持距离"));
            ComboMenu.Add("Plugins.Tristana.ComboMenu.UseRVsInterruptible", new CheckBox("使用 R 打断技能"));
            ComboMenu.Add("Plugins.Tristana.ComboMenu.UseRVsGapclosers", new CheckBox("用 R 反突进"));
            ComboMenu.AddSeparator(5);

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("小炮 清线 设置");

            LaneClearMenu.AddLabel("基本设置 :");
            LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.EnableLCIfNoEn", new CheckBox("开启 在附近没有敌人才使用清兵"));
            var scanRange = LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.ScanRange", new Slider("扫描敌人范围", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.AllowedEnemies", new Slider("敌人数量", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Q 设置:");
            LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("E 设置 :");
            LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.UseEInLaneClear", new CheckBox("Use E in Lane Clear"));
            LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.UseEInJungleClear", new CheckBox("Use E in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.MinManaE", new Slider("蓝量高于 ({0}%) 使用 E 清线", 80, 1));
			LaneClearMenu.Add("Plugins.Tristana.LaneClearMenu.UseEOnTowers", new CheckBox("使用 Q E 拆塔"));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("小炮 线圈 设置值");

            DrawingsMenu.AddLabel("基本设置 :");
            DrawingsMenu.Add("Plugins.Tristana.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("只有在技能冷却"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("W 设置 :");
            DrawingsMenu.Add("Plugins.Tristana.DrawingsMenu.DrawW", new CheckBox("Draw W range"));
            DrawingsMenu.Add("Plugins.Tristana.DrawingsMenu.DrawWColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.Add("Plugins.Tristana.DrawingsMenu.DrawInfo", new CheckBox("Draw Infos")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Tristana.DrawingsMenu.InfoColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddLabel("显示 伤害 和 Buff 持续时间");
        }

        protected override void PermaActive()
        {
            E.Range = (uint)(630 + 7 * Player.Instance.Level);
            R.Range = (uint)(630 + 7 * Player.Instance.Level);

            if ((Orbwalker.ForcedTarget != null) && !Orbwalker.ForcedTarget.IsValidTarget(Player.Instance.GetAutoAttackRange()))
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseW"];

                public static bool UseWVsGapclosers => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseWVsGapclosers"];

                public static bool DoubleWKeybind => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.DoubleWKeybind"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseE"];

                public static bool FocusE => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.FocusE"];

                public static bool UseR => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseR"];

                public static bool UseRVsMelees => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseRVsMelees"];

                public static bool UseRVsInterruptible => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseRVsInterruptible"];

                public static bool UseRVsGapclosers => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseRVsGapclosers"];

                public static bool IsEnabledFor(AIHeroClient unit) => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseEOn." + unit.Hero];

                public static bool IsEnabledFor(string championName) => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseEOn." + championName];

                public static bool IsEnabledFor(Champion championName) => MenuManager.MenuValues["Plugins.Tristana.ComboMenu.UseEOn." + championName];
            }
            
            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.UseQInJungleClear"]; 

                public static bool UseEInLaneClear => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.UseEInLaneClear"];

                public static bool UseEInJungleClear => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.UseEInJungleClear"];

                public static bool UseEOnTowers => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.UseEOnTowers"];

                public static int MinManaE => MenuManager.MenuValues["Plugins.Tristana.LaneClearMenu.MinManaE", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Tristana.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawW => MenuManager.MenuValues["Plugins.Tristana.DrawingsMenu.DrawW"];

                public static bool DrawInfo => MenuManager.MenuValues["Plugins.Tristana.DrawingsMenu.DrawInfo"];
            }
        }

        protected static class Damage
        {
            public static int[] EMagicDamage { get; } = {0, 50, 75, 100, 125, 150};
            public static float EMagicDamageApMod { get; } = 0.25f;
            public static int[] EPhysicalDamage { get; } = {0, 60, 70, 80, 90, 100};
            public static float[] EPhysicalDamageBonusAdMod { get; } = {0, 0.5f, 0.65f, 0.8f, 0.95f, 1.1f};
            public static float EPhysicalDamageBonusApMod { get; } = 0.5f;
            public static int[] EDamagePerStack { get; } = {0, 18, 21, 24, 27, 30};
            public static float[] EDamagePerStackBonusAdMod { get; } = {0, 0.15f, 0.195f, 0.24f, 0.285f, 0.33f};
            public static float EDamagePerStackBonusApMod { get; } = 0.15f;
            public static int[] RDamage { get; } = {0, 300, 400, 500};

            private static CustomCache<KeyValuePair<int, int>, float> GetEPhysicalDamages => Cache.Resolve<CustomCache<KeyValuePair<int, int>, float>>();
            private static CustomCache<int, bool> IsKillableFromR => Cache.Resolve<CustomCache<int, bool>>();
            private static CustomCache<int, float> RDamageCached => Cache.Resolve<CustomCache<int, float>>();

            public static float GetEMagicDamage(Obj_AI_Base unit)
            {
                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, EMagicDamage[E.Level] + Player.Instance.FlatMagicDamageMod * EMagicDamageApMod);
            }

            public static float GetEPhysicalDamage(Obj_AI_Base unit, int customStacks = -1)
            {
                if (MenuManager.IsCacheEnabled && GetEPhysicalDamages.Exist(new KeyValuePair<int, int>(unit.NetworkId, customStacks)))
                {
                    return GetEPhysicalDamages.Get(new KeyValuePair<int, int>(unit.NetworkId, customStacks));
                }

                var rawDamage = (EPhysicalDamage[E.Level] +
                                 (Player.Instance.FlatPhysicalDamageMod*EPhysicalDamageBonusAdMod[E.Level] +
                                  Player.Instance.FlatMagicDamageMod*EPhysicalDamageBonusApMod))
                                +
                                (EDamagePerStack[E.Level] +
                                 (Player.Instance.FlatPhysicalDamageMod*EDamagePerStackBonusAdMod[E.Level] +
                                  Player.Instance.FlatMagicDamageMod*EDamagePerStackBonusApMod))*
                                (customStacks > 0
                                    ? customStacks
                                    : (unit.Buffs.Any(x => x.Name.Equals("tristanaecharge", StringComparison.CurrentCultureIgnoreCase))
                                        ? unit.Buffs.Find(x => x.Name.Equals("tristanaecharge", StringComparison.CurrentCultureIgnoreCase)).Count
                                        : 0));

                var damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, rawDamage);

                if (MenuManager.IsCacheEnabled)
                {
                    GetEPhysicalDamages.Add(new KeyValuePair<int, int>(unit.NetworkId, customStacks), damage);
                }
                return damage;
            }

            public static float GetRDamage(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && RDamageCached.Exist(unit.NetworkId))
                {
                    return RDamageCached.Get(unit.NetworkId);
                }

                var damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical,
                    RDamage[R.Level] + Player.Instance.TotalMagicalDamage);

                if (MenuManager.IsCacheEnabled)
                {
                    RDamageCached.Add(unit.NetworkId, damage);
                }
                return damage;
            }

            public static bool IsTargetKillableFromR(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && IsKillableFromR.Exist(unit.NetworkId))
                {
                    return IsKillableFromR.Get(unit.NetworkId);
                }

                bool isKillable;

                if (unit.GetType() != typeof(AIHeroClient))
                {
                    isKillable = unit.TotalHealthWithShields(true) <= GetRDamage(unit);

                    if (MenuManager.IsCacheEnabled)
                    {
                        IsKillableFromR.Add(unit.NetworkId, isKillable);
                    }
                    return isKillable;
                }

                var enemy = (AIHeroClient)unit;

                if (enemy.HasSpellShield() || enemy.HasUndyingBuffA())
                {
                    if (MenuManager.IsCacheEnabled)
                    {
                        IsKillableFromR.Add(unit.NetworkId, false);
                    }
                    return false;
                }

                if (enemy.ChampionName != "Blitzcrank")
                {
                    isKillable = enemy.TotalHealthWithShields(true) < GetRDamage(enemy);

                    if (MenuManager.IsCacheEnabled)
                    {
                        IsKillableFromR.Add(unit.NetworkId, isKillable);
                    }
                    return isKillable;
                }

                if (!enemy.HasBuff("BlitzcrankManaBarrierCD") && !enemy.HasBuff("ManaBarrier"))
                {
                    isKillable = enemy.TotalHealthWithShields(true) + enemy.Mana / 2 < GetRDamage(enemy);

                    if (MenuManager.IsCacheEnabled)
                    {
                        IsKillableFromR.Add(unit.NetworkId, isKillable);
                    }
                    return isKillable;
                }

                isKillable = enemy.TotalHealthWithShields(true) < GetRDamage(enemy);

                if (MenuManager.IsCacheEnabled)
                {
                    IsKillableFromR.Add(unit.NetworkId, isKillable);
                }
                return isKillable;
            }
        }
    }
}