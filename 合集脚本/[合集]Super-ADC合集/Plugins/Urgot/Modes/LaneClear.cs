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

namespace Marksman_Master.Plugins.Urgot.Modes
{
    using Utils;

    internal class LaneClear : Urgot
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || (Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies);
        }

        public static void Execute()
        {
            if (!CanILaneClear())
                return;

            var laneMinions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTarget() && IsInQRange(x)).ToList();

            if (!laneMinions.Any())
            {
                return;
            }

            if (Q.IsReady() && Settings.LaneClear.UseQInLaneClear &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ))
            {
                foreach (var castPosition in laneMinions.Where(x =>
                {
                    if (Player.Instance.IsInAutoAttackRange(x) && (x.Health <= Player.Instance.GetAutoAttackDamageCached(x, true)))
                        return false;

                    if (!HasEDebuff(x) && Q.GetPrediction(x).Collision)
                        return false;

                    var prediction = Prediction.Health.GetPrediction(x,
                        Q.CastDelay + (int) (x.DistanceCached(Player.Instance)/Q.Speed*1000));

                    return (prediction > 0) && (prediction <= Player.Instance.GetSpellDamageCached(x, SpellSlot.Q));
                }).Select(x => HasEDebuff(x) ? x.Position : Q.GetPrediction(x).CastPosition))
                {
                    Player.Instance.Spellbook.CastSpell(SpellSlot.Q, castPosition);
                    return;
                }
            }

            if (!Settings.LaneClear.UseEInLaneClear || (Player.Instance.CountEnemyMinionsInRangeCached(900) <= 3))
                return;

            E.CastOnBestFarmPosition(1);
        }
    }
}