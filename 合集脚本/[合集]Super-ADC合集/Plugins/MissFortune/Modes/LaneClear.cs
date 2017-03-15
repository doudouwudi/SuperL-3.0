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

namespace Marksman_Master.Plugins.MissFortune.Modes
{
    using Utils;

    internal class LaneClear : MissFortune
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies ||
                   (Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <=
                    Settings.LaneClear.AllowedEnemies);
        }

        public static void Execute()
        {
            var laneMinions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTarget(E.Range)).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (Settings.LaneClear.UseQInLaneClear && Q.IsReady() &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ) && (laneMinions.Count(x=>x.IsValidTarget(Q.Range)) >= 2) && !IsPreAttack)
            {
                if (laneMinions.Any(x => x.Health < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)))
                {
                    foreach (var objAiMinion in laneMinions.Where(x => (x.Health < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)) && GetObjectsWithinQBounceRange<Obj_AI_Minion>(x.Position).Any(b=>b.IsValidTarget() && (b.NetworkId != x.NetworkId)) && (Prediction.Health.GetPrediction(x, (int)(x.Distance(Player.Instance) / 1400 * 1000 + 250)) > 20)))
                    {
                        Q.Cast(objAiMinion);
                        break;
                    }
                }
            }

            if (Settings.LaneClear.UseWInLaneClear && W.IsReady() &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW) && (laneMinions.Count(x => x.IsValidTarget(Q.Range)) >= 2) && IsPreAttack)
            {
                W.Cast();
            }

            if (!Settings.LaneClear.UseEInLaneClear || !E.IsReady() ||
                !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaE) ||
                (laneMinions.Count(x => x.IsValidTarget(E.Range)) < 2))
                return;

#pragma warning disable 618
            var farmLocation = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(laneMinions, E.Width, (int) E.Range, E.CastDelay, E.Speed, Player.Instance.ServerPosition.To2D());
#pragma warning restore 618

            if (farmLocation.HitNumber >= 3)
            {
                E.Cast(farmLocation.CastPosition);
            }
        }
    }
}