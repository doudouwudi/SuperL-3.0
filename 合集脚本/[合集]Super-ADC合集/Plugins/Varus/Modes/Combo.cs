#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Combo.cs" company="EloBuddy">
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
namespace Marksman_Master.Plugins.Varus.Modes
{
    using System.Linq;
    using System.Collections.Generic;
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Spells;
    using Utils;

    internal class Combo : Varus
    {
        public static void Execute()
        {
            RLogics();

            QLogics();

            if (!Settings.Combo.UseE || !E.IsReady() || IsPreAttack)
                return;

            if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Count(x => x.IsValidTargetCached(E.Range)) >= 2)
            {
                E.CastIfItWillHit();
            }

            var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                x => x.IsValidTargetCached(E.Range) && !x.HasSpellShield() && !x.HasUndyingBuffA() &&
                     (!Settings.Combo.UseEToProc || HasWDebuff(x) && GetWDebuff(x).Count == 3)).ToList();

            var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

            if (target != null)
            {
                E.CastMinimumHitchance(target, 60);
            }
        }

        private static void QLogics()
        {
            if (!Q.IsReady() || !Settings.Combo.UseQ)
                return;
            {
                var possibleTargets =
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTargetCached(Q.IsCharging ? Q.Range : Q.MaximumRange) && !x.HasSpellShield() &&
                            !x.HasUndyingBuffA()).ToList();

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if (target != null)
                {
                    if (!Q.IsCharging && !IsPreAttack &&
                        (possibleTargets.Any(
                            x => (x.TotalHealthWithShields() < Damage.GetQDamage(x) + Damage.GetWDamage(x)) &&
                                 (Player.Instance.CountEnemyHeroesInRangeWithPrediction((int)Player.Instance.GetAutoAttackRange(), 350) <= 1)) ||
                         (Player.Instance.CountEnemyHeroesInRangeWithPrediction(Settings.Combo.QMinDistanceToTarget, 350) == 0)))
                    {
                        Q.StartCharging();
                        return;
                    }
                }

                if (!Q.IsCharging)
                    return;

                if (target != null)
                {
                    var damage = Damage.GetQDamage(target);

                    if (HasWDebuff(target) && (GetWDebuff(target).EndTime - Game.Time > 0.25f + Player.Instance.DistanceCached(target) / Q.Speed))
                        damage += Damage.GetWDamage(target);

                    if ((damage < target.TotalHealthWithShields()) && (Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange()) == 0) && !Q.IsFullyCharged)
                        return;

                    if (Prediction.Manager.PredictionSelected == "ICPrediction")
                    {
                        var qPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                        {
                            CollisionTypes = new HashSet<CollisionType> { CollisionType.YasuoWall },
                            Delay = 0,
                            From = Player.Instance.Position,
                            Radius = 70,
                            Range = Q.Range,
                            RangeCheckFrom = Player.Instance.Position,
                            Speed = Q.Speed,
                            Target = target,
                            Type = SkillShotType.Linear
                        });

                        if (qPrediction.HitChancePercent >= 60)
                        {
                            Q.Cast(qPrediction.CastPosition);
                        }
                    }
                    else
                    {
                        var qPrediction = Q.GetPrediction(target);

                        if (qPrediction.HitChancePercent >= 60)
                        {
                            Q.Cast(qPrediction.CastPosition);
                        }
                    }
                }
                else if (Player.Instance.CountEnemiesInRangeCached(Player.Instance.GetAutoAttackRange()) >= 1)
                {
                    var t =
                        StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached())
                            .OrderBy(x => x.DistanceCached(Player.Instance))
                            .FirstOrDefault();

                    if (t != null)
                    {
                        Q.CastMinimumHitchance(t, 50);
                    }
                }
            }
        }

        private static void RLogics()
        {
            if (!Settings.Combo.UseR || !R.IsReady() || IsPreAttack)
                return;

            var possibleTargets =
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x =>
                    {
                        if (x.IsValidTargetCached(R.Range) && (x.TotalHealthWithShields() > Player.Instance.GetAutoAttackDamageCached(x, true) * 2) && Player.Instance.IsInAutoAttackRange(x))
                            return false;

                        return x.IsValidTargetCached(R.Range) && !x.HasSpellShield() && !x.HasUndyingBuffA() &&
                               (x.TotalHealthWithShields() < GetComboDamage(x));
                    }).ToList();

            var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

            if (target != null)
            {
                if (Prediction.Manager.PredictionSelected == "ICPrediction")
                {
                    var rPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        CollisionTypes = new HashSet<CollisionType> { CollisionType.YasuoWall, CollisionType.AiHeroClient },
                        Delay = .25f,
                        From = Player.Instance.Position,
                        Radius = R.Width,
                        Range = R.Range,
                        RangeCheckFrom = Player.Instance.Position,
                        Speed = R.Speed,
                        Target = target,
                        Type = SkillShotType.Linear
                    });

                    if (rPrediction.HitChancePercent >= 60)
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
                else
                {
                    var rPrediction = R.GetPrediction(target);

                    if (rPrediction.HitChancePercent >= 60)
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
            }
            else
            {
                var t = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).FirstOrDefault(
                    x => x.IsValidTargetCached(R.Range) && !x.HasSpellShield() && (x.CountEnemiesInRangeCached(850) >= 3));

                if(t == null)
                    return;

                if (Prediction.Manager.PredictionSelected == "ICPrediction")
                {
                    var rPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        CollisionTypes = new HashSet<CollisionType> { CollisionType.YasuoWall, CollisionType.AiHeroClient },
                        Delay = .25f,
                        From = Player.Instance.Position,
                        Radius = R.Width,
                        Range = R.Range,
                        RangeCheckFrom = Player.Instance.Position,
                        Speed = R.Speed,
                        Target = t,
                        Type = SkillShotType.Linear
                    });

                    if (rPrediction.HitChancePercent >= 60)
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
                else
                {
                    var rPrediction = R.GetPrediction(t);

                    if (rPrediction.HitChancePercent >= 60)
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
            }
        }
    }
}