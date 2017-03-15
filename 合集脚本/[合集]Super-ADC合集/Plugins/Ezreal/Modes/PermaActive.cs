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

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Ezreal.Modes
{
    internal class PermaActive : Ezreal
    {
        public static void Execute()
        {
            if (Settings.Misc.EnableKillsteal)
            {
                var enemies =
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x => x.IsValidTarget(Q.Range) && (x.HealthPercent < 20) && !x.HasUndyingBuffA() &&
                             !x.HasSpellShield()).ToList();

                if (enemies.Any() && !IsPreAttack)
                {
                    if (Q.IsReady())
                    {
                        foreach (var enemy in enemies.Where(x=> x.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)))
                        {
                            Q.CastMinimumHitchance(enemy, 65);
                            return;
                        }
                    } else if (W.IsReady())
                    {
                        foreach (var enemy in enemies.Where(x => x.TotalHealthWithShields(true) < Player.Instance.GetSpellDamageCached(x, SpellSlot.W)))
                        {
                            W.CastMinimumHitchance(enemy, 65);
                            return;
                        }
                    }
                }
            }

            if (Settings.Combo.WEComboKeybind && W.IsReady() && E.IsReady())
            {
                W.Cast(Player.Instance.Position.Extend(Game.CursorPos, E.Range).To3D());
                return;
            }

            if (Q.IsReady() && !Player.Instance.IsRecalling() && !IsPreAttack && Settings.Misc.KeepPassiveStacks && (GetPassiveBuffAmount >= 4) && (GetPassiveBuff.EndTime - Game.Time < 1.5f) && (GetPassiveBuff.EndTime - Game.Time > 0.3f) && (Player.Instance.ManaPercent > 25) && !StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x=>x.IsValidTargetCached(Q.Range)))
            {
                foreach (var minion in StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions, x=>x.IsValidTarget(Q.Range)))
                {
                    Q.Cast(minion);
                    return;
                }
            }

            if (W.IsReady() && !IsPreAttack && !Player.Instance.IsRecalling() && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Settings.Harass.UseW && (Player.Instance.ManaPercent >= Settings.Harass.MinManaQ))
            {
                foreach (var target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x =>
                        Settings.Harass.IsAutoHarassEnabledFor(x) && x.IsValidTargetCached(W.Range) &&
                        !x.HasUndyingBuffA() && !x.HasSpellShield()).OrderByDescending(x => Player.Instance.GetSpellDamageCached(x, SpellSlot.W)))
                {
                    W.CastMinimumHitchance(target, 75);
                    return;
                }
            }

            if (Q.IsReady() && !IsPreAttack && Settings.Harass.UseQ && (Player.Instance.ManaPercent >= Settings.Harass.MinManaQ) &&
                !Player.Instance.HasSheenBuff() && (Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange()) == 0))
            {
                var immobileEnemies = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => Settings.Harass.IsAutoHarassEnabledFor(x) && x.IsValidTargetCached(Q.Range) && !x.HasUndyingBuffA() &&
                        !x.HasSpellShield() && (x.GetMovementBlockedDebuffDuration() > 0.3f)).ToList();

                if (immobileEnemies.Any() && !IsPreAttack)
                {
                    foreach (
                        var qPrediction in
                            from immobileEnemy in immobileEnemies.OrderByDescending(
                                    x => Player.Instance.GetSpellDamageCached(x, SpellSlot.Q))
                            where (immobileEnemy.GetMovementBlockedDebuffDuration() > Player.Instance.Distance(immobileEnemy)/Q.Speed + 0.25f) &&
                                  !Player.Instance.HasSheenBuff()
                            select Q.GetPrediction(immobileEnemy)
                            into qPrediction
                            where qPrediction.HitChancePercent > 60
                            select qPrediction)
                    {
                        Q.Cast(qPrediction.CastPosition);
                        return;
                    }
                }

                else if(!Player.Instance.IsRecalling() && !IsPreAttack && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    foreach (var target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x =>
                            Settings.Harass.IsAutoHarassEnabledFor(x) && x.IsValidTargetCached(Q.Range) &&
                            !x.HasUndyingBuffA() && !x.HasSpellShield()).OrderByDescending(x => Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)))
                    {
                        Q.CastMinimumHitchance(target, 75);
                        return;
                    }
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR)
                return;

            if (Player.Instance.CountEnemyHeroesInRangeWithPrediction(
                (int) (Player.Instance.GetAutoAttackRange() + 100), R.CastDelay) == 0)
            {
                var rKillable = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x =>
                        x.IsValidTarget(Settings.Misc.MaxRRangeKillsteal) && (x.CountAlliesInRangeCached(800) == 0) && !x.HasUndyingBuffA() && !x.HasSpellShield())
                    .ToList();

                foreach (var rPrediction in
                    from targ in rKillable
                    let health = targ.TotalHealthWithShields(true) - IncomingDamage.GetIncomingDamage(targ)
                    where health < Damage.GetRDamage(targ)
                    select R.GetPrediction(targ)
                    into rPrediction
                    where rPrediction.HitChancePercent >= 65
                    select rPrediction)
                {
                    R.Cast(rPrediction.CastPosition);
                    return;
                }
            }

            var t = TargetSelector.GetTarget(2500, DamageType.Physical);

            if ((t == null) || !Settings.Combo.RKeybind)
                return;

            var rPrediciton = R.GetPrediction(t);

            if (rPrediciton.HitChancePercent >= 65)
            {
                R.Cast(rPrediciton.CastPosition);
            }
        }
    }
}