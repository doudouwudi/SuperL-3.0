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

namespace Marksman_Master.Plugins.Quinn.Modes
{
    internal class LaneClear : Quinn
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies ||
                   Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <=
                   Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            if (!Settings.LaneClear.UseQInLaneClear || !Q.IsReady() || Player.Instance.IsUnderTurret() ||
                !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ))
                return;

            var laneMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                Player.Instance.Position, Q.Range).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            foreach (var minion in laneMinions.Where(x=>x.IsValidTarget(Q.Range) && Q.GetPrediction(x).HitChance == HitChance.High).Where(minion => minion.CountEnemyMinionsInRange(200) >= Settings.LaneClear.MinMinionsKilledForQ))
            {
                Q.Cast(minion);
                break;
            }
        }
    }
}