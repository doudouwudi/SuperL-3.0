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

namespace Marksman_Master.Plugins.Jinx.Modes
{
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using Utils;

    internal class Combo : Jinx
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ && !IsPreAttack)
            {
                var validTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => !x.IsZombie && x.IsValidTargetCached(GetRealRocketLauncherRange()) && !x.HasUndyingBuffA());

                var target = TargetSelector.GetTarget(validTargets, DamageType.Physical);

                if (target != null)
                {
                    if (Player.Instance.IsInRangeCached(target, GetRealMinigunRange()) && HasRocketLauncher &&
                        (target.TotalHealthWithShields() > Player.Instance.GetAutoAttackDamageCached(target)*2.2f))
                    {
                        Q.Cast();
                        return;
                    }

                    if (!Player.Instance.IsInRangeCached(target, GetRealMinigunRange()) &&
                        Player.Instance.IsInRangeCached(target, GetRealRocketLauncherRange()) && !HasRocketLauncher)
                    {
                        Q.Cast();
                        return;
                    }
                    if (HasMinigun && (GetMinigunStacks >= 2) &&
                        (target.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamageCached(target)*2.2f) &&
                        (target.TotalHealthWithShields() > Player.Instance.GetAutoAttackDamageCached(target)*2f))
                    {
                        Q.Cast();
                        return;
                    }
                }
            }

            if (W.IsReady() && Settings.Combo.UseW &&
                (Player.Instance.CountEnemiesInRangeCached(Settings.Combo.WMinDistanceToTarget) == 0) &&
                !Player.Instance.Position.IsVectorUnderEnemyTower() &&
                (Player.Instance.Mana - (50 + 10*(W.Level - 1)) > (R.IsReady() ? 100 : 50)))
            {
                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => x.IsValidTargetCached(W.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield());

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                var orbwalkerTarget = Orbwalker.GetTarget();

                if ((orbwalkerTarget != null) && (orbwalkerTarget.GetType() == typeof (AIHeroClient)))
                {
                    var wt = orbwalkerTarget as AIHeroClient;

                    if ((wt != null) && wt.IsValidTargetCached(W.Range) && !wt.HasUndyingBuffA() && !wt.HasSpellShield())
                    {
                        var wPrediction = W.GetPrediction(wt);

                        if (wPrediction.HitChance == HitChance.High)
                        {
                            W.Cast(wPrediction.CastPosition);
                            return;
                        }
                    }
                }
                else if (target != null)
                {
                    var wPrediction = W.GetPrediction(target);

                    if (wPrediction.HitChance == HitChance.High)
                    {
                        W.Cast(wPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE && (Player.Instance.Mana - 50 > 100))
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                
                if (target != null)
                {
                    var ePrediction = E.GetPrediction(target);

                    if (((ePrediction.HitChancePercent >= 80) &&
                        (ePrediction.CastPosition.DistanceCached(target) > 150)) || ((ePrediction.HitChancePercent >= 50) &&
                        (ePrediction.CastPosition.DistanceCached(target) > 150) && target.IsMovingTowards(Player.Instance, 500)))
                    {
                        E.Cast(ePrediction.CastPosition);
                        return;
                    }
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR || Player.Instance.Position.IsVectorUnderEnemyTower())
                return;

            R.CastIfItWillHit(4, 60);

            Misc.PrintDebugMessage("AOE ULT");
        }
    }
}
