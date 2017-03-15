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
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Caitlyn.Modes
{
    internal class LaneClear : Caitlyn
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies ||
                   Player.Instance.CountEnemiesInRangeCached(Settings.LaneClear.ScanRange) <=
                   Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            if (!Settings.LaneClear.UseQInLaneClear || !Q.IsReady() ||
                (Player.Instance.ManaPercent < Settings.LaneClear.MinManaQ))
                return;

            var laneMinions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTargetCached(Q.Range)).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            foreach (var objAiMinion in from objAiMinion in laneMinions
                let polygon =
                    new Geometry.Polygon.Rectangle(objAiMinion.Position,
                        Player.Instance.Position.Extend(objAiMinion.Position, Q.Range).To3D(), 90)
                where
                    laneMinions.Count(
                        x => polygon.IsInside(x) && (x.Health < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q))) >=
                    Settings.LaneClear.MinMinionsKilledForQ
                select objAiMinion)
            {
                Q.Cast(objAiMinion);
                break;
            }
        }
    }
}