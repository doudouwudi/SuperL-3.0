#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Lucian.cs" company="EloBuddy">
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

namespace Marksman_Master.Plugins.Lucian
{
    using Utils;
    using Cache.Modules;

    internal class Lucian : ChampionPlugin
    {
        protected static Spell.Targeted Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawingsMenu { get; set; }

        protected static bool HasPassiveBuff
            => CanLosePassive || Player.Instance.Buffs.Any(x => x.Name.Equals("lucianpassivebuff", StringComparison.CurrentCultureIgnoreCase));
            //=> CanLosePassive || ObjectManager.Get<Obj_GeneralParticleEmitter>().Any(
            //        x =>
            //            x.Name.Equals("Lucian_Base_P_buf.troy", StringComparison.CurrentCultureIgnoreCase) &&
            //            (x.Distance(Player.Instance) <= 200));

        protected static BuffInstance GetPassiveBuff
            => Player.Instance.Buffs.FirstOrDefault(x => x.Name.Equals("lucianpassivebuff", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasWDebuff(Obj_AI_Base unit)
            => unit.Buffs.Any(x => x.Name.Equals("lucianwdebuff", StringComparison.CurrentCultureIgnoreCase));

        private static readonly ColorPicker[] ColorPicker;
        private static bool _changingRangeScan;

        protected static bool IsPreAttack { get; private set; }
        protected static bool IsPostAttack { get; private set; }

        protected static bool IsCastingQ
            =>
                Player.Instance.Spellbook.IsCastingSpell && (Q.Handle.CooldownExpires - Game.Time <= 0) &&
                (Q.Handle.State == SpellState.Surpressed);

        protected static bool IsCastingR => Player.Instance.Buffs.Any(x => x.Name.Equals("lucianr", StringComparison.CurrentCultureIgnoreCase));

        protected static bool HasAnyOrbwalkerFlags
            =>
                (Orbwalker.ActiveModesFlags &
                 (Orbwalker.ActiveModes.Combo | Orbwalker.ActiveModes.Harass | Orbwalker.ActiveModes.LaneClear |
                  Orbwalker.ActiveModes.LastHit | Orbwalker.ActiveModes.JungleClear | Orbwalker.ActiveModes.Flee)) != 0;

        protected static int QCastTime => 400;

        protected static Cache.Cache Cache => StaticCacheProvider.Cache;

        private static CustomCache<KeyValuePair<int, int>, float> CachedComboDamage { get; }
        
        private static float LastETime { get; set; }

        protected static int QMana => Q.IsLearned ? 50 + 5 * (Q.Level-1): 0;
        protected static int WMana => W.IsLearned ? 50 : 0;
        protected static int EMana => E.IsLearned ? 40 - 10 * (E.Level - 1) : 0;
        protected static int RMana => R.IsLearned ? 100 : 0;

        protected static Vector3 RDirection { get; private set; }

        protected static List<MissileClient> RMissiles =>
            ObjectManager.Get<MissileClient>()
                .Where(
                    x =>
                        x.SpellCaster.IsMe &&
                        x.SData.Name.Equals("LucianRMissile", StringComparison.CurrentCultureIgnoreCase)).ToList();

        private static int LastSpellCastTime { get; set; }

        protected static bool CanLosePassive
            => Core.GameTickCount - LastSpellCastTime <= Player.Instance.AttackCastDelay*1000 + Math.Min(Game.Ping/2, 70);

        static Lucian()
        {
            Q = new Spell.Targeted(SpellSlot.Q, 650);
            W = new Spell.Skillshot(SpellSlot.W, 1000, SkillShotType.Circular, 320, 1600, 100);
            E = new Spell.Skillshot(SpellSlot.E, 475, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 1150, SkillShotType.Linear, 250, 2000, 110);

            CachedComboDamage = Cache.Resolve<CustomCache<KeyValuePair<int, int>, float>>(1000);
            
            ColorPicker = new ColorPicker[3];

            ColorPicker[0] = new ColorPicker("LucianQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("LucianR", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("LucianHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(ColorPicker[2].Color);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[2].OnColorChange += (a, b) => { DamageIndicator.Color = b.Color; };

            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            
            Game.OnPostTick += args => IsPostAttack = false;

            Obj_AI_Base.OnPlayAnimation += (sender, args) =>
            {
                if (sender.IsMe && ((args.Animation == "Spell1") || (args.Animation == "Spell2") || (args.Animation == "Spell3")) && HasAnyOrbwalkerFlags)
                {
                    Player.ForceIssueOrder(GameObjectOrder.MoveTo, Game.CursorPos, false);
                }
            };
            
            Spellbook.OnCastSpell += (sender, args) =>
            {
                if (!sender.Owner.IsMe)
                    return;

                if ((args.Slot != SpellSlot.Q) && (args.Slot != SpellSlot.W) && (args.Slot != SpellSlot.E))
                    return;

                if (HasAnyOrbwalkerFlags)
                {
                    if (IsPreAttack)
                    {
                        args.Process = false;
                        return;
                    }

                    if (args.Slot == SpellSlot.E)
                    {
                        LastETime = Core.GameTickCount;
                    }
                }

                LastSpellCastTime = Core.GameTickCount;
            };
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!HasAnyOrbwalkerFlags)
                return;

            if (sender.GetType() == typeof (MissileClient))
            {
                var missile = sender as MissileClient;

                if ((missile != null) && missile.SpellCaster.IsMe)
                {
                    if (missile.SData.Name == "LucianWMissile")
                    {
                        Orbwalker.ResetAutoAttack();
                        return;
                    }
                }
            }

            if (sender.GetType() != typeof (Obj_GeneralParticleEmitter))
                return;

            var particle = sender as Obj_GeneralParticleEmitter;

            if ((particle == null) || !particle.Name.Contains("Lucian_Base_Q_laser") || (particle.Distance(Player.Instance) > 200))
                return;
            
            Orbwalker.ResetAutoAttack();
        }
        
        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Player.Instance.Spellbook.IsCastingSpell || (Core.GameTickCount - LastETime < 300))
                args.Process = false;// q bug stops occuring
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || !args.SData.Name.Equals("LucianR", StringComparison.CurrentCultureIgnoreCase))
                return;

            Activator.Activator.Items[ItemsEnum.Ghostblade].UseItem();
            RDirection = Player.Instance.Position.Extend(args.End, 9999).To3D();
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            IsPreAttack = false;
            IsPostAttack = true;
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawInfo)
            {
                return 0;
            }

            var enemy = (AIHeroClient)unit;

            return enemy == null ? 0 : GetComboDamage(unit);
        }

        protected static float GetComboDamage(Obj_AI_Base unit, int autoAttacks = 1)
        {
            if (MenuManager.IsCacheEnabled &&
                CachedComboDamage.Exist(new KeyValuePair<int, int>(unit.NetworkId, autoAttacks)))
            {
                return CachedComboDamage.Get(new KeyValuePair<int, int>(unit.NetworkId, autoAttacks));
            }
            
            var damage = Player.Instance.GetAutoAttackDamageCached(unit) * autoAttacks;

            if (unit.IsValidTarget(900) && Q.IsReady())
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.Q);

            if (unit.IsValidTarget(W.Range) && W.IsReady())
                damage += Player.Instance.GetSpellDamageCached(unit, SpellSlot.W);
            
            if (MenuManager.IsCacheEnabled)
            {
                CachedComboDamage.Add(new KeyValuePair<int, int>(unit.NetworkId, autoAttacks), damage);
            }
            return damage;
        }

        protected static Obj_AI_Base GetQExtendSource(Obj_AI_Base target)
        {
            var dyingTargets =
                GetValidHeroesAndMinions(Q.Range)
                    .Where(x => Prediction.Health.GetPrediction(x, QCastTime) <= 0)
                    .ToList();

            return (from entity in dyingTargets.Any() ? dyingTargets : GetValidHeroesAndMinions(Q.Range)
                    let pos =
                    Player.Instance.Position.Extend(target,
                        Player.Instance.Distance(target) > 900 ? 900 - Player.Instance.Distance(target) : 900).To3D()
                    let targetpos = Prediction.Position.PredictUnitPosition(target, QCastTime)
                    let rect = new Geometry.Polygon.Rectangle(entity.Position, pos, 20)
                    where new Geometry.Polygon.Circle(targetpos, target.BoundingRadius).Points.Any(rect.IsInside)
                    select entity).FirstOrDefault();
        }

        protected static IEnumerable<Obj_AI_Base> GetValidHeroesAndMinions(float range)
        {
            return
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached()).Cast<Obj_AI_Base>()
                    .Concat(StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                        x => x.IsValidTargetCached(range)));
        }

        protected static void ELogics()
        {
            if (!E.IsReady() || !Settings.Combo.UseE || IsCastingR || HasPassiveBuff || Player.Instance.HasSheenBuff())
                return;

            if ((Settings.Misc.EUsageMode == 1) && !IsPostAttack)
                return;

            var heroClient = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 470, DamageType.Physical);

            if (heroClient == null)
                return;

            if (!IsPostAttack && !Q.IsReady() && (heroClient.TotalHealthWithShields() >= Player.Instance.GetAutoAttackDamageCached(heroClient, true) * 5))
                return;

            if (IsCastingQ && !PossibleToInterruptQ(heroClient))
                return;

            var castTime = Player.Instance.Spellbook.CastTime - Game.Time;

            if(!IsPostAttack && (castTime > 0))
                return;

            var positionAfterE = Prediction.Position.PredictUnitPosition(heroClient, 300); // +-
            var shortEPosition = Player.Instance.Position.Extend(Game.CursorPos, 70).To3D();

            if (Q.IsReady() && !IsPostAttack && shortEPosition.IsVectorUnderEnemyTower())
                return;

            if ((
                    ((GetComboDamage(heroClient, 4) >= heroClient.TotalHealthWithShields()) && (Player.Instance.CountEnemiesInRangeCached(1300) <= 2)) ||
                    (Player.Instance.CountEnemiesInRangeCached(1300) <= 1)
                ) && Player.Instance.IsInRange(positionAfterE, Player.Instance.GetAutoAttackRange() - 70) && (shortEPosition.Distance(heroClient) > 400))
            {
                E.Cast(shortEPosition);
                Orbwalker.ResetAutoAttack();
                return;
            }

            var damage = GetComboDamage(heroClient, 2);
            var pos = Game.CursorPos.Distance(Player.Instance) > 470 ? Player.Instance.Position.Extend(Game.CursorPos, 470).To3D() : Game.CursorPos;
            var enemiesInPosition = pos.CountEnemyHeroesInRangeWithPrediction((int) Player.Instance.GetAutoAttackRange(), 335);

            if (!IsPostAttack && ((damage < heroClient.TotalHealthWithShields()) || !PossibleEqCombo(heroClient) ||
                 (enemiesInPosition <= 0) || (enemiesInPosition >= 3)))
                return;

            var enemies = Player.Instance.CountEnemiesInRange(1300);
            
            if (!pos.IsVectorUnderEnemyTower())
            {
                if (enemies == 1)
                {
                    var isInRange = pos.IsInRangeCached(positionAfterE, heroClient.IsMelee ? 500 : 300);

                    if (!isInRange ||
                        ((damage >= heroClient.TotalHealthWithShields()) &&
                         EnemiesInDirectionOfTheDash(pos, 2000).Any(x => x.IdEquals(heroClient))) || !heroClient.IsMovingTowards(Player.Instance, 600))
                    {
                        if ((Player.Instance.HealthPercent >= heroClient.HealthPercent) && 
                            Player.Instance.IsInRangeCached(heroClient, Player.Instance.GetAutoAttackRange()) &&
                            !pos.IsInRangeCached(heroClient, Player.Instance.GetAutoAttackRange() - 50))
                        {
                            return;
                        }

                        E.Cast(pos);
                        return;
                    }
                }
                else if ((enemies == 2) && ((Player.Instance.CountAlliesInRangeCached(400) > 1) ||
                                            ((damage >= heroClient.TotalHealthWithShields()) && (pos.CountEnemiesInRangeCached(Player.Instance.GetAutoAttackRange()) == 1)) ||
                                            !StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                                                x =>
                                                    x.IsValidTarget(1200) &&
                                                    pos.IsInRangeCached(
                                                        Prediction.Position.PredictUnitPosition(heroClient, 300),
                                                        x.IsMelee ? 500 : x.GetAutoAttackRange())).Any()))
                {
                    E.Cast(pos);
                    return;
                }
                else
                {
                    var range = enemies*150;

                    if (!StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x =>
                        pos.IsInRangeCached(Prediction.Position.PredictUnitPosition(x, 300), range < x.GetAutoAttackRange() ? x.GetAutoAttackRange() : range)).Any())
                    {
                        E.Cast(pos);
                        return;
                    }
                }
            }

            var closest = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(1300)).OrderBy(x => x.DistanceCached(Player.Instance)).FirstOrDefault();
            var paths = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(1300)).Count(x => x.IsMovingTowards(Player.Instance));
            var validEscapeDash = (pos.DistanceCached(closest) > Player.Instance.DistanceCached(closest)) && (pos.DistanceCached(Player.Instance) >= 450);

            if ((closest != null) && (Player.Instance.CountEnemiesInRangeCached(350) >= 1) && (paths >= 1) && validEscapeDash)
            {
                E.Cast(pos);
            }
        }

        protected static bool PossibleToInterruptQ(AIHeroClient target)
        {
            if (target == null)
                return false;

            return IsCastingQ && E.IsReady() && (Player.Instance.Mana >= EMana) && (target.TotalHealthWithShields() <= GetComboDamage(target, 2)) && Player.Instance.IsInAutoAttackRange(target);
        }

        protected static bool PossibleEqCombo(AIHeroClient target)
        {
            if (target == null)
                return false;

            return Q.IsReady() && E.IsReady() && (Player.Instance.Mana >= QMana + EMana) && !HasPassiveBuff;
        }

        protected static IEnumerable<AIHeroClient> EnemiesInDirectionOfTheDash(Vector3 dashEndPosition, float maxRangeToEnemy)
        {
            return
                from enemy in
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(maxRangeToEnemy))

                let dotProduct = (dashEndPosition - Player.Instance.Position).Normalized()
                    .To2D()
                    .DotProduct(enemy.Position.To2D().Normalized())
                    
                where dotProduct >= .65
                select enemy;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Lucian.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady(
                )))
                Circle.Draw(ColorPicker[1].Color, R.Range, Player.Instance);

            if (Misc.IsMe && MenuManager.IsDebugEnabled)
            {
                var objects =
                    ObjectManager.Get<Obj_GeneralParticleEmitter>()
                        .Where(x => x.DistanceCached(Player.Instance) <= 200)
                        .Aggregate("Particles near player : ",
                            (current, objectType) =>
                                current + objectType.Name +
                                $" distance : {Player.Instance.DistanceCached(objectType)}, ");

                var buffs = Player.Instance.Buffs.Aggregate("Buffs : ", (current, buff) => current + buff.Name + ", ");

                Drawing.DrawText(300, 300, System.Drawing.Color.White, objects);
                Drawing.DrawText(Drawing.WorldToScreen(Player.Instance.Position), System.Drawing.Color.White, buffs, 11);
                Drawing.DrawText(Drawing.WorldToScreen(Player.Instance.Position), System.Drawing.Color.White, $"\n\nLast cast : {Core.GameTickCount - LastSpellCastTime}\n" +
                                                                                                              $"Attack cast delay : {Player.Instance.AttackCastDelay*1000}\n" +
                                                                                                              $"CanLosePassive : {CanLosePassive}", 11);

                if (objects != "Particles near player : ")
                    Console.WriteLine(objects);
            }

            if (!IsCastingR || !Settings.Drawings.DrawInfo)
                return;
            
            Misc.DrawRectangle(Player.Instance.Position, Player.Instance.Position.Extend(RDirection, R.Range).To3D(), 130, 3, System.Drawing.Color.White);
            Misc.DrawRectangle(Player.Instance.Position, Player.Instance.Position.Extend(RDirection, R.Range).To3D(), 90, 2, System.Drawing.Color.Gold,RectangleDrawingFlags.Side);
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
            ComboMenu.AddGroupLabel("Combo mode settings for Lucian addon");

            ComboMenu.AddLabel("Piercing Light (Q) settings :");
            ComboMenu.Add("Plugins.Lucian.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.Add("Plugins.Lucian.ComboMenu.ExtendQOnMinions", new CheckBox("Try to extend Q on minions"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Ardent Blaze (W) settings :");
            ComboMenu.Add("Plugins.Lucian.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.Add("Plugins.Lucian.ComboMenu.IgnoreCollisionW", new CheckBox("Ignore collision"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Relentless Pursuit (E) settings :");
            ComboMenu.Add("Plugins.Lucian.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("The Culling (R) settings :");
            ComboMenu.Add("Plugins.Lucian.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Lucian.ComboMenu.RKeybind", new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Lucian addon");

            HarassMenu.AddLabel("Piercing Light (Q) settings :");
            HarassMenu.Add("Plugins.Lucian.HarassMenu.UseQ",
                new KeyBind("Enable auto harass", false, KeyBind.BindTypes.PressToggle, 'A'));
            HarassMenu.Add("Plugins.Lucian.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Auto harass enabled for :");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                HarassMenu.Add("Plugins.Lucian.HarassMenu.UseQ."+enemy.Hero, new CheckBox(enemy.ChampionName == "MonkeyKing" ? "Wukong" : enemy.ChampionName));
            }

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Lucian addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Piercing Light (Q) settings :");
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.MinMinionsHitQ", new Slider("Min minions hit to use Q", 3, 1, 8));
            LaneClearMenu.AddSeparator(5);
            
            LaneClearMenu.AddGroupLabel("Jungle Clear : ");
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.UseWInJungleClear", new CheckBox("Use W in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.UseEInJungleClear", new CheckBox("Use E in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) for jungle clear", 50, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Lucian addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Lucian.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Relentless Pursuit (E) settings :");
            MiscMenu.Add("Plugins.Lucian.MiscMenu.EUsageMode", new ComboBox("E usage", 0, "Always", "After autoattack only"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Lucian addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Piercing Light (Q) settings :");
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("The Culling (R) settings :");
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawInfo", new CheckBox("Draw Infos")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.InfoColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddLabel("Draws damage indicator and R shooting indicator");
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
                public static bool UseQ => MenuManager.MenuValues["Plugins.Lucian.ComboMenu.UseQ"];

                public static bool ExtendQOnMinions => MenuManager.MenuValues["Plugins.Lucian.ComboMenu.ExtendQOnMinions"];

                public static bool UseW => MenuManager.MenuValues["Plugins.Lucian.ComboMenu.UseW"];

                public static bool IgnoreCollisionW => MenuManager.MenuValues["Plugins.Lucian.ComboMenu.IgnoreCollisionW"];

                public static bool UseE => MenuManager.MenuValues["Plugins.Lucian.ComboMenu.UseE"];

                public static bool UseR => MenuManager.MenuValues["Plugins.Lucian.ComboMenu.UseR"];

                public static bool RKeybind => MenuManager.MenuValues["Plugins.Lucian.ComboMenu.RKeybind"];
            }

            internal static class Harass
            {
                public static bool UseQ => MenuManager.MenuValues["Plugins.Lucian.HarassMenu.UseQ"];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Lucian.HarassMenu.MinManaQ", true];

                public static bool IsAutoHarassEnabledFor(AIHeroClient unit) => MenuManager.MenuValues["Plugins.Lucian.HarassMenu.UseQ." + unit.Hero];

                public static bool IsAutoHarassEnabledFor(string championName) => MenuManager.MenuValues["Plugins.Lucian.HarassMenu.UseQ." + championName];
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.EnableLCIfNoEn"];

                public static int ScanRange => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.ScanRange", true];

                public static int AllowedEnemies => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.AllowedEnemies", true];

                public static bool UseQInLaneClear => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.UseQInLaneClear"];

                public static bool UseQInJungleClear => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.UseQInJungleClear"];

                public static bool UseWInJungleClear => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.UseWInJungleClear"];

                public static bool UseEInJungleClear => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.UseEInJungleClear"];

                public static int MinMinionsHitQ => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.MinMinionsHitQ", true];

                public static int MinManaQ => MenuManager.MenuValues["Plugins.Lucian.LaneClearMenu.MinManaQ", true];
            }

            internal static class Misc
            {
                public static bool EnableKillsteal => MenuManager.MenuValues["Plugins.Lucian.MiscMenu.EnableKillsteal"];

                /// <summary>
                /// 0 - "Always"
                /// 1 - "After autoattack only"
                /// </summary>
                public static int EUsageMode => MenuManager.MenuValues["Plugins.Lucian.MiscMenu.EUsageMode", true];
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady => MenuManager.MenuValues["Plugins.Lucian.DrawingsMenu.DrawSpellRangesWhenReady"];

                public static bool DrawQ => MenuManager.MenuValues["Plugins.Lucian.DrawingsMenu.DrawQ"];

                public static bool DrawR => MenuManager.MenuValues["Plugins.Lucian.DrawingsMenu.DrawR"];

                public static bool DrawInfo => MenuManager.MenuValues["Plugins.Lucian.DrawingsMenu.DrawInfo"];
            }
        }

        protected class Damage
        {
            public static float GetSingleRShotDamage(AIHeroClient unit)
            {
                int[] qDamages = {0, 20, 35, 50};

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, qDamages[R.Level] + (Player.Instance.FlatPhysicalDamageMod * 0.2f + Player.Instance.FlatMagicDamageMod * 0.1f));
            }
        }
    }
}