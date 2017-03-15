#region Licensing
// ---------------------------------------------------------------------
// <copyright file="PermaActive.cs" company="EloBuddy">
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


using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Spells;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Jhin.Modes
{
    internal class PermaActive : Jhin
    {
        public static void Execute()
        {
            if (Settings.Misc.EnableKillsteal)
            {
                if (W.IsReady() && !IsCastingR)
                {
                    if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTarget(W.Range) && x.IsHPBarRendered &&
                            Damage.IsTargetKillableFromW(x)).Any())
                    {
                        foreach (var target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                                x => x.IsValidTargetCached(W.Range) && !x.IsDead && Damage.IsTargetKillableFromW(x) && !x.HasUndyingBuffA() && !x.HasSpellShield()))
                        {
                            var wPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                            {
                                Range = W.Range,
                                Target = target,
                                Speed = int.MaxValue,
                                RangeCheckFrom = Player.Instance.Position,
                                Delay = 1,
                                From = Player.Instance.Position,
                                Radius = W.Width,
                                Type = SkillShotType.Linear,
                                CollisionTypes = new HashSet<CollisionType> { Prediction.Manager.PredictionSelected == "ICPrediction" ? CollisionType.AiHeroClient : CollisionType.ObjAiMinion }
                            });

                            if (wPrediction.HitChance >= HitChance.Medium)
                            {
                                Player.CastSpell(SpellSlot.W, wPrediction.CastPosition);
                                return;
                            }
                        }
                    }
                    else if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(W.Range) && HasSpottedBuff(x)).Count() < 2)
                    {
                        foreach (
                            var target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                                    x => !x.IsDead && x.IsUserInvisibleFor(250) && !x.IsZombie && Damage.IsTargetKillableFromW(x)))
                        {
                            var data = target.GetVisibilityTrackerData();

                            if (!(Game.Time*1000 - data.LastVisibleGameTime*1000 < 2000) ||
                                !(data.LastHealthPercent > 0))
                                continue;

                            W.Cast(
                                data.LastPosition.Extend(data.LastPath,
                                    target.MoveSpeed*1 + (Game.Time*1000 - data.LastVisibleGameTime*1000)/1000).To3D());
                            break;
                        }
                    }
                }
            }
            if (E.IsReady() && Settings.Combo.UseE)
            {

                foreach (
                    var enemy in
                        StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                            x =>
                                x.IsValidTargetCached(E.Range) && x.Buffs.Any(
                                    m =>
                                        m.Name.ToLowerInvariant() == "zhonyasringshield" ||
                                        m.Name.ToLowerInvariant() == "bardrstasis")))
                {
                    E.Cast(enemy.ServerPosition);
                    break;
                }

                var ga =
                    ObjectManager.Get<Obj_GeneralParticleEmitter>()
                        .Where(
                            x =>
                                x.Name == "LifeAura.troy")
                        .ToList();

                if (ga.Any())
                {
                    foreach (var owner in ga.Select(objGeneralParticleEmitter => StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x => x.DistanceCached(objGeneralParticleEmitter) < 20).FirstOrDefault()).Where(owner => owner != null))
                    {
                        E.Cast(owner.ServerPosition);
                        break;
                    }
                }
            }

            if (!IsCastingR || !Settings.Combo.UseR || GetCurrentShootsRCount < 1)
                return;
            
            if ((Settings.Combo.RMode != 0 || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) &&
                (Settings.Combo.RMode != 1 || !Settings.Combo.RKeybind) && Settings.Combo.RMode != 2)
                return;
            
            if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x=> x.IsValidTargetCached(3700) && IsInsideRRange(x) && x.IsHPBarRendered).Any())
            {
                if (TargetSelector.SelectedTarget != null && IsInsideRRange(TargetSelector.SelectedTarget) && TargetSelector.SelectedTarget.IsValidTarget())
                {
                    var rPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        Range = 3500,
                        Target = TargetSelector.SelectedTarget,
                        Speed = 5000,
                        RangeCheckFrom = Player.Instance.Position,
                        Delay = .2f,
                        From = Player.Instance.Position,
                        Radius = 80,
                        Type = SkillShotType.Linear,
                        CollisionTypes = Prediction.Manager.PredictionSelected == "ICPrediction"
                            ? new HashSet<CollisionType> {CollisionType.AiHeroClient, CollisionType.YasuoWall}
                            : new HashSet<CollisionType> {CollisionType.ObjAiMinion}
                    });
                    if (rPrediction.HitChance >= HitChance.High)
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
                else
                {
                    var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTarget() && !x.IsZombie && !x.IsDead && IsInsideRRange(x) && !x.HasUndyingBuffA() &&
                            !x.HasSpellShield()
                            && Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                            {
                                Range = 3500,
                                Target = x,
                                Speed = 5000,
                                RangeCheckFrom = Player.Instance.Position,
                                Delay = .2f,
                                From = Player.Instance.Position,
                                Radius = 80,
                                Type = SkillShotType.Linear,
                                CollisionTypes = Prediction.Manager.PredictionSelected == "ICPrediction"
                                    ? new HashSet<CollisionType> {CollisionType.AiHeroClient, CollisionType.YasuoWall}
                                    : new HashSet<CollisionType> {CollisionType.ObjAiMinion}
                            }).HitChance != HitChance.Collision);

                    var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                    if (target == null)
                        return;

                    var rPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        Range = 3500,
                        Target = target,
                        Speed = 5000,
                        RangeCheckFrom = Player.Instance.Position,
                        Delay = .2f,
                        From = Player.Instance.Position,
                        Radius = 80,
                        Type = SkillShotType.Linear,
                        CollisionTypes =
                            Prediction.Manager.PredictionSelected == "ICPrediction"
                                ? new HashSet<CollisionType> {CollisionType.AiHeroClient, CollisionType.YasuoWall}
                                : new HashSet<CollisionType> {CollisionType.ObjAiMinion}
                    });

                    if (rPrediction.HitChance >= HitChance.High)
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
            }
            else if(Settings.Combo.EnableFowPrediction)
            {
                foreach (var enemy in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Where(x => !x.IsDead && !x.IsZombie && x.IsUserInvisibleFor(250)))
                {
                    var data = enemy.GetVisibilityTrackerData();

                    if ((Core.GameTickCount - data.LastVisibleGameTime*1000 > 2200) || (data.LastHealthPercent <= 0) || !IsInsideRRange(data.LastPosition))
                        continue;

                    var eta = data.LastPosition.Distance(Player.Instance) / 5000;

                    R.Cast(data.LastPosition.Extend(data.LastPath, enemy.MoveSpeed*eta).To3D());
                    return;
                }
            }
        }
    }
}