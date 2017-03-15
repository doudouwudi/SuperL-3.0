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
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Varus.Modes
{
    internal class JungleClear : Varus
    {
        public static void Execute()
        {
            var jungleMinions =
                StaticCacheProvider.GetMinions(CachedEntityType.Monsters,
                    x => x.IsValidTargetCached(Player.Instance.GetAutoAttackRange())).ToList();

            if (!jungleMinions.Any())
                return;

            string[] allowedMonsters =
            {
                "SRU_Gromp", "SRU_Blue", "SRU_Red", "SRU_Razorbeak", "SRU_Krug", "SRU_Murkwolf", "Sru_Crab", "SRU_Crab",
                "SRU_RiftHerald", "SRU_Dragon_Fire", "SRU_Dragon_Earth", "SRU_Dragon_Air", "SRU_Dragon_Elder",
                "SRU_Dragon_Water", "SRU_Baron"
            };

            if (Q.IsReady() && Settings.LaneClear.UseQInLaneClear &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ) &&
                ((jungleMinions.Count >= Settings.LaneClear.MinMinionsHitQ) ||
                 allowedMonsters.Any(x => jungleMinions.Any(k => x.Contains(k.BaseSkinName)))))
            {
                if (!Q.IsCharging && !IsPreAttack &&
                    (EntityManager.MinionsAndMonsters.GetLineFarmLocation(jungleMinions, Q.Width, 1550).HitNumber >=
                     Settings.LaneClear.MinMinionsHitQ))
                {
                    Q.StartCharging();
                }
                else if (Q.IsCharging && Q.IsFullyCharged)
                {
                    var bigMonster =
                        jungleMinions.FirstOrDefault(k => allowedMonsters.Any(x => x.Contains(k.BaseSkinName)));

                    if (bigMonster != null)
                    {
                        Q.Cast(bigMonster);
                    }
                    else
                    {
                        Q.CastOnBestFarmPosition(1);
                    }
                }
            }

            if (!E.IsReady() || !Settings.LaneClear.UseEInLaneClear ||
                (Player.Instance.ManaPercent < Settings.LaneClear.MinManaE) ||
                ((jungleMinions.Count < Settings.LaneClear.MinMinionsHitQ) &&
                 !allowedMonsters.Any(x => jungleMinions.Any(k => x.Contains(k.BaseSkinName)))))
                return;

            if (jungleMinions.Count >= Settings.LaneClear.MinMinionsHitE)
            {
                E.CastOnBestFarmPosition(Settings.LaneClear.MinMinionsHitE);
            }
            else
            {
                var bigMonster = jungleMinions.FirstOrDefault(k => allowedMonsters.Any(x => x.Contains(k.BaseSkinName)));

                if (bigMonster != null)
                {
                    E.Cast(bigMonster);
                }
                else
                {
                    E.CastOnBestFarmPosition(1);
                }
            }
        }
    }
}