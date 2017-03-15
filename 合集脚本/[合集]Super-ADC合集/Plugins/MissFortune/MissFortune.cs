#region Licensing
// ---------------------------------------------------------------------
// <copyright file="MissFortune.cs" company="EloBuddy">
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
using SharpDX;
using Marksman_Master.PermaShow.Values;
using Marksman_Master.Utils;
using Color = System.Drawing.Color;

namespace Marksman_Master.Plugins.MissFortune
{
    using Cache.Modules;

    internal class MissFortune : ChampionPlugin
    {
        protected static Spell.Targeted Q { get; }
        protected static Spell.Active W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }
        internal static Menu MiscMenu { get; set; }

        private static BoolItem AutoHarassItem { get; set; }

        private static ColorPicker[] ColorPicker { get; }

        private static bool _changingRangeScan;
        
        protected static byte[] QMana { get; } = {0, 43, 46, 49, 52, 55};
        protected static byte WMana { get; } = 30;
        protected static byte EMana { get; } = 80;
        protected static byte RMana { get; } = 100;
        protected static byte[] RWaves { get; } = { 0, 12, 14, 16 };

        protected static bool IsAfterAttack { get; private set; }
        protected static bool IsPreAttack { get; private set; }
        protected static bool RCasted { get; private set; }
        protected static float RCastTime { get; private set; }

        protected static float Q_ETA(Obj_AI_Base unit) => Player.Instance.DistanceCached(unit) / 1400 * 1000 + 250;

        protected static bool HasLoveTap(Obj_AI_Base unit)
            =>
                ObjectManager.Get<Obj_GeneralParticleEmitter>()
                    .Any(
                        x =>
                            x.Name.Equals("MissFortune_Base_P_Mark.troy", StringComparison.CurrentCultureIgnoreCase) &&
                            (Math.Abs(x.Distance(unit)) < 0.01));

        protected static bool HasWBuff => Player.Instance.Buffs.Any(x => x.Name.Equals("missfortuneviciousstrikes", StringComparison.CurrentCultureIgnoreCase));

        protected static Cache.Cache Cache => StaticCacheProvider.Cache;

        private static CustomCache<KeyValuePair<int, int>, float> CachedComboDamage { get; }

        static MissFortune()
        {
            Q = new Spell.Targeted(SpellSlot.Q, 720);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 1000, SkillShotType.Circular)
            {
                Width = 350
            };
            R = new Spell.Skillshot(SpellSlot.R, 1400, SkillShotType.Cone)
            {
                Width = (int)Math.PI / 180 * 35
            };

            CachedComboDamage = Cache.Resolve<CustomCache<KeyValuePair<int, int>, float>>(1000);

            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("MissFortuneQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("MissFortuneE", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("MissFortuneR", new ColorBGRA(255, 134, 0, 255));
            ColorPicker[3] = new ColorPicker("MissFortuneHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(ColorPicker[3].Color, (int)R.Range);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[3].OnColorChange +=
                (a, b) =>
                {
                    DamageIndicator.Color = b.Color;
                };

            Orbwalker.OnPostAttack += (sender, args) =>
            {
                IsAfterAttack = true;
                IsPreAttack = false;
            };

            Orbwalker.OnPreAttack += (target, args) => IsPreAttack = true;
            Game.OnPostTick += args => IsAfterAttack = false;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Player.OnIssueOrder += Player_OnIssueOrder;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
        }

        private static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe || !Settings.Combo.RBlockMovement || (args.Animation != "Spell4"))
                return;

            Orbwalker.DisableAttacking = true;
            Orbwalker.DisableMovement = true;
        }

        private static void Player_OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (!sender.IsMe || !Settings.Combo.RBlockMovement)
                return;

            if (RCasted && (Core.GameTickCount - RCastTime < 1500))
            {
                args.Process = false;
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!Settings.Combo.RBlockMovement)
                return;

            if (args.Slot == SpellSlot.R)
            {
                RCasted = true;
                RCastTime = Core.GameTickCount;

                Orbwalker.DisableAttacking = true;
                Orbwalker.DisableMovement = true;
            }

            if (RCasted && Player.Instance.Spellbook.IsChanneling)
            {
                args.Process = false;
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!Settings.Combo.RBlockMovement ||
                !(sender.IsMe &&
                  ((args.Slot == SpellSlot.R) ||
                   args.SData.Name.Equals("MissFortuneBulletTime", StringComparison.CurrentCultureIgnoreCase))))
                return;

            Orbwalker.DisableAttacking = true;
            Orbwalker.DisableMovement = true;
        }

        protected static IEnumerable<T> GetObjectsWithinQBounceRange<T>(Vector3 position) where T : Obj_AI_Base
        {
            var qPolygon = new Geometry.Polygon.Sector(position,
                Player.Instance.Position.Extend(position, position.Distance(Player.Instance) + 400).To3D(), (float) Math.PI/180f*55f, 400);

            if (typeof (T) == typeof (AIHeroClient))
            {
                return (IEnumerable<T>) StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    unit =>
                        new Geometry.Polygon.Circle(unit.Position, unit.BoundingRadius - 15).Points.Any(
                            k => qPolygon.IsInside(k)));
            }
            if (typeof (T) == typeof (Obj_AI_Base))
            {
                return (IEnumerable<T>) StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                    unit =>
                        new Geometry.Polygon.Circle(unit.Position, unit.BoundingRadius - 15).Points.Any(
                            k => qPolygon.IsInside(k))).Cast<Obj_AI_Base>()
                    .Concat(StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        unit =>
                            new Geometry.Polygon.Circle(unit.Position, unit.BoundingRadius - 15).Points.Any(
                                k => qPolygon.IsInside(k))));
            }
            if (typeof (T) == typeof (Obj_AI_Minion))
            {
                return (IEnumerable<T>)StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                    unit =>
                        new Geometry.Polygon.Circle(unit.Position, unit.BoundingRadius - 15).Points.Any(
                            k => qPolygon.IsInside(k)));
            }
            return null;
        }

        protected static IEnumerable<T> GetObjectsWithinRRange<T>(Vector3 position) where T : Obj_AI_Base
        {
            var rPolygon = new Geometry.Polygon.Sector(Player.Instance.Position, position, (float)Math.PI / 180f * 35f,
                1280);

            if (typeof(T) == typeof(AIHeroClient))
            {
                return (IEnumerable<T>)StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    unit =>
                        new Geometry.Polygon.Circle(unit.Position, unit.BoundingRadius - 15).Points.Any(
                            k => rPolygon.IsInside(k)));
            }
            if (typeof(T) == typeof(Obj_AI_Base))
            {
                return (IEnumerable<T>)StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                    unit =>
                        new Geometry.Polygon.Circle(unit.Position, unit.BoundingRadius - 15).Points.Any(
                            k => rPolygon.IsInside(k))).Cast<Obj_AI_Base>()
                    .Concat(StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        unit =>
                            new Geometry.Polygon.Circle(unit.Position, unit.BoundingRadius - 15).Points.Any(
                                k => rPolygon.IsInside(k))));
            }
            if (typeof(T) == typeof(Obj_AI_Minion))
            {
                return (IEnumerable<T>)StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                    unit =>
                        new Geometry.Polygon.Circle(unit.Position, unit.BoundingRadius - 15).Points.Any(
                            k => rPolygon.IsInside(k)));
            }
            return null;
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawDamageIndicator)
            {
                return 0;
            }

            return unit.GetType() != typeof(AIHeroClient) ? 0 : GetComboDamage(unit);
        }

        protected static float GetComboDamage(Obj_AI_Base unit, int autoAttacks = 1)
        {
            if (MenuManager.IsCacheEnabled && CachedComboDamage.Exist(new KeyValuePair<int, int>(unit.NetworkId, autoAttacks)))
            {
                return CachedComboDamage.Get(new KeyValuePair<int, int>(unit.NetworkId, autoAttacks));
            }
            var damage = 0f;

            if (unit.IsValidTarget(Q.Range) && Q.IsReady())
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.Q);
            
            if (unit.IsValidTarget(E.Range) && E.IsReady())
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.E);

            if (Player.Instance.IsInAutoAttackRange(unit))
                damage += Player.Instance.GetAutoAttackDamageCached(unit, true) * autoAttacks;
            
            if (MenuManager.IsCacheEnabled)
            {
                CachedComboDamage.Add(new KeyValuePair<int, int>(unit.NetworkId, autoAttacks), damage);
            }
            return damage;
        }
        
        protected static Obj_AI_Base GetQMinion(AIHeroClient target)
        {
            return GetQKillableMinion(target) ?? GetQUnkillableMinion(target);
        }

        protected static Obj_AI_Base GetQKillableMinion(AIHeroClient target)
        {
            return
                (from minion in
                     GetValidHeroesAndMinions().Where(
                        x =>
                            x.IsValidTarget(Q.Range) && !x.IsMoving && (x.Distance(target) <= 400) &&
                            (Prediction.Health.GetPrediction(x, (int)(x.Distance(Player.Instance) / 1400 * 1000 + 250)) > 20) &&
                            (Prediction.Health.GetPrediction(x, (int)(x.Distance(Player.Instance) / 1400 * 1000 + 250)) <
                             Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)))
                 let closest = GetQBouncePossibleObject(minion)
                 where
                     (closest != null) && (closest.Type == GameObjectType.AIHeroClient) &&
                     (closest.NetworkId == target.NetworkId)
                 select minion).FirstOrDefault();
        }

        protected static Obj_AI_Base GetQUnkillableMinion(AIHeroClient target)
        {
            return
                (from minion in
                     GetValidHeroesAndMinions().Where(
                        x =>
                            x.IsValidTarget(Q.Range) && !x.IsMoving && (x.Distance(target) <= 400) &&
                            (Prediction.Health.GetPrediction(x, (int)(x.Distance(Player.Instance) / 1400 * 1000 + 250)) > 20))
                 let closest = GetQBouncePossibleObject(minion)
                 where
                     (closest != null) && (closest.Type == GameObjectType.AIHeroClient) &&
                     (closest.NetworkId == target.NetworkId)
                 select minion).FirstOrDefault();
        }

        protected static Obj_AI_Base GetQBouncePossibleObject(Obj_AI_Base from)
        {
            var qobjects = GetObjectsWithinQBounceRange<Obj_AI_Base>(from.Position);

            foreach (var objAiBase in qobjects.OrderBy(x=>x.Distance(@from)).Where(objAiBase => @from.NetworkId != objAiBase.NetworkId))
            {
                var position = Prediction.Position.PredictUnitPosition(objAiBase, (int)Q_ETA(objAiBase)).To3D();

                if ((objAiBase.GetType() == typeof(AIHeroClient)) && HasLoveTap(objAiBase) && 
                    new Geometry.Polygon.Circle(position, objAiBase.BoundingRadius).Points.Any(k => new Geometry.Polygon.Sector(position,
                          Player.Instance.Position.Extend(objAiBase, objAiBase.Distance(Player.Instance) + 400).To3D(), (float)Math.PI / 180f * 40f, 400).IsInside(k)))
                {
                    return objAiBase;
                }
                
                if ((objAiBase.GetType() == typeof (Obj_AI_Minion)) &&
                    new Geometry.Polygon.Circle(objAiBase.Position, objAiBase.BoundingRadius - 15).Points.Any(
                        k =>
                            new Geometry.Polygon.Sector(objAiBase.Position,
                                Player.Instance.Position.Extend(objAiBase, position.Distance(Player.Instance) + 400)
                                    .To3D(), (float) Math.PI/180f*20f, 400).IsInside(k)))
                {
                    return objAiBase;
                }
                if ((objAiBase.GetType() == typeof (AIHeroClient)) &&
                    new Geometry.Polygon.Circle(position, objAiBase.BoundingRadius).Points.Any(
                        k =>
                            new Geometry.Polygon.Sector(position,
                                Player.Instance.Position.Extend(objAiBase, position.Distance(Player.Instance) + 400)
                                    .To3D(), (float) Math.PI/180f*20f, 400).IsInside(k)))
                {
                    return objAiBase;
                }
                if ((objAiBase.GetType() == typeof (Obj_AI_Minion)) &&
                    new Geometry.Polygon.Circle(objAiBase.Position, objAiBase.BoundingRadius - 15).Points.Any(
                        k =>
                            new Geometry.Polygon.Sector(objAiBase.Position,
                                Player.Instance.Position.Extend(objAiBase, objAiBase.Distance(Player.Instance) + 400)
                                    .To3D(), (float) Math.PI/180f*40f, 400).IsInside(k)))
                {
                    return objAiBase;
                }
                if ((objAiBase.GetType() == typeof (AIHeroClient)) &&
                    new Geometry.Polygon.Circle(position, objAiBase.BoundingRadius).Points.Any(
                        k =>
                            new Geometry.Polygon.Sector(position,
                                Player.Instance.Position.Extend(objAiBase, position.Distance(Player.Instance) + 400)
                                    .To3D(), (float) Math.PI/180f*40f, 400).IsInside(k)))
                {
                    return objAiBase;
                }
            }
            return null;
        }

        protected static IEnumerable<Obj_AI_Base> GetValidHeroesAndMinions()
        {
            return
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached()).Cast<Obj_AI_Base>()
                    .Concat(StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                        x => x.IsValidTargetCached()));
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.MissFortune.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue,
                    Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[1].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (Settings.Misc.EVsGapclosers && E.IsReady() && (args.End.Distance(Player.Instance) < 350) && (Player.Instance.Mana - EMana > QMana[Q.Level] + WMana + RMana))
            {
                E.CastMinimumHitchance(sender, 65);
            }
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Miss Fortune addon");

            ComboMenu.AddLabel("Double Up (Q) settings :");
            ComboMenu.Add("Plugins.MissFortune.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Strut (W) settings :");
            ComboMenu.Add("Plugins.MissFortune.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Make It Rain (E) settings :");
            ComboMenu.Add("Plugins.MissFortune.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Bullet Time (R) settings :");
            ComboMenu.Add("Plugins.MissFortune.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.MissFortune.ComboMenu.RWhenXEnemies", new Slider("Use R when can hit {0} or more enemies", 5, 1, 5));
            ComboMenu.AddSeparator(2);

            ComboMenu.Add("Plugins.MissFortune.ComboMenu.RBlockMovement", new CheckBox("Block movement when casting R"));
            ComboMenu.Add("Plugins.MissFortune.ComboMenu.SemiAutoRKeybind",
                new KeyBind("Semi-Auto R", false, KeyBind.BindTypes.HoldActive, 'T'));

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Miss Fortune addon");

            HarassMenu.AddLabel("Double Up (Q) settings :");
            HarassMenu.Add("Plugins.MissFortune.HarassMenu.UseQ", new CheckBox("Use Q on killable minion if Q2 will hit champion"));
            HarassMenu.Add("Plugins.MissFortune.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q on killable minion", 50, 1));
            HarassMenu.Add("Plugins.MissFortune.HarassMenu.UseQUnkillable", new CheckBox("Use Q on unkillable minion if Q2 will hit champion"));
            HarassMenu.Add("Plugins.MissFortune.HarassMenu.MinManaQUnkillable", new Slider("Min mana percentage ({0}%) to use Q on unkillable minion", 75, 1));

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Miss Fortune addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Double Up (Q) settings :");
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane clear", false));
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle clear"));
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Strut (W) settings :");
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.UseWInLaneClear", new CheckBox("Use W in Lane clear", false));
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.UseWInJungleClear", new CheckBox("Use W in Jungle clear"));
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Make It Rain (E) settings :");
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.UseEInLaneClear", new CheckBox("Use E in Lane clear", false));
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.UseEInJungleClear", new CheckBox("Use E in Jungle clear", false));
            LaneClearMenu.Add("Plugins.MissFortune.LaneClearMenu.MinManaE", new Slider("Min mana percentage ({0}%) to use E", 50, 1));


            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Miss Fortune addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.MissFortune.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Double Up (Q) settings :");
            MiscMenu.Add("Plugins.MissFortune.MiscMenu.BounceQFromMinions", new CheckBox("Cast Q on killable minions if can hit enemy"));
            MiscMenu.Add("Plugins.MissFortune.MiscMenu.AutoHarassQ", new CheckBox("Auto harass with Q")).OnValueChange
                +=
                (sender, args) =>
                {
                    AutoHarassItem.Value = args.NewValue;
                };
            MiscMenu.Add("Plugins.MissFortune.MiscMenu.AutoHarassQMinMana", new Slider("Min mana percentage ({0}%) for auto harass", 50, 1));

            if (EntityManager.Heroes.Enemies.Any())
            {
                MiscMenu.AddLabel("Enable auto harras for : ");

                EntityManager.Heroes.Enemies.ForEach(x => MiscMenu.Add("Plugins.MissFortune.MiscMenu.AutoHarassEnabled." + x.ChampionName, new CheckBox(x.ChampionName == "MonkeyKing" ? "Wukong" : x.ChampionName)));
            }

            MiscMenu.AddLabel("Make It Rain (E) settings :");
            MiscMenu.Add("Plugins.MissFortune.MiscMenu.EVsGapclosers", new CheckBox("Cast E against gapclosers"));

            MenuManager.BuildAntiGapcloserMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Miss Fortune addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Double Up (Q) settings :");
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DrawQ", new CheckBox("Draw Q range", false));
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Make It Rain (E) settings :");
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Bullet Time (R) settings :");
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };

            DrawingsMenu.AddLabel("Damage indicator settings :");
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DrawDamageIndicator", new CheckBox("Draw damage indicator")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.MissFortune.DrawingsMenu.DamageIndicatorColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[3].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };

            AutoHarassItem = MenuManager.PermaShow.AddItem("MissFortune.AutoHarass",
                new BoolItem("Auto harass with Q", Settings.Misc.AutoHarassQ));
        }

        protected override void PermaActive()
        {
            if (Settings.Combo.RBlockMovement && RCasted && (Player.Instance.Spellbook.IsChanneling || Player.Instance.Spellbook.IsCastingSpell))
            {
                Orbwalker.DisableAttacking = true;
                Orbwalker.DisableMovement = true;
            }
            else if(!Player.Instance.Spellbook.IsChanneling && (Core.GameTickCount - RCastTime > 1500))
            {
                Orbwalker.DisableAttacking = false;
                Orbwalker.DisableMovement = false;

                RCasted = false;
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

        protected internal static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.MissFortune.ComboMenu.UseQ"];

                public static bool UseW => MenuManager.MenuValues["Plugins.MissFortune.ComboMenu.UseW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.MissFortune.ComboMenu.UseE"];

                public static bool UseR => MenuManager.MenuValues["Plugins.MissFortune.ComboMenu.UseR"];

                public static bool RBlockMovement => MenuManager.MenuValues["Plugins.MissFortune.ComboMenu.RBlockMovement"];

                public static int RWhenXEnemies => MenuManager.MenuValues["Plugins.MissFortune.ComboMenu.RWhenXEnemies", true];

                public static bool SemiAutoRKeybind => MenuManager.MenuValues["Plugins.MissFortune.ComboMenu.SemiAutoRKeybind"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.MissFortune.HarassMenu.UseQ"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.MissFortune.HarassMenu.MinManaQ", true];

                public static bool UseQUnkillable => MenuManager.MenuValues["Plugins.MissFortune.HarassMenu.UseQUnkillable"];

                public static int MinManaQUnkillable => MenuManager.MenuValues["Plugins.MissFortune.HarassMenu.MinManaQUnkillable", true];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.UseQInJungleClear"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.MinManaQ", true];

                public static bool UseWInLaneClear => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.UseWInLaneClear"];

                public static bool UseWInJungleClear => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.UseWInJungleClear"];

                public static int MinManaW => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.MinManaW", true];

                public static bool UseEInLaneClear => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.UseEInLaneClear"];

                public static bool UseEInJungleClear => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.UseEInJungleClear"];

                public static int MinManaE => MenuManager.MenuValues["Plugins.MissFortune.LaneClearMenu.MinManaE", true];
            }

            internal static class Misc
            {
                public static bool EnableKillsteal => MenuManager.MenuValues["Plugins.MissFortune.MiscMenu.EnableKillsteal"];

                public static bool BounceQFromMinions => MenuManager.MenuValues["Plugins.MissFortune.MiscMenu.BounceQFromMinions"];

                public static bool AutoHarassQ => MenuManager.MenuValues["Plugins.MissFortune.MiscMenu.AutoHarassQ"];

                public static int AutoHarassQMinMana => MenuManager.MenuValues["Plugins.MissFortune.MiscMenu.AutoHarassQMinMana", true];

                public static bool IsAutoHarassEnabledFor(AIHeroClient unit) => MenuManager.MenuValues["Plugins.MissFortune.MiscMenu.AutoHarassEnabled." + unit.ChampionName];

                public static bool IsAutoHarassEnabledFor(string championName) => MenuManager.MenuValues["Plugins.MissFortune.MiscMenu.AutoHarassEnabled." + championName];

                public static bool EVsGapclosers => MenuManager.MenuValues["Plugins.MissFortune.MiscMenu.EVsGapclosers"];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.MissFortune.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.MissFortune.DrawingsMenu.DrawQ"];

                public static bool DrawE => MenuManager.MenuValues["Plugins.MissFortune.DrawingsMenu.DrawE"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.MissFortune.DrawingsMenu.DrawR"];

                public static bool DrawDamageIndicator => MenuManager.MenuValues["Plugins.MissFortune.DrawingsMenu.DrawDamageIndicator"];
            }
        }
    }
}