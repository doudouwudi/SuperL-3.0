#region Licensing
// ---------------------------------------------------------------------
// <copyright file="LaneClear.cs" company="EloBuddy">
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
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Kalista.Modes
{
    internal class LaneClear : Kalista
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.JungleLaneClear.UseQ && !Player.Instance.IsDashing() &&
                (Player.Instance.ManaPercent >= Settings.JungleLaneClear.MinManaForQ))
            {
                var minions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion,
                        x => x.Health < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)).ToList();

                if (!minions.Any() || Player.Instance.IsDashing())
                    return;

                foreach (var minion in minions.Where(x =>
                {
                    if (x == null || (x.Health > Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)))
                        return false;

                    var prediction = Q.GetPrediction(x);

                    return prediction != null && prediction.HitChance >= HitChance.Medium;
                }))
                {
                    if (Settings.JungleLaneClear.MinMinionsForQ == 1)
                    {
                        Q.Cast(minion.ServerPosition);
                        break;
                    }

                    var collisionableObjects =
                        StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion,
                            x => x.IsValidTargetCached(Q.Range) &&
                                 new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).Points.Any(
                                     b => new Geometry.Polygon.Rectangle(Player.Instance.Position,
                                         Player.Instance.Position.Extend(minion.Position,
                                             minion.DistanceCached(Player.Instance) >= Q.Range ? 0 : Q.Range).To3D(), 40)
                                         .IsInside(b)) && x.NetworkId != minion.NetworkId)
                            .OrderBy(x => x.DistanceCached(Player.Instance))
                            .ToList();

                    var count = 1;

                    Obj_AI_Base lastMinion = null;

                    for (int i = 0, lenght = collisionableObjects.Count; i < lenght; i++)
                    {
                        if (collisionableObjects[i].Health > Player.Instance.GetSpellDamageCached(collisionableObjects[i], SpellSlot.Q))
                            continue;

                        count++;
                        lastMinion = collisionableObjects[i];

                        if ((i + 1 < lenght) && (collisionableObjects[i + 1].Health > Player.Instance.GetSpellDamageCached(collisionableObjects[i + 1], SpellSlot.Q)))
                        {
                            break;
                        }
                    }

                    if ((count < Settings.JungleLaneClear.MinMinionsForQ) || lastMinion == null)
                        continue;

                    Q.Cast(lastMinion.ServerPosition);
                    break;
                }
            }

            if (!E.IsReady() || !Settings.JungleLaneClear.UseE || (Player.Instance.ManaPercent < Settings.JungleLaneClear.MinManaForE))
                return;
            {
                var minions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion,
                    x => x.IsValidTargetCached(E.Range) && Damage.IsTargetKillableByRend(x) &&
                         (Prediction.Health.GetPrediction(x, 250) > 10));

                if (minions.Count() >= Settings.JungleLaneClear.MinMinionsForE)
                {
                    E.Cast();
                }
            }
        }
    }
}