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
namespace Marksman_Master.Plugins.Tristana.Modes
{
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using Utils;

    internal class Combo : Tristana
    {
        public static void Execute()
        {
            if (Q.IsReady() && IsPreAttack && Settings.Combo.UseQ)
            {
                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero)
                        .Any(x => x.IsValidTarget(Player.Instance.GetAutoAttackRange() - 50)))
                {
                    Q.Cast();
                }
            }

            if ((WTarget != null) && W.IsReady() && Settings.Combo.DoubleWKeybind)
            {
                var target =
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero)
                        .FirstOrDefault(x => x.NetworkId == WTarget.NetworkId);

                if (target != null)
                {
                    var wPrediction = W.GetPrediction(target);

                    if (wPrediction.HitChance >= HitChance.Medium)
                    {
                        WTarget = null;

                        W.Cast(wPrediction.CastPosition);
                    }
                }
                else
                {
                    WTarget = null;
                }
            }

            if (W.IsReady() && IsCatingW)
            {
                W.Cast(
                    Player.Instance.Position.Extend(WStartPos,
                        WStartPos.DistanceCached(Player.Instance) > 850
                            ? 850
                            : WStartPos.DistanceCached(Player.Instance)).To3D());
                IsCatingW = false;
            }

            if (W.IsReady() && Settings.Combo.UseW && R.IsReady() && Settings.Combo.UseR &&
                (Player.Instance.Mana - 160 > 90) && (Player.Instance.HealthPercent > 25))
            {
                var target = TargetSelector.GetTarget(900, DamageType.Physical);

                if ((target != null) && (target.CountEnemiesInRangeCached(500) == 1) &&
                    (target.DistanceCached(Player.Instance) > R.Range))
                {
                    var damage = IncomingDamage.GetIncomingDamage(target) + Damage.GetRDamage(target) +
                                 Damage.GetEPhysicalDamage(target);

                    if (HasExplosiveChargeBuff(target) && (target.Health < damage))
                    {
                        var wPrediction = W.GetPrediction(target);

                        if (wPrediction.HitChance >= HitChance.Medium)
                        {
                            IsCatingW = true;
                            Core.DelayAction(() => IsCatingW = false, 2000);
                            WStartPos = Player.Instance.Position;

                            W.Cast(wPrediction.CastPosition);
                        }
                    }
                }
            }

            if (E.IsReady() && IsPreAttack && Settings.Combo.UseE)
            {
                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => x.IsValidTargetCached(E.Range + 200) && Settings.Combo.IsEnabledFor(x));

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);
                var target2 = TargetSelector.GetTarget(E.Range, DamageType.Physical);

                if ((target2 != null) && Settings.Combo.IsEnabledFor(target) &&
                    (target2.TotalHealthWithShields() <
                     Damage.GetEPhysicalDamage(target2, 2) + Player.Instance.GetAutoAttackDamageCached(target2)))
                {
                    E.Cast(target2);
                }
                else if ((target != null) && Settings.Combo.IsEnabledFor(target) && target.IsValidTargetCached(E.Range) &&
                         Player.Instance.IsInRangeCached(target, Player.Instance.GetAutoAttackRange() - 50))
                {
                    E.Cast(target);
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR || !Settings.Combo.UseRVsMelees ||
                (Player.Instance.HealthPercent > 25) || !StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero)
                    .Any(x => x.IsMelee && x.IsValidTarget(500) && (x.HealthPercent > 50)))
                return;

            foreach (
                var enemy in
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsMelee && x.IsMovingTowards(Player.Instance, 500) && x.IsValidTarget(500) &&
                            (x.HealthPercent > 50))
                        .OrderByDescending(TargetSelector.GetPriority)
                        .ThenBy(x => x.DistanceCached(Player.Instance)))
            {
                R.Cast(enemy);
            }
        }
    }
}