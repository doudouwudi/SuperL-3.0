#region Licensing
// ---------------------------------------------------------------------
// <copyright file="JungleClear.cs" company="EloBuddy">
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
using EloBuddy.SDK.Events;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Kalista.Modes
{
    internal class JungleClear : Kalista
    {
        public static void Execute()
        {
            if (!StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTargetCached(Q.Range)).Any())
                return;

            if (Q.IsReady() && Settings.JungleLaneClear.UseQ && !Player.Instance.IsDashing() &&
                Player.Instance.ManaPercent >= Settings.JungleLaneClear.MinManaForQ)
            {
                var minions =
                    StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTargetCached(Q.Range))
                        .ToList();

                if (!minions.Any())
                    return;

                string[] allowedMonsters =
                {
                    "SRU_Gromp", "SRU_Blue", "SRU_Red", "SRU_Razorbeak", "SRU_Krug", "SRU_Murkwolf", "Sru_Crab",
                    "SRU_Crab",
                    "SRU_RiftHerald", "SRU_Dragon_Fire", "SRU_Dragon_Earth", "SRU_Dragon_Air", "SRU_Dragon_Elder",
                    "SRU_Dragon_Water", "SRU_Baron"
                };

                if (minions.Any(
                        minion =>
                            allowedMonsters.Contains(minion.BaseSkinName) &&
                            (minion.Health > Player.Instance.GetAutoAttackDamageCached(minion) * 2)))
                {
                    Q.Cast(minions.FirstOrDefault(minion => allowedMonsters.Contains(minion.BaseSkinName)));
                }
            }

            if (!E.IsReady() || !Settings.JungleLaneClear.UseE ||
                (Player.Instance.ManaPercent < Settings.JungleLaneClear.MinManaForE))
                return;

            var killableMonsters = StaticCacheProvider.GetMinions(CachedEntityType.Monsters,
                x => x.IsValidTargetCached(E.Range) && Damage.IsTargetKillableByRend(x) &&
                     (Prediction.Health.GetPrediction(x, 250) > 10));

            if (killableMonsters.Count() >= Settings.JungleLaneClear.MinMinionsForE)
            {
                E.Cast();
            }
        }
    }
}
