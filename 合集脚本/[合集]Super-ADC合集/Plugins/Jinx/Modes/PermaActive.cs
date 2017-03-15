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

namespace Marksman_Master.Plugins.Jinx.Modes
{
    using System;
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using Utils;

    internal class PermaActive : Jinx
    {
        public static void Execute()
        {
            if (W.IsReady() && Settings.Misc.WKillsteal && (Player.Instance.Mana - 90 > (R.IsReady() ? 130 : 30)) &&
                !Player.Instance.Position.IsVectorUnderEnemyTower() &&
                (Player.Instance.CountEnemiesInRangeCached(Settings.Combo.WMinDistanceToTarget) == 0))
            {
                if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(
                    x =>
                        x.IsValidTargetCached(W.Range) && !x.HasSpellShield() && !x.HasUndyingBuffA()))
                {
                    foreach (
                        var wPrediction in 
                        from enemy in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                            x => x.IsValidTargetCached(W.Range) && !x.HasSpellShield() && !x.HasUndyingBuffA())
                            let health = enemy.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(enemy)
                            let wDamage = Player.Instance.GetSpellDamageCached(enemy, SpellSlot.W)
                            let wPrediction = W.GetPrediction(enemy)
                            where (health <= wDamage) && (wPrediction.HitChance == HitChance.High)
                            select wPrediction)
                    {
                        W.Cast(wPrediction.CastPosition);
                    }
                }

                if (Settings.Harass.UseW && !IsPreAttack && (Player.Instance.ManaPercent >= Settings.Harass.MinManaW) &&
                    (Player.Instance.CountEnemiesInRangeCached(Settings.Combo.WMinDistanceToTarget) == 0))
                {
                    foreach (var wPrediction in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTargetCached(W.Range) && Settings.Harass.IsWHarassEnabledFor(x) &&
                            (x.Distance(Player.Instance) > GetRealRocketLauncherRange()))
                        .Where(enemy => enemy.IsValidTargetCached(W.Range))
                        .Select(enemy => W.GetPrediction(enemy))
                        .Where(wPrediction => wPrediction.HitChancePercent > 70))
                    {
                        W.Cast(wPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (R.IsReady() && Settings.Combo.UseR && !Player.Instance.Position.IsVectorUnderEnemyTower())
            {
                if (Settings.Combo.RKeybind)
                {
                    var target = TargetSelector.GetTarget(Settings.Combo.RRangeKeybind, DamageType.Physical);

                    if (target != null)
                    {
                        var rPrediciton = R.GetPrediction(target);

                        if (rPrediciton.HitChance == HitChance.High)
                        {
                            R.Cast(rPrediciton.CastPosition);
                            return;
                        }
                    }
                }
                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x =>
                    {
                        if (((x.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamageCached(x, true)*1.8f) &&
                             x.IsValidTarget(Player.Instance.GetAutoAttackRange())) ||
                            ((x.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(x, SpellSlot.W)) &&
                             x.IsValidTarget(W.Range) && W.IsReady()))
                        {
                            return false;
                        }
                        return x.IsValidTarget(Settings.Misc.RKillstealMaxRange) && (x.CountAlliesInRangeCached(800) == 0) && (x.TotalHealthWithShields() < Damage.GetRDamage(x));
                    });

                var t = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if ((t != null) && !t.HasUndyingBuffA() && (Player.Instance.CountEnemiesInRangeCached(550) == 0))
                {
                    if(t.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(t) <= 50)
                        return;

                    var rPrediction = R.GetPrediction(t);

                    if (rPrediction.HitChancePercent >= 65)
                    {
                        R.Cast(rPrediction.CastPosition);
                        Misc.PrintDebugMessage("KS ULT");
                    }
                }
            }
            
            if (!E.IsReady() || !Settings.Combo.AutoE || !(Player.Instance.Mana - 50 > 100))
                return;

            foreach (
                var enemy in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTargetCached(E.Range) &&
                            ((x.GetMovementBlockedDebuffDuration() > 0.7f) ||
                             x.Buffs.Any(
                                 m =>
                                     m.Name.Equals("zhonyasringshield", StringComparison.CurrentCultureIgnoreCase) ||
                                     m.Name.Equals("bardrstasis", StringComparison.CurrentCultureIgnoreCase)))))
            {
                if (enemy.Buffs.Any(m => m.Name.Equals("zhonyasringshield", StringComparison.CurrentCultureIgnoreCase) ||
                                         m.Name.Equals("bardrstasis", StringComparison.CurrentCultureIgnoreCase)))
                {
                    var buffTime = enemy.Buffs.FirstOrDefault(m => m.Name.Equals("zhonyasringshield", StringComparison.CurrentCultureIgnoreCase) ||
                                                                   m.Name.Equals("bardrstasis", StringComparison.CurrentCultureIgnoreCase));

                    if ((buffTime != null) && (buffTime.EndTime - Game.Time < 1) && (buffTime.EndTime - Game.Time > .3f) && enemy.IsValidTargetCached(E.Range))
                    {
                        E.Cast(enemy.ServerPosition);
                    }
                } else if (enemy.IsValidTargetCached(E.Range))
                {
                    E.Cast(enemy.ServerPosition);
                }
                Misc.PrintDebugMessage($"Name : {enemy.Hero} | Duration : {enemy.GetMovementBlockedDebuffDuration()}");
            }
        }
    }
}
