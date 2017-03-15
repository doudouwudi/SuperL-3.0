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

using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Spells;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Ashe.Modes
{
    internal class Combo : Ashe
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ &&
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, 
                    x => x.IsValidTargetCached(Player.Instance.GetAutoAttackRange() - 50) && !IsPreAttack).Any())
            {
                Q.Cast();
            }

            if (W.IsReady() && Settings.Combo.UseW && (Player.Instance.Mana - 50 > (R.IsReady() ? 140 : 40)))
            {
                var possibleTargets =
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                        {
                            if (!x.IsValidTargetCached(W.Range))
                                return false;

                            var wPred = GetWPrediction(x);

                            if (wPred == null)
                                return false;

                            return !x.HasSpellShield() && wPred.HitChance >= HitChance.Medium;
                        })
                        .ToList();

                if (possibleTargets.Any() && !IsPreAttack)
                {
                    var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                    if (target != null)
                    {
                        var wPrediction = GetWPrediction(target);

                        if (wPrediction != null && wPrediction.HitChance >= HitChance.Medium)
                        {
                            W.Cast(wPrediction.CastPosition);
                        }
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE)
            {
                foreach (var source in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x=> !x.IsDead && !x.IsZombie && x.IsValid && x.IsUserInvisibleFor(500)))
                {
                    var data = source.GetVisibilityTrackerData();

                    if (data.LastHealthPercent < 25 && data.LastPosition.DistanceCached(Player.Instance) < 3000)
                    {
                        E.Cast(data.LastPath);
                    }
                }
            }

            if (R.IsReady() && Settings.Combo.UseR)
            {
                var target = TargetSelector.GetTarget(Settings.Combo.RMaximumRange, DamageType.Physical);

                if (target != null && !target.IsUnderTurret() && !target.HasSpellShield() && !target.HasUndyingBuffA() && target.DistanceCached(Player.Instance) > Settings.Combo.RMinimumRange)
                {
                    if (target.TotalHealthWithShields(true) < Player.Instance.GetAutoAttackDamageCached(target, true) * 2 && Player.Instance.IsInAutoAttackRange(target))
                        return;

                    var damage = 0f;
                    var wPred = GetWPrediction(target);

                    if ((Player.Instance.Mana > 200) && target.IsValidTargetCached(W.Range))
                    {
                        damage = Player.Instance.GetSpellDamageCached(target, SpellSlot.R) +
                                 (wPred != null && wPred.HitChance >= HitChance.Medium ? Player.Instance.GetSpellDamageCached(target, SpellSlot.W) : 0) +
                                 Player.Instance.GetAutoAttackDamageCached(target, true)*2.5f;
                    }
                    else if (Player.Instance.Mana > 150 && target.IsValidTargetCached(W.Range))
                        damage = Player.Instance.GetSpellDamageCached(target, SpellSlot.R) +
                                 Player.Instance.GetAutoAttackDamageCached(target, true)*2.5f;

                   var rPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
                    {
                        CollisionTypes = new HashSet<CollisionType> { Prediction.Manager.PredictionSelected == "ICPrediction" ? CollisionType.AiHeroClient : CollisionType.ObjAiMinion },
                        Delay = .25f,
                        From = Player.Instance.Position,
                        Radius = 120,
                        Range = Settings.Combo.RMaximumRange,
                        RangeCheckFrom = Player.Instance.Position,
                        Speed = R.Speed,
                        Target = target,
                        Type = SkillShotType.Linear
                    });

                    if (damage > target.TotalHealthWithShields() && (rPrediction.HitChancePercent >= 65))
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
            }
        }
    }
}