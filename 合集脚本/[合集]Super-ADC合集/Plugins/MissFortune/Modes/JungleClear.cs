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

namespace Marksman_Master.Plugins.MissFortune.Modes
{
    using Utils;

    internal class JungleClear : MissFortune
    {
        public static void Execute()
        {
            var jungleMinions = StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTarget(Player.Instance.GetAutoAttackRange())).ToList();

            if (!jungleMinions.Any())
                return;

            string[] allowedMonsters =
            {
                "SRU_Gromp", "SRU_Blue", "SRU_Red", "SRU_Razorbeak", "SRU_Krug", "SRU_Murkwolf", "Sru_Crab", "SRU_Crab",
                "SRU_RiftHerald", "SRU_Dragon_Fire", "SRU_Dragon_Earth", "SRU_Dragon_Air", "SRU_Dragon_Elder",
                "SRU_Dragon_Water", "SRU_Baron"
            };

            if (Settings.LaneClear.UseQInJungleClear && Q.IsReady() &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ) && !IsPreAttack)
            {
                if (jungleMinions.Any(x => x.Health < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)))
                {
                    foreach (
                        var objAiMinion in
                            jungleMinions.Where(
                                x =>
                                    (x.Health < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)) &&
                                    GetObjectsWithinQBounceRange<Obj_AI_Minion>(x.Position)
                                        .Any(b => b.IsValidTarget() && (b.NetworkId != x.NetworkId)) &&
                                    (Prediction.Health.GetPrediction(x, 600) > 25)))
                    {
                        Q.Cast(objAiMinion);
                        break;
                    }
                }
                else if(jungleMinions.Any(minion => allowedMonsters.Contains(minion.BaseSkinName) && (minion.Health > Player.Instance.GetAutoAttackDamageCached(minion, true) * 2)))
                {
                    Q.Cast(jungleMinions.FirstOrDefault(minion => allowedMonsters.Contains(minion.BaseSkinName)));
                }
            }

            if (Settings.LaneClear.UseWInJungleClear && W.IsReady() &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW) && ((jungleMinions.Count(x => x.IsValidTarget(Q.Range)) >= 2) || jungleMinions.Any(minion => allowedMonsters.Contains(minion.BaseSkinName) && (minion.Health > Player.Instance.GetAutoAttackDamageCached(minion, true) * 2))) && IsPreAttack)
            {
                W.Cast();
            }

            if (!Settings.LaneClear.UseEInJungleClear || !E.IsReady() ||
                !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaE) ||
                (jungleMinions.Count(x => x.IsValidTarget(E.Range)) < 2))
                return;

#pragma warning disable CS0618 // Type or member is obsolete
            var farmLocation = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(jungleMinions, E.Width, (int) E.Range,E.CastDelay, E.Speed, Player.Instance.ServerPosition.To2D());
#pragma warning restore CS0618 // Type or member is obsolete

            if (farmLocation.HitNumber >= 2)
            {
                E.Cast(farmLocation.CastPosition);
            }
        }
    }
}
