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

namespace Marksman_Master.Plugins.Corki.Modes
{
    using Utils;

    internal class LaneClear : Corki
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies ||
                   (Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <=
                    Settings.LaneClear.AllowedEnemies);
        }

        public static void Execute()
        {
            var laneMinions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTarget(Player.Instance.GetAutoAttackRange() + 250)).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (Q.IsReady() && Settings.LaneClear.UseQ &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaToUseQ))
            {
#pragma warning disable 618
                var farmLoc = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(laneMinions.Where(x => x.Health < Damage.GetSpellDamage(x, SpellSlot.Q)), 250, 825, 250, 1000);
#pragma warning restore 618

                if (farmLoc.HitNumber >= Settings.LaneClear.MinMinionsKilledToUseQ)
                {
                    Q.Cast(farmLoc.CastPosition);
                }
            }

            if (E.IsReady() && Settings.LaneClear.UseE &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaToUseE))
            {
                if (laneMinions.Count(x => x.IsValidTarget(500)) >= 3)
                {
                    E.Cast();
                    return;
                }
            }

            if (!R.IsReady() || !Settings.LaneClear.UseR ||
                (Player.Instance.ManaPercent < Settings.LaneClear.MinManaToUseR) ||
                (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo < Settings.LaneClear.MinStacksToUseR))
                return;

            foreach (var target in from target in laneMinions.Where(x => x.IsValidTarget())
                let count = GetCollisionObjects<Obj_AI_Minion>(target).Count(x => x.IsValidTarget())
                where count >= Settings.LaneClear.MinMinionsHitToUseR
                select target)
            {
                R.Cast(target.ServerPosition);
                break;
            }
        }
    }
}