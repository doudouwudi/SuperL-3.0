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
using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.MissFortune.Modes
{
    internal class Combo : MissFortune
    {
        public static void Execute()
        {
            if(RCasted)
                return;

            if (Settings.Combo.UseW && W.IsReady() && IsPreAttack &&
                (Player.Instance.Mana - WMana > (R.IsReady() ? RMana + QMana[Q.Level] : QMana[Q.Level])))
            {
                if ((Orbwalker.LastTarget != null) && (Orbwalker.LastTarget.GetType() == typeof (AIHeroClient)) && !HasWBuff)
                {
                    W.Cast();
                }
            }

            if (Q.IsReady() && Settings.Combo.UseQ && !IsPreAttack &&
                (Player.Instance.Mana - QMana[Q.Level] > (R.IsReady() ?  RMana + WMana : WMana)))
            {
                var qTarget = TargetSelector.GetTarget(Q.Range + (Settings.Misc.BounceQFromMinions ? 420 : 0),
                    DamageType.Physical);

                if (qTarget != null)
                {
                    if (Settings.Misc.BounceQFromMinions)
                    {
                        var minion = GetQMinion(qTarget);
                        if (minion != null)
                        {
                            Q.Cast(minion);
                            return;
                        }
                        if (qTarget.IsValidTargetCached(Q.Range))
                        {
                            Q.Cast(qTarget);
                            return;
                        }
                    }
                    else if (qTarget.IsValidTargetCached(Q.Range))
                    {
                        Q.Cast(qTarget);
                        return;
                    }
                }
            }

            if (Settings.Combo.UseE && E.IsReady() && !IsPreAttack &&
                (Player.Instance.Mana - EMana > (R.IsReady() ? RMana + WMana + QMana[Q.Level] : WMana + QMana[Q.Level])))
            {
                E.CastIfItWillHit(3);

                var target = E.GetTarget();

                if (target != null)
                {
                    E.CastMinimumHitchance(target, HitChance.High);
                }
            }


            if (!R.IsReady() || !Settings.Combo.UseR || IsPreAttack || IsAfterAttack ||
                new Geometry.Polygon.Circle(Player.Instance.Position, Player.Instance.BoundingRadius).Points.Any(x => x.To3D().IsVectorUnderEnemyTower()))
                return;

            if (Player.Instance.CountEnemiesInRangeCached(820) == 1)
            {
                var target = R.GetTarget();

                if ((target != null) && (target.TotalHealthWithShields() > GetComboDamage(target, 3)) &&
                    Player.Instance.IsInRangeCached(target, 600))
                {
                    var waves = (int) Math.Floor(target.Health/Player.Instance.GetSpellDamageCached(target, SpellSlot.R));

                    if ((waves < RWaves[R.Level]) && !Player.Instance.IsMoving && !IsPreAttack && !IsAfterAttack)
                    {
                        R.Cast(target.ServerPosition);
                        return;
                    }
                }
            }

            if (Player.Instance.CountEnemiesInRangeCached(825) > 0)
                    return;

            if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).Any(x => x.IsValidTargetCached(R.Range)))
            {
                var wavesNeeded = new Dictionary<int, Tuple<int, int>>();

                foreach (var target in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(R.Range)))
                {
                    wavesNeeded[target.NetworkId] =
                        new Tuple<int, int>(
                            (int) Math.Floor(target.Health/Player.Instance.GetSpellDamageCached(target, SpellSlot.R)),
                            GetObjectsWithinRRange<AIHeroClient>(target.Position)
                                .Count(x => x.IsValidTargetCached(R.Range)));
                }

                if (wavesNeeded.Any(x => x.Value.Item1 < RWaves[R.Level])) // can be killed by x amount of waves
                {
                    foreach (var tuple in wavesNeeded)
                    {
                        var enemy = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).First(x => x.NetworkId == tuple.Key);

                        if (!enemy.IsValidTargetCached(1280))
                            continue;

                        var dist = enemy.Distance(Player.Instance);

                        if ((dist < 800) && (tuple.Value.Item1 < 10) && !Player.Instance.IsMoving && !IsPreAttack &&
                            !IsAfterAttack)
                        {
                            R.CastMinimumHitchance(enemy, HitChance.High);
                            break;
                        }
                        if ((dist < 1000) && (dist > 800) && (tuple.Value.Item1 < 8) && !Player.Instance.IsMoving &&
                            !IsPreAttack && !IsAfterAttack)
                        {
                            R.CastMinimumHitchance(enemy, HitChance.High);
                            break;
                        }
                        if ((dist < 1200) && (dist > 1000) && (tuple.Value.Item1 <= 4) && !Player.Instance.IsMoving &&
                            !IsPreAttack && !IsAfterAttack)
                        {
                            R.CastMinimumHitchance(enemy, HitChance.High);
                            break;
                        }

                        if (!(dist < 1300) || !(dist > 1200) || (tuple.Value.Item1 > 2) || Player.Instance.IsMoving ||
                            IsPreAttack || IsAfterAttack)
                            continue;

                        R.CastMinimumHitchance(enemy, HitChance.High);
                        break;
                    }
                }
                else if (R.IsReady() && wavesNeeded.Any(x => x.Value.Item2 >= Settings.Combo.RWhenXEnemies))
                {
                    var enemy =
                        StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).First(
                            x =>
                                x.NetworkId ==
                                wavesNeeded.FirstOrDefault(l => l.Value.Item2 >= Settings.Combo.RWhenXEnemies).Key);

                    if (enemy.IsValidTarget(1280) && !Player.Instance.IsMoving && !IsPreAttack && !IsAfterAttack)
                    {
                        R.CastMinimumHitchance(enemy, HitChance.High);
                    }
                }
            }
        }
    }
}