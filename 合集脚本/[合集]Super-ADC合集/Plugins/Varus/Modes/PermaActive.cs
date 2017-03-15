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

namespace Marksman_Master.Plugins.Varus.Modes
{
    internal class PermaActive : Varus
    {
        public static void Execute()
        {
            if (Settings.Misc.EnableKillsteal)
            {
                if(Q.IsReady() && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x=>x.IsValidTargetCached(Q.Range)))
                {
                    foreach (var targ in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(Q.Range) &&
                                                                                                           !x.HasUndyingBuffA() && !x.HasSpellShield() &&
                                                                                                           (x.TotalHealthWithShields() <= Damage.GetQDamage(x) + Damage.GetWDamage(x))))
                    {
                        if (!Q.IsCharging)
                        {
                            if (!IsPreAttack && (Player.Instance.CountEnemiesInRangeCached(Player.Instance.GetAutoAttackRange()) <= 1))
                            {
                                Q.StartCharging();
                            }
                        }
                        if (!Q.IsCharging || (targ == null) || (targ.TotalHealthWithShields() > Damage.GetQDamage(targ) + Damage.GetWDamage(targ)))
                            continue;

                        var qPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                        {
                            CollisionTypes =
                                Prediction.Manager.PredictionSelected == "ICPrediction"
                                    ? new HashSet<CollisionType> { CollisionType.YasuoWall }
                                    : new HashSet<CollisionType> { CollisionType.AiHeroClient, CollisionType.ObjAiMinion },
                            Delay = 0,
                            From = Player.Instance.Position,
                            Radius = 70,
                            Range = Q.Range,
                            RangeCheckFrom = Player.Instance.Position,
                            Speed = Q.Speed,
                            Target = targ,
                            Type = SkillShotType.Linear
                        });

                        if (qPrediction.HitChancePercent >= 60)
                        {
                            Q.Cast(qPrediction.CastPosition);
                        }
                    }
                } else if (E.IsReady())
                {
                    foreach (
                        var targ in
                            StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                                x => (x != null) && x.IsValidTargetCached(E.Range) &&
                                    (x.TotalHealthWithShields() <= Player.Instance.GetSpellDamageCached(x, SpellSlot.E) + Damage.GetWDamage(x))))
                    {
                        E.CastMinimumHitchance(targ, HitChance.Medium);
                    }
                }
            }

            if (Q.IsReady() && Settings.Harass.AutoHarassWithQ && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && !Player.Instance.IsRecalling())
            {
                if (!Q.IsCharging && !IsPreAttack && !Orbwalker.ShouldWait && (Player.Instance.CountEnemyHeroesInRangeWithPrediction(Settings.Combo.QMinDistanceToTarget, 350) == 0) && !Player.Instance.Position.IsVectorUnderEnemyTower() && (Player.Instance.ManaPercent >= Settings.Harass.MinManaQ) &&
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x => x.IsValidTargetCached(Q.MaximumRange - 100) && Settings.Harass.IsAutoHarassEnabledFor(x)))
                {
                    Q.StartCharging();
                }
                else if (Q.IsCharging)
                {
                    foreach (var qPrediction in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x => (x != null) && x.IsValidTargetCached(Q.Range) && Settings.Harass.IsAutoHarassEnabledFor(x) &&
                             ((Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange()) > 0) ||
                              Q.IsFullyCharged))
                        .Select(target => Q.GetPrediction(target)).Where(qPrediction => qPrediction.HitChancePercent >= 60))
                    {
                        Q.Cast(qPrediction.CastPosition);
                    }
                }
            }

            if (!R.IsReady() || !Settings.Combo.RKeybind)
                return;

            var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            if (t == null)
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