#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Kalista.cs" company="EloBuddy">
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
using EloBuddy.SDK.Rendering;
using Marksman_Master.Plugins.Kalista.Modes;
using Marksman_Master.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using FontStyle = System.Drawing.FontStyle;
using Marksman_Master.Cache.Modules;

namespace Marksman_Master.Plugins.Kalista
{
    internal class Kalista : ChampionPlugin
    {
        public static Spell.Skillshot Q { get; }
        public static Spell.Active W { get; private set; }
        public static Spell.Active E { get; }
        public static Spell.Active R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu JungleLaneClearMenu { get; set; }
        internal static Menu FleeMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }

        public static AIHeroClient SouldBoundAlliedHero { get; private set; }

        private static readonly Text Text;
        private static readonly ColorPicker[] ColorPicker;

        private static float LastECastTime { get; set; }

        protected static Cache.Cache Cache { get; }
        
        static Kalista()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1150, SkillShotType.Linear, 250, 2400, 40)
            {
                AllowedCollisionCount = 0
            };
            W = new Spell.Active(SpellSlot.W, 5500);
            E = new Spell.Active(SpellSlot.E, 1000);
            R = new Spell.Active(SpellSlot.R, 1150);

            Cache = StaticCacheProvider.Cache;
            
            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("KalistaQ", new ColorBGRA(243, 109, 160, 255));
            ColorPicker[1] = new ColorPicker("KalistaE", new ColorBGRA(255, 210, 54, 255));
            ColorPicker[2] = new ColorPicker("KalistaR", new ColorBGRA(1, 109, 160, 255));
            ColorPicker[3] = new ColorPicker("KalistaDamageIndicator", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(ColorPicker[3].Color, true, SharpDX.Color.Azure, (int)E.Range);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[3].OnColorChange += (a, b) => { DamageIndicator.Color = b.Color;};

            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Orbwalker.OnUnkillableMinion += Orbwalker_OnUnkillableMinion;
            Game.OnTick += Game_OnTick;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;

            WallJumper.Init();
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe)
                return;

            if (args.Slot != SpellSlot.E)
                return;

            if (Game.Time * 1000 - LastECastTime < 200)
            {
                args.Process = false;
            } else LastECastTime = Game.Time * 1000;
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            if (StaticCacheProvider.GetChampions(CachedEntityType.AllyHero).Count() < 2)
                return;

            if (SouldBoundAlliedHero == null)
            {
                var entity = StaticCacheProvider.GetChampions(CachedEntityType.AllyHero).ToList().Find(
                    unit => !unit.IsMe &&
                        unit.Buffs.Any(
                            n =>
                                n.Caster.IsMe &&
                                n.DisplayName.ToLowerInvariant() =="kalistapassivecoopstrike"));

                if (entity != null)
                {
                    var allies =
                        (from aiHeroClient in StaticCacheProvider.GetChampions(CachedEntityType.AllyHero).ToList()
                            where !aiHeroClient.IsMe
                            select aiHeroClient.Hero.ToString()).ToList();

                    MiscMenu["Plugins.Kalista.MiscMenu.SoulBoundHero"].Cast<ComboBox>().CurrentValue = allies.FindIndex(x=>x.Equals(entity.Hero.ToString()));

                    SouldBoundAlliedHero = entity;
                }
            }

            if (SouldBoundAlliedHero == null)
                return;

            if (R.IsReady() && Settings.Misc.SaveAlly && SouldBoundAlliedHero.HealthPercent < 15 && !SouldBoundAlliedHero.IsInShopRange() && IncomingDamage.GetIncomingDamage(SouldBoundAlliedHero) > SouldBoundAlliedHero.Health)
            {
                Misc.PrintInfoMessage("Saving <font color=\"#adff2f\">"+SouldBoundAlliedHero.Hero+"</font> from death.");
                R.Cast();
            }

            if (R.IsReady() && Settings.Misc.BlitzCombo &&
                SouldBoundAlliedHero.Position.DistanceCached(Player.Instance) > Player.Instance.GetAutoAttackRange() &&
                Player.Instance.CountEnemiesInRangeCached(1500) > 0)
            {
                switch (SouldBoundAlliedHero.Hero)
                {
                    case Champion.Blitzcrank:
                    {
                        var enemy =
                            StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).FirstOrDefault(
                                x =>
                                    x.Buffs.Any(
                                        buff =>
                                            buff.IsActive && buff.Name.ToLowerInvariant() == "rocketgrab2" &&
                                            buff.Caster.NetworkId == SouldBoundAlliedHero.NetworkId));

                        if (enemy != null && enemy.DistanceCached(Player.Instance) > 500)
                        {
                            if (Settings.Misc.BlitzComboKillable && enemy.Health < Damage.GetComboDamage(enemy, 8))
                            {
                                Misc.PrintInfoMessage("Doing Blitzcrank-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                            else
                            {
                                Misc.PrintInfoMessage("Doing Blitzcrank-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                        }
                    }
                        break;
                    case Champion.TahmKench:
                    {
                        var enemy =
                            StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).FirstOrDefault(
                                x =>
                                    x.Buffs.Any(
                                        buff =>
                                            buff.IsActive && buff.Name.ToLowerInvariant() == "tahmkenchwdevoured" &&
                                            buff.Caster.NetworkId == SouldBoundAlliedHero.NetworkId));

                        if (enemy != null && enemy.DistanceCached(Player.Instance) > 500)
                        {
                            if (Settings.Misc.BlitzComboKillable && enemy.Health < Damage.GetComboDamage(enemy, 8))
                            {
                                Misc.PrintInfoMessage("Doing Tahm Kench-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                            else
                            {
                                Misc.PrintInfoMessage("Doing Tahm Kench-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                        }
                    }
                        break;
                    case Champion.Skarner:
                    {
                        var enemy =
                            StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).FirstOrDefault(
                                x =>
                                    x.Buffs.Any(
                                        buff =>
                                            buff.IsActive && buff.Name.ToLowerInvariant() == "skarnerimpale" &&
                                            buff.Caster.NetworkId == SouldBoundAlliedHero.NetworkId));

                        if (enemy != null && enemy.DistanceCached(Player.Instance) > 500)
                        {
                            if (Settings.Misc.BlitzComboKillable && enemy.Health < Damage.GetComboDamage(enemy, 8))
                            {
                                Misc.PrintInfoMessage("Doing Skarner-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                            else
                            {
                                Misc.PrintInfoMessage("Doing Skarner-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                        }
                    }
                        break;
                }
            }
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!Q.IsReady() || !Settings.Combo.UseQ || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            var hero = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (hero == null || hero.IsDead || hero.HasSpellShield() ||  hero.HasUndyingBuffA())
                return;

            var prediction = Q.GetPrediction(hero);

            if (prediction.HitChancePercent >= 70)
            {
                Q.Cast(prediction.CastPosition);
            }
        }

        private static void Orbwalker_OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (!E.IsReady() || Player.Instance.ManaPercent < Settings.JungleLaneClear.MinManaForE ||
                !Settings.JungleLaneClear.UseE || !Settings.JungleLaneClear.UseEOnUnkillableMinions ||
                !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ||
                Player.Instance.ManaPercent < Settings.JungleLaneClear.MinManaForE)
                return;

            var aiMinion = target as Obj_AI_Minion;

            if (aiMinion == null || Prediction.Health.GetPrediction(aiMinion, 300) < 10)
                return;

            if (Damage.IsTargetKillableByRend(aiMinion))
            {
                E.Cast();
            }
        }

        public static float HandleDamageIndicator(Obj_AI_Base target)
        {
            if (!Settings.Drawings.DrawDamageIndicator || Player.Instance.IsDead)
                return 0f;

            if (target.GetType() != typeof(AIHeroClient))
                return Damage.GetRendDamageOnTarget(target);

            if(Settings.Drawings.DamageIndicatorMode == 0)
                return Damage.GetRendDamageOnTarget(target);

            var hero = (AIHeroClient) target;

            float damage = 0;

            damage += Damage.GetRendDamageOnTarget(hero);
            damage += Damage.GetComboDamage(hero, 0);

            return damage;
        }

        protected override void OnDraw()
        {
            if (Player.Instance.IsDead)
                return;

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[1].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);

            if (Settings.Flee.JumpWithQ && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                WallJumper.DrawSpots();
            }

            if (!Settings.Drawings.DrawDamageIndicator)
                return;
            
            foreach (
                var source in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsVisible && x.IsHPBarRendered && x.Position.IsOnScreen() && Damage.HasRendBuff(x)))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30; // tracker friendly.

                if (Damage.GetRendBuff(source) != null)
                {
                    var timeLeft = Damage.GetRendBuff(source).EndTime - Game.Time;
                    var endPos = timeLeft * 0x3e8 / 0x25;

                    var degree = Misc.GetNumberInRangeFromProcent(timeLeft * 1000d / 4000d * 100d, 3, 110);
                    var color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();

                    Text.X = (int)(hpPosition.X + endPos);
                    Text.Y = (int)hpPosition.Y + 15; // + text size 
                    Text.Color = color;
                    Text.TextValue = timeLeft.ToString("F1");
                    Text.Draw();
                    
                    Drawing.DrawLine(hpPosition.X + endPos, hpPosition.Y, hpPosition.X, hpPosition.Y, 1, color);
                }
                var percentDamage = Math.Min(100,
                    Damage.GetRendDamageOnTarget(source)/source.TotalHealthWithShields()*100);

                Text.X = (int) (hpPosition.X - 50);
                Text.Y = (int) source.HPBarPosition.Y;
                Text.Color =
                    new Misc.HsvColor(Misc.GetNumberInRangeFromProcent(percentDamage, 3, 110), 1, 1).ColorFromHsv();
                Text.TextValue = percentDamage.ToString("F1");
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
            ComboMenu.AddGroupLabel("Combo mode settings for Kalista addon");

            ComboMenu.AddLabel("Pierce (Q) settings :");
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Rend (E) settings :");
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseE", new CheckBox("Use E to execute"));
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEBeforeDeath", new CheckBox("Use E before death"));
            ComboMenu.AddSeparator(1);
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRange",
                new CheckBox("Use E before enemy leaves the range of E", false));
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRangeS",
                new Slider("Minimum percentage ({0}%) damage dealt", 50, 15, 99));
            ComboMenu.AddLabel(
                "Uses E to before enemy leaves range if Rend can deal desired percentage amount of his health.");
            ComboMenu.AddSeparator(1);

            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEToSlow", new CheckBox("Use E to slow"));
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEToSlowMinMinions",
                new Slider("Use E to slow min minions", 2, 1, 6));
            ComboMenu.AddLabel("Uses E to slow enemy when desired amout of minions can be killed using Rend.");
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Additional settings :");
            ComboMenu.Add("Plugins.Kalista.ComboMenu.JumpOnMinions", new CheckBox("Use minions to jump"));
            ComboMenu.AddLabel("Uses minions to jump when enemy is outside Kalista's auto attack range.");
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Kalista addon");
            HarassMenu.AddLabel("Pierce (Q) settings :");
            HarassMenu.Add("Plugins.Kalista.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Kalista.HarassMenu.MinManaForQ",
                new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Rend (E) settings :");
            HarassMenu.Add("Plugins.Kalista.HarassMenu.UseE", new CheckBox("Use E"));
            HarassMenu.Add("Plugins.Kalista.HarassMenu.UseEIfManaWillBeRestored",
                new CheckBox("Use E only if mana will be restored"));
            HarassMenu.Add("Plugins.Kalista.HarassMenu.MinManaForE",
                new Slider("Min mana percentage ({0}%) to use E", 50, 1));
            HarassMenu.Add("Plugins.Kalista.HarassMenu.MinStacksForE", new Slider("Min stacks to use E", 3, 2, 12));

            JungleLaneClearMenu = MenuManager.Menu.AddSubMenu("Jungle and Lane clear");
            JungleLaneClearMenu.AddGroupLabel("Jungle and Lane clear mode settings for Kalista addon");

            JungleLaneClearMenu.AddLabel("Pierce (Q) settings :");
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseQ", new CheckBox("Use Q"));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.MinManaForQ",
                new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.MinMinionsForQ",
                new Slider("Min minions killed to use Q", 3, 1, 6));
            JungleLaneClearMenu.AddSeparator(5);

            JungleLaneClearMenu.AddLabel("Rend (E) settings :");
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseE", new CheckBox("Use E"));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseEForUnkillable",
                new CheckBox("Use E to lasthit unkillable minions"));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.MinManaForE",
                new Slider("Min mana percentage ({0}%) to use E", 50, 1));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.MinMinionsForE",
                new Slider("Min minions killed to use E", 3, 1, 6));
            JungleLaneClearMenu.AddSeparator(5);

            JungleLaneClearMenu.AddLabel("Additional settings :");
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseEToStealBuffs",
                new CheckBox("Use E to steal buffs"));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseEToStealDragon",
                new CheckBox("Use E to steal Dragon / Baron"));

            FleeMenu = MenuManager.Menu.AddSubMenu("Flee");
            FleeMenu.AddGroupLabel("Flee mode settings for Kalista addon");
            FleeMenu.Add("Plugins.Kalista.FleeMenu.Jump", new CheckBox("Try to jump over walls using Q"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Kalista addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Pierce (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawQColor", new CheckBox("Change color", false))
                .OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[0].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Rend (E) drawing settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawEColor", new CheckBox("Change color", false))
                .OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[1].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Fate's Call (R) drawing settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawRColor", new CheckBox("Change color", false))
                .OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[2].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Damage indicator drawing settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawDamageIndicator",
                new CheckBox("Draw damage indicator on enemy HP bars")).OnValueChange += (a, b) =>
                {
                    if (b.NewValue)
                        DamageIndicator.DamageDelegate = HandleDamageIndicator;
                    else if (!b.NewValue)
                        DamageIndicator.DamageDelegate = null;
                };
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawDamageIndicatorColor",
                new CheckBox("Change color", false)).OnValueChange +=
                (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[3].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DamageIndicatorMode", new ComboBox("Damage indicator mode", 0, "Only E damage", "Combo damage"));


            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Kalista addon");
            MiscMenu.AddLabel("Soulbound settings : ");
            MiscMenu.Add("Plugins.Kalista.MiscMenu.SaveAlly", new CheckBox("Save your Soulbound ally from danger"));
            MiscMenu.AddSeparator(5);
            MiscMenu.Add("Plugins.Kalista.MiscMenu.BlitzCombo", new CheckBox("Enable Blitzcrank combo", false));
            MiscMenu.Add("Plugins.Kalista.MiscMenu.BlitzComboKillable",
                new CheckBox("Blitzcrank combo only if enemy is killable"));
            MiscMenu.AddLabel("Uses R when blitzcrank grabbed someone. Works also with Tahm Kench and Skarner.");
            MiscMenu.AddSeparator(5);

            MiscMenu.Add("Plugins.Kalista.MiscMenu.ReduceEDmg",
                new Slider("Reduce E damage calculations by ({0}%) percent", 5, 1));
            MiscMenu.AddLabel(
                "Reduces calculated Rend damage by desired amount. Might help if Kalista uses E too early.");
            MiscMenu.AddSeparator(5);

            var allies =(from aiHeroClient in EntityManager.Heroes.Allies
                    where !aiHeroClient.IsMe
                    select aiHeroClient.Hero.ToString()).ToList();

            if (!allies.Any())
                return;

            var soulBound = MiscMenu.Add("Plugins.Kalista.MiscMenu.SoulBoundHero", new ComboBox("Soulbound : ", allies));
            soulBound.OnValueChange += (a, b) =>
            {
                SouldBoundAlliedHero =
                    EntityManager.Heroes.Allies.Find(x => x.Hero.ToString() == soulBound.DisplayName);
            };

        }

        protected override void PermaActive()
        {
            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            Combo.Execute();
        }

        protected override void HarassMode()
        {
            Harass.Execute();
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Kalista.ComboMenu.UseQ"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Kalista.ComboMenu.UseE"];

                public static bool UseEBeforeDeath => MenuManager.MenuValues["Plugins.Kalista.ComboMenu.UseEBeforeDeath"];

                public static bool UseEBeforeEnemyLeavesRange => MenuManager.MenuValues["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRange"];

                public static int MinDamagePercToUseEBeforeEnemyLeavesRange => MenuManager.MenuValues["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRangeS", true];

                public static bool UseEToSlow => MenuManager.MenuValues["Plugins.Kalista.ComboMenu.UseEToSlow"];

                public static int UseEToSlowMinMinions => MenuManager.MenuValues["Plugins.Kalista.ComboMenu.UseEToSlowMinMinions", true];

                public static bool JumpOnMinions => MenuManager.MenuValues["Plugins.Kalista.ComboMenu.JumpOnMinions"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Kalista.HarassMenu.UseQ"];

                public static int MinManaForQ => MenuManager.MenuValues["Plugins.Kalista.HarassMenu.MinManaForQ", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Kalista.HarassMenu.UseE"];

                public static bool UseEIfManaWillBeRestored => MenuManager.MenuValues["Plugins.Kalista.HarassMenu.UseEIfManaWillBeRestored"];

                public static int MinManaForE => MenuManager.MenuValues["Plugins.Kalista.HarassMenu.MinManaForE", true];

                public static int MinStacksForE => MenuManager.MenuValues["Plugins.Kalista.HarassMenu.MinStacksForE", true];
            }

            internal static class JungleLaneClear
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.UseQ"];

                public static int MinManaForQ => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.MinManaForQ", true];

                public static int MinMinionsForQ => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForQ", true];

                public static bool UseE => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.UseE"];

                public static bool UseEOnUnkillableMinions => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.UseEForUnkillable"];

                public static int MinManaForE => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.MinManaForE", true];

                public static int MinMinionsForE => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForE", true];

                public static bool UseEToStealBuffs => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.UseEToStealBuffs"];

                public static bool UseEToStealDragon => MenuManager.MenuValues["Plugins.Kalista.JungleLaneClearMenu.UseEToStealDragon"];
            }

            internal static class Flee
            {
                public static bool JumpWithQ => MenuManager.MenuValues["Plugins.Kalista.FleeMenu.Jump"];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Kalista.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.Kalista.DrawingsMenu.DrawQ"];

                public static bool DrawE => MenuManager.MenuValues["Plugins.Kalista.DrawingsMenu.DrawE"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.Kalista.DrawingsMenu.DrawR"];

                public static bool DrawDamageIndicator => MenuManager.MenuValues["Plugins.Kalista.DrawingsMenu.DrawDamageIndicator"];

                public static int DamageIndicatorMode => MenuManager.MenuValues["Plugins.Kalista.DrawingsMenu.DamageIndicatorMode", true];
            }

            internal static class Misc
            {
                public static bool SaveAlly => MenuManager.MenuValues["Plugins.Kalista.MiscMenu.SaveAlly"];

                public static bool BlitzCombo => MenuManager.MenuValues["Plugins.Kalista.MiscMenu.BlitzCombo"];

                public static bool BlitzComboKillable => MenuManager.MenuValues["Plugins.Kalista.MiscMenu.BlitzComboKillable"];

                public static int ReduceEDmg => MenuManager.MenuValues["Plugins.Kalista.MiscMenu.ReduceEDmg", true];
            }
        }

        protected static class Damage
        {
            private static readonly int[] EDamage = { 0, 20, 30, 40, 50, 60 };
            private const float EDamageMod = 0.6f;
            private static readonly int[] EDamagePerSpear = { 0, 10, 14, 19, 25, 32 };
            private static readonly float[] EDamagePerSpearMod = { 0, 0.2f, 0.225f, 0.25f, 0.275f, 0.3f };
            
            private static CustomCache<KeyValuePair<int, int>, float> ComboDamages { get; } = Cache.Resolve<CustomCache<KeyValuePair<int, int>, float>>();
            private static CustomCache<int, int> EStacks { get; } = Cache.Resolve<CustomCache<int, int>>();
            private static CustomCache<int, bool> IsKillable { get; } = Cache.Resolve<CustomCache<int, bool>>();
            private static CustomCache<KeyValuePair<int, int>, float> EDamages { get; } = Cache.Resolve<CustomCache<KeyValuePair<int, int>, float>>();

            public static float GetComboDamage(AIHeroClient enemy, int stacks)
            {
                if (MenuManager.IsCacheEnabled && ComboDamages.Exist(new KeyValuePair<int, int>(enemy.NetworkId, stacks)))
                {
                    return ComboDamages.Get(new KeyValuePair<int, int>(enemy.NetworkId, stacks));
                }

                float damage = 0;

                if (Q.IsReady())
                    damage += Player.Instance.GetSpellDamage(enemy, SpellSlot.Q);

                if (Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing] != null && Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Blade_of_the_Ruined_King);

                if (Activator.Activator.Items[ItemsEnum.Cutlass] != null && Activator.Activator.Items[ItemsEnum.Cutlass].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Bilgewater_Cutlass);

                if (Activator.Activator.Items[ItemsEnum.Gunblade] != null && Activator.Activator.Items[ItemsEnum.Gunblade].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Hextech_Gunblade);

                if (E.IsReady())
                    damage += GetRendDamageOnTarget(enemy, stacks);

                damage += Player.Instance.GetAutoAttackDamage(enemy, true) * stacks;

                if (MenuManager.IsCacheEnabled)
                {
                    ComboDamages.Add(new KeyValuePair<int, int>(enemy.NetworkId, stacks), damage);
                }

                return damage;
            }

            public static bool CanCastEOnUnit(Obj_AI_Base target)
            {
                if (target == null || !target.IsValidTarget(E.Range) || CountEStacks(target) < 1 ||
                    !E.IsReady()) //|| GetRendBuff(target).Count < 1)BUG
                    return false;

                if (target.GetType() != typeof(AIHeroClient))
                    return true;

                var heroClient = (AIHeroClient)target;

                return !heroClient.HasUndyingBuffA() && !heroClient.HasSpellShield();
            }

            public static bool IsTargetKillableByRend(Obj_AI_Base target)
            {
                if (MenuManager.IsCacheEnabled && IsKillable.Exist(target.NetworkId))
                {
                    return IsKillable.Get(target.NetworkId);
                }

                if (target == null || !target.IsValidTarget(E.Range) || /*GetRendBuff(target) == null || */
                    !E.IsReady()) //|| GetRendBuff(target).Count < 1)BUG
                    return false;

                bool output;

                if (target.GetType() != typeof(AIHeroClient))
                {
                    output = GetRendDamageOnTarget(target) > target.TotalHealthWithShields();

                    if (MenuManager.IsCacheEnabled)
                    {
                        IsKillable.Add(target.NetworkId, output);
                    }
                    return output;
                }

                var heroClient = (AIHeroClient)target;

                if (heroClient.HasUndyingBuffA() || heroClient.HasSpellShield())
                {
                    if (MenuManager.IsCacheEnabled)
                    {
                        IsKillable.Add(target.NetworkId, false);
                    }

                    return false;
                }

                if (heroClient.ChampionName != "Blitzcrank")
                {
                    output = GetRendDamageOnTarget(heroClient) >= heroClient.TotalHealthWithShields();

                    if (MenuManager.IsCacheEnabled)
                    {
                        IsKillable.Add(target.NetworkId, output);
                    }

                    return output;
                }
                if (!heroClient.HasBuff("BlitzcrankManaBarrierCD") && !heroClient.HasBuff("ManaBarrier"))
                {
                    output = GetRendDamageOnTarget(heroClient) > heroClient.TotalHealthWithShields() + heroClient.Mana / 2;

                    if (MenuManager.IsCacheEnabled)
                    {
                        IsKillable.Add(target.NetworkId, output);
                    }
                    return output;
                }

                output = GetRendDamageOnTarget(heroClient) > heroClient.TotalHealthWithShields();

                if (MenuManager.IsCacheEnabled)
                {
                    IsKillable.Add(target.NetworkId, output);
                }

                return output;
            }

            public static float GetRendDamageOnTarget(Obj_AI_Base target)
            {
                if (!CanCastEOnUnit(target))
                    return 0f;

                if (MenuManager.IsCacheEnabled && EDamages.Exist(new KeyValuePair<int, int>(target.NetworkId, 0)))
                {
                    return EDamages.Get(new KeyValuePair<int, int>(target.NetworkId, 0));
                }

                var damageReduction = 100 - Settings.Misc.ReduceEDmg;
                var damage = EDamage[E.Level] + Player.Instance.TotalAttackDamage * EDamageMod +
                            (CountEStacks(target) > 1 //(GetRendBuff(target).Count > 1 BUG
                                 ? (EDamagePerSpear[E.Level] +
                                    Player.Instance.TotalAttackDamage * EDamagePerSpearMod[E.Level]) *
                                  (CountEStacks(target) - 1) //(GetRendBuff(target).Count - 1)BUG
                                 : 0);

                var finalDamage = Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                    damage * damageReduction / 100);

                if (MenuManager.IsCacheEnabled)
                {
                    EDamages.Add(new KeyValuePair<int, int>(target.NetworkId, 0), finalDamage);
                }

                return finalDamage;
            }

            public static float GetRendDamageOnTarget(Obj_AI_Base target, int stacks)
            {
                if (target == null || stacks < 1)
                    return 0f;

                if (MenuManager.IsCacheEnabled && EDamages.Exist(new KeyValuePair<int, int>(target.NetworkId, stacks)))
                {
                    return EDamages.Get(new KeyValuePair<int, int>(target.NetworkId, stacks));
                }

                var damageReduction = 100 - Settings.Misc.ReduceEDmg;

                var damage = EDamage[E.Level] + Player.Instance.TotalAttackDamage * EDamageMod +
                             (stacks > 1
                                 ? (EDamagePerSpear[E.Level] +
                                    Player.Instance.TotalAttackDamage * EDamagePerSpearMod[E.Level]) *
                                   (stacks - 1)
                                 : 0);
                var finalDamage = Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                    damage * damageReduction / 100);

                if (MenuManager.IsCacheEnabled)
                {
                    EDamages.Add(new KeyValuePair<int, int>(target.NetworkId, stacks), finalDamage);
                }

                return finalDamage;
            }

            public static int CountEStacks(Obj_AI_Base unit)
            {
                if (MenuManager.IsCacheEnabled && EStacks.Exist(unit.NetworkId))
                {
                    return EStacks.Get(unit.NetworkId);
                }

                var buff = GetRendBuff(unit);

                var stacks = buff?.Count ?? 0;

                if (MenuManager.IsCacheEnabled)
                {
                    EStacks.Add(unit.NetworkId, stacks);
                }

                return stacks;
            }

            public static BuffInstance GetRendBuff(Obj_AI_Base target)
            {
                return
                    target.Buffs.Find(
                        b => b.Caster.IsMe && b.IsValid && b.DisplayName.ToLowerInvariant() == "kalistaexpungemarker");
            }

            public static bool HasRendBuff(Obj_AI_Base target)
            {
                //return GetRendBuff(target) != null;BUG
                return CountEStacks(target) > 0;
            }
        }

        protected static class WallJumper
        {
            private static readonly Dictionary<Vector3, Vector3> WallJumpSpots = new Dictionary<Vector3, Vector3>
            {
                {
                    new Vector3(4674.075f, 5862.176f, 51.39587f), new Vector3(4964.955f, 5410.113f, 50.27698f)
                },
                {
                    new Vector3(4776.036f, 5681.148f, 50.2323f), new Vector3(4450.556f, 6258.124f, 51.30017f)
                },
                {
                    new Vector3(4205.026f, 6230.98f, 52.04443f), new Vector3(3390.128f, 7107.43f, 51.62903f)
                },
                {
                    new Vector3(4058.4f, 6407.077f, 52.46643f), new Vector3(4344.324f, 6137.956f, 52.11548f)
                },
                {
                    new Vector3(3648.005f, 6740.299f, 52.45801f), new Vector3(3432.113f, 7171.313f, 51.7063f)
                },
                {
                    new Vector3(3361.615f, 7455.041f, 51.89197f), new Vector3(3363.352f, 8117.096f, 51.78662f)
                },
                {
                    new Vector3(3321.974f, 7708.548f, 52.18164f), new Vector3(3353.69f, 7095.598f, 51.64148f)
                },
                {
                    new Vector3(3017.809f, 6767.353f, 51.46631f), new Vector3(2547.598f, 6734.157f, 55.98999f)
                },
                {
                    new Vector3(2799.904f, 6726.203f, 56.13196f), new Vector3(3233.671f, 6737.399f, 51.71729f) //
                },
                {
                    new Vector3(3019.342f, 6154.791f, 57.04688f), new Vector3(3353.769f, 6354.227f, 52.30249f)
                },
                {
                    new Vector3(3183.354f, 6284.209f, 52.05823f), new Vector3(2811.656f, 6101.958f, 57.04346f)
                },
                {
                    new Vector3(5985.822f, 5483.679f, 51.78357f), new Vector3(6224.729f, 5154.694f, 48.52795f)
                },
                {
                    new Vector3(6088.231f, 5311.382f, 48.66809f), new Vector3(5644.362f, 5734.164f, 51.55969f)
                },
                {
                    new Vector3(5958.93f, 4905.675f, 48.56433f), new Vector3(6064.395f, 4395.197f, 48.854f)
                },
                {
                    new Vector3(6003.119f, 4699.23f, 48.53394f), new Vector3(5836.034f, 5264.244f, 51.49707f) //
                },
                {
                    new Vector3(2073.425f, 9449.21f, 52.81799f), new Vector3(2324.867f, 9086.609f, 51.77649f)
                },
                {
                    new Vector3(2192.551f, 9277.479f, 51.77612f), new Vector3(1784.083f, 9769.506f, 52.83789f)
                },
                {
                    new Vector3(2596.218f, 9475.452f, 53.19934f), new Vector3(3001.343f, 9483.234f, 50.86426f)
                },
                {
                    new Vector3(2782.156f, 9524.927f, 51.70544f), new Vector3(2372.439f, 9541.388f, 54.12097f)
                },
                {
                    new Vector3(3260.017f, 9570.996f, 50.75f), new Vector3(3686.57f, 9676.949f, -67.49133f)
                },
                {
                    new Vector3(3830.311f, 9291.035f, -39.20325f), new Vector3(4330.704f, 9366.914f, -65.57581f)
                },
                {
                    new Vector3(4049.929f, 9337.727f, -67.83044f), new Vector3(3548.787f, 9153.723f, 42.43506f)
                },
                {
                    new Vector3(3463.348f, 9573.761f, -11.46021f), new Vector3(3027.57f, 9511.312f, 50.88171f) //
                },
                {
                    new Vector3(4678.605f, 8930.429f, -68.85168f), new Vector3(4657.199f, 8345.509f, 42.93982f)
                },
                {
                    new Vector3(4669.956f, 8709.591f, -26.17603f), new Vector3(4670.273f, 9154.957f, -67.51306f)
                },
                {
                    new Vector3(4964.398f, 9784.654f, -70.81885f), new Vector3(5107.994f, 10348.38f, -71.24084f)
                },
                {
                    new Vector3(4349.712f, 10221.11f, -71.2406f), new Vector3(4543.807f, 10568.52f, -71.24072f)
                },
                {
                    new Vector3(4990.49f, 10008.76f, -71.2406f), new Vector3(4911.171f, 9536.953f, -67.45691f)
                },
                {
                    new Vector3(4478.381f, 10413.06f, -71.24072f), new Vector3(4133.447f, 10008.29f, -71.24048f)
                },
                {
                    new Vector3(5450.241f, 10682.51f, -71.2406f), new Vector3(6056.064f, 10726.44f, 55.04712f)
                },
                {
                    new Vector3(5756.578f, 10627.04f, 55.50256f), new Vector3(5091.194f, 10523.42f, -71.2406f)
                },
                {
                    new Vector3(4654.404f, 12054.92f, 56.48206f), new Vector3(4866.387f, 12496.41f, 56.47717f)
                },
                {
                    new Vector3(4808.343f, 12232.11f, 56.47681f), new Vector3(4536.632f, 11772.7f, 56.84839f)
                },
                {
                    new Vector3(6597.816f, 11970.13f, 56.47681f), new Vector3(6654.76f, 11584.67f, 53.84241f)
                },
                {
                    new Vector3(5037.089f, 12122.35f, 56.47681f), new Vector3(4845.982f, 11712.43f, 56.83057f)
                },
                {
                    new Vector3(4921.988f, 11921.41f, 56.63684f), new Vector3(5131.101f, 12412.9f, 56.42578f)
                },
                {
                    new Vector3(6562.792f, 11729.48f, 53.84436f), new Vector3(6524.585f, 12614.06f, 55.2002f)
                },
                {
                    new Vector3(8112.829f, 9822.483f, 50.63965f), new Vector3(8879.749f, 9838.395f, 50.32629f)
                },
                {
                    new Vector3(8348.689f, 9854.147f, 50.38232f), new Vector3(7733.365f, 9923.805f, 51.47546f)
                },
                {
                    new Vector3(8876.396f, 9477.259f, 51.44019f), new Vector3(8683.688f, 9867.419f, 50.38428f)
                },
                {
                    new Vector3(8768.142f, 9647.292f, 50.38757f), new Vector3(8950.688f, 9282.373f, 52.81299f)
                },
                {
                    new Vector3(7237.933f, 8535.534f, 53.06848f), new Vector3(6911.81f, 8232.869f, -64.66357f)
                },
                {
                    new Vector3(7062.687f, 8373.167f, -70.64819f), new Vector3(7469.869f, 8734.628f, 52.87256f)
                },
                {
                    new Vector3(6873.494f, 8916.587f, 52.87219f), new Vector3(6505.242f, 8619.341f, -71.24048f)
                },
                {
                    new Vector3(6658.066f, 8833.806f, -71.24719f), new Vector3(6997.431f, 9236.629f, 52.93079f)
                },
                {
                    new Vector3(6516.605f, 9115.799f, 5.47644f), new Vector3(6472.492f, 8783.131f, -71.24048f)
                },
                {
                    new Vector3(6486.219f, 8932.186f, -50.38245f), new Vector3(6593.634f, 9636.98f, 53.25244f)
                },
                {
                    new Vector3(10191.71f, 9087.334f, 49.85303f), new Vector3(9929.119f, 9643.738f, 51.92957f)
                },
                {
                    new Vector3(10079.68f, 9291.497f, 51.9646f), new Vector3(10357.46f, 8853.28f, 53.68933f)
                },
                {
                    new Vector3(10656.18f, 8731.279f, 62.88135f), new Vector3(11305.75f, 7722.244f, 52.21777f)
                },
                {
                    new Vector3(10798.26f, 8547.337f, 63.08923f), new Vector3(10297.43f, 9050.915f, 49.49707f)
                },
                {
                    new Vector3(11239.86f, 8183.693f, 60.09253f), new Vector3(11422.37f, 7669.299f, 52.21594f)
                },
                {
                    new Vector3(11316.68f, 7953.193f, 52.21985f), new Vector3(10576.1f, 8966.911f, 56.98792f)
                },
                {
                    new Vector3(11792.18f, 8036.237f, 53.35071f), new Vector3(12238.76f, 8009.057f, 52.45483f)
                },
                {
                    new Vector3(12035.42f, 8022.847f, 52.50647f), new Vector3(11344.4f, 8019.567f, 52.20801f)
                },
                {
                    new Vector3(11615.39f, 8731.227f, 64.79346f), new Vector3(12007.9f, 9157.172f, 51.31812f)
                },
                {
                    new Vector3(11761.23f, 8902.433f, 50.30737f), new Vector3(11371.68f, 8622.363f, 62.18396f)
                },
                {
                    new Vector3(11461.58f, 7220.586f, 51.72644f), new Vector3(11435.65f, 7900.254f, 52.22717f)
                },
                {
                    new Vector3(11345.33f, 7475.27f, 52.20227f), new Vector3(11332.8f, 6884.023f, 51.71301f)
                },
                {
                    new Vector3(10943.87f, 7498.245f, 52.20349f), new Vector3(10985.02f, 6940.704f, 51.7229f)
                },
                {
                    new Vector3(10989.51f, 7276.3f, 51.72388f), new Vector3(11001.48f, 7824.464f, 52.20337f)
                },
                {
                    new Vector3(12685.75f, 5630.602f, 51.64124f), new Vector3(12987.24f, 5297.553f, 51.72949f)
                },
                {
                    new Vector3(12805.22f, 5476.07f, 52.39209f), new Vector3(12423.69f, 5878.115f, 57.12878f)
                },
                {
                    new Vector3(12271.95f, 5267.301f, 51.72949f), new Vector3(11785.53f, 5273.65f, 53.09851f)
                },
                {
                    new Vector3(12023.61f, 5546.923f, 54.08569f), new Vector3(12576f, 5489.254f, 51.9386f)
                },
                {
                    new Vector3(12270.39f, 5542.233f, 52.21411f), new Vector3(11741.91f, 5622.261f, 52.41943f)
                },
                {
                    new Vector3(12044.07f, 4595.935f, 51.72961f), new Vector3(11388.58f, 4303.719f, -71.2406f)
                },
                {
                    new Vector3(11657.52f, 4744.356f, -71.24072f), new Vector3(12110.17f, 5006.434f, 52.04895f)
                },
                {
                    new Vector3(11888.61f, 4827.719f, 51.75354f), new Vector3(11382.68f, 4625.833f, -71.24048f)
                },
                {
                    new Vector3(11367.42f, 5515.814f, 9.731201f), new Vector3(11784.17f, 5372.018f, 54.12024f)
                },
                {
                    new Vector3(11552.44f, 5440.104f, 54.07751f), new Vector3(11100.15f, 5604.056f, -28.15002f)
                },
                {
                    new Vector3(10078.97f, 2709.129f, 49.2229f), new Vector3(9992.279f, 3197.125f, 51.94043f)
                },
                {
                    new Vector3(10078.25f, 2985.698f, 50.72534f), new Vector3(10024.98f, 2478.564f, 49.22253f)
                },
                {
                    new Vector3(8300.578f, 2927.003f, 51.12988f), new Vector3(8174.804f, 3346.193f, 51.64172f)
                },
                {
                    new Vector3(8233.854f, 3175.692f, 51.64331f), new Vector3(8382.541f, 2395.067f, 51.10413f)
                },
                {
                    new Vector3(9052.554f, 4364.01f, 52.74133f), new Vector3(9483.579f, 4424.337f, -71.2406f)
                },
                {
                    new Vector3(9264.467f, 4418.867f, -71.24072f), new Vector3(8749.148f, 4352.381f, 53.22034f)
                },
                {
                    new Vector3(4776.829f, 3261.627f, 50.87463f), new Vector3(4359.231f, 3119.195f, 95.74817f)
                }
            };

            public static bool Jumping { get; private set; }
            public static Vector3 JumpingSpot { get; private set; }
            public static Vector3 OrbwalkingSpot { get; private set; }
            public static float StartTime { get; private set; }

            public static void Init()
            {
                Game.OnTick += OnTick;
            }

            private static void OnTick(EventArgs args)
            {
                if (!Jumping || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                {
                    return;
                }

                if (!Q.IsReady() || StartTime + 1000 < Game.Time*1000 || !Settings.Flee.JumpWithQ)
                {
                    Orbwalker.OverrideOrbwalkPosition = () => Game.CursorPos;
                    Jumping = false;
                    JumpingSpot = Vector3.Zero;
                    OrbwalkingSpot = Vector3.Zero;
                }

                if (Player.Instance.ServerPosition.Distance(OrbwalkingSpot) <
                    (OrbwalkingSpot == new Vector3(9253.057f, 4442.405f, -71.24084f)
                        ? 160
                        : Player.Instance.BoundingRadius) ||
                    (Player.Instance.Path.LastOrDefault().Distance(Player.Instance) < 10 &&
                     Player.Instance.ServerPosition.Distance(OrbwalkingSpot) < 200))
                {
                    Player.ForceIssueOrder(GameObjectOrder.Stop, Player.Instance.ServerPosition, true);
                    Q.Cast(Player.Instance.Position.Extend(JumpingSpot, 400).To3D());
                    Player.ForceIssueOrder(GameObjectOrder.MoveTo, JumpingSpot, true);
                    Orbwalker.OverrideOrbwalkPosition = () => Game.CursorPos;

                    Jumping = false;
                    JumpingSpot = Vector3.Zero;
                    OrbwalkingSpot = Vector3.Zero;
                }
            }

            public static void DrawSpots()
            {
                foreach (
                    var spot in
                        WallJumpSpots.Where(
                            id =>
                                id.Key.Distance(Player.Instance.Position) < 500 ||
                                id.Value.Distance(Player.Instance.Position) < 500))
                {
                    Circle.Draw(SharpDX.Color.LimeGreen, Player.Instance.BoundingRadius, spot.Key);
                }
            }

            public static void TryToJump()
            {
                if (!Q.IsReady() || Jumping || !Settings.Flee.JumpWithQ)
                    return;

                var pos = WallJumpSpots.OrderBy(x => x.Key.Distance(Player.Instance.ServerPosition)).FirstOrDefault();
                var oPos = pos.Value;

                if (Player.Instance.ServerPosition.Distance(pos.Key) <
                    (pos.Key == new Vector3(9264.467f, 4418.867f, -71.24072f) ? 150 : 75))
                {
                    Orbwalker.OverrideOrbwalkPosition =
                        () =>
                            pos.Key == new Vector3(9264.467f, 4418.867f, -71.24072f)
                                ? new Vector3(9247.006f, 4413.333f, -54.87134f)
                                : pos.Key;
                    OrbwalkingSpot = pos.Key;
                    JumpingSpot = oPos;
                    Jumping = true;
                    StartTime = Game.Time*1000;
                }
            }
        }
    }
}