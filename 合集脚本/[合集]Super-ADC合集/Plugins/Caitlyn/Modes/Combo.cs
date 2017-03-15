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

namespace Marksman_Master.Plugins.Caitlyn.Modes
{
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using Utils;

    internal class Combo : Caitlyn
    {
        public static void Execute()
        {
            if (Settings.Combo.UseE && E.IsReady() && !HasAutoAttackRangeBuffOnChamp)
            {
                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x => x.IsValidTargetCached(E.Range) && (x.IsMovingTowards(Player.Instance) || Player.Instance.IsInRangeCached(x, BasicAttackRange-150))))
                {
                    foreach (
                        var ePrediciton in
                            StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                                x =>
                                    x.IsValidTargetCached(E.Range) && Player.Instance.IsInAutoAttackRange(x) &&
                                    (x.IsMovingTowards(Player.Instance) || Player.Instance.IsInRangeCached(x, BasicAttackRange - 150)))
                                .Where(target => target.DistanceCached(Player.Instance) < BasicAttackRange - 150)
                                .Select(target => E.GetPrediction(target))
                                .Where(
                                    ePrediciton =>
                                        (ePrediciton.HitChancePercent >= Settings.Combo.EHitChancePercent) &&
                                        !GetDashEndPosition(ePrediciton.CastPosition).IsVectorUnderEnemyTower()))
                    {
                        E.Cast(ePrediciton.CastPosition);
                        return;
                    }
                }

                var possibleTargets =
                       StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                           x => x.IsValidTarget(E.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield());

                var eTarget = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if (eTarget != null)
                {
                    var ePrediciton = E.GetPrediction(eTarget);

                    if ((ePrediciton.HitChancePercent >= Settings.Combo.EHitChancePercent) && !GetDashEndPosition(ePrediciton.CastPosition).IsVectorUnderEnemyTower())
                    {
                        var damage = Player.Instance.GetSpellDamageCached(eTarget, SpellSlot.E);

                        var endPos = GetDashEndPosition(ePrediciton.CastPosition);

                        var predictiedUnitPosition = eTarget.Position.Extend(eTarget.Path.Last(), eTarget.MoveSpeed * 0.5f * 0.35f);
                        var unitPosafterAfter = predictiedUnitPosition.Extend(eTarget.Path.Last(), eTarget.MoveSpeed * 0.25f);

                        if (endPos.IsInRange(predictiedUnitPosition, 1300))
                            damage += Damage.GetHeadShotDamage(eTarget);

                        if (Q.IsReady() && endPos.IsInRange(unitPosafterAfter, 1200))
                            damage += Player.Instance.GetSpellDamageCached(eTarget, SpellSlot.Q);

                        if ((damage > eTarget.TotalHealthWithShields()) || endPos.IsInRange(eTarget, BasicAttackRange - 100) || (eTarget.IsMelee && eTarget.IsValidTarget(400)))
                        {
                            E.Cast(ePrediciton.CastPosition);
                            return;
                        }
                    }
                }
            }
            
            if (Settings.Combo.UseW && W.IsReady() && !IsPreAttack)
            {
                var possibleTargets =
                       StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                           x => x.IsValidTargetCached(700) && !x.HasUndyingBuffA() && !x.HasSpellShield() && !x.Position.IsVectorUnderEnemyTower());

                if (
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero)
                        .Any(
                            x =>
                                x.IsValidTargetCached() && x.IsMelee && (x.DistanceCached(Player.Instance) <= 500) &&
                                Player.Instance.IsInRangeCached(x, BasicAttackRange) &&
                                x.IsMovingTowards(Player.Instance, 400)) && IsValidWCast(Player.Instance.ServerPosition))
                {
                    W.Cast(Player.Instance.ServerPosition);
                    return;
                }

                var wTarget = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if (wTarget != null)
                {
                    var wPrediction = W.GetPrediction(wTarget);

                    if ((wPrediction.HitChancePercent >= Settings.Combo.WHitChancePercent) &&
                        (wPrediction.CastPosition.DistanceCached(wTarget) > 50) &&
                        IsValidWCast(wPrediction.CastPosition))
                    {
                        W.Cast(wPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (Settings.Combo.UseQ && !IsPreAttack && Q.IsReady() && !Player.Instance.Position.IsVectorUnderEnemyTower() && !HasAutoAttackRangeBuffOnChamp)
            {
                if (!StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x=> x.IsValidTargetCached() && Player.Instance.IsInRangeCached(x, BasicAttackRange)))
                {
                    var possibleTargets =
                        StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                            x => x.IsValidTargetCached(Q.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield());
                    
                    var qTarget = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                    if (qTarget != null)
                    {
                        var hitchance = 80;

                        if (((qTarget.HealthPercent <= 25) && (Player.Instance.ManaPercent >= 55)) || (Player.Instance.ManaPercent >= 85))
                            hitchance = 65;

                        if(Q.CastMinimumHitchance(qTarget, hitchance))
                            return;
                    }
                }
            }

            if (!Settings.Combo.UseR || IsPreAttack || !R.IsReady() ||
                Player.Instance.Position.IsVectorUnderEnemyTower())
                return;

            {
                var possibleTargets =
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTarget(R.Range) &&
                            (x.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(x, SpellSlot.R)) &&
                            !x.HasUndyingBuffA() && !x.HasSpellShield() &&
                            !EntityManager.Heroes.Enemies.Where(b => b.NetworkId != x.NetworkId)
                                .Any(
                                    c =>
                                        c.IsValidTarget() &&
                                        new Geometry.Polygon.Rectangle(Player.Instance.Position, x.Position, 400)
                                            .IsInside(c.ServerPosition)));

                var rTarget = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if (rTarget == null)
                    return;

                if (Q.IsReady() && rTarget.IsValidTargetCached(Q.Range) &&
                    (rTarget.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(rTarget, SpellSlot.Q)))
                    return;

                if (
                    (Player.Instance.CountEnemyHeroesInRangeWithPrediction(
                         (int) (Player.Instance.GetAutoAttackRange() + 100), 1200) == 0) && !IsPreAttack)
                {
                    R.Cast(rTarget);
                }
            }
        }
    }
}