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
using Marksman_Master.Utils;
using SharpDX;

namespace Marksman_Master.Plugins.Ashe.Modes
{
    internal class LaneClear : Ashe
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || Player.Instance.CountEnemiesInRangeCached(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            var laneMinions =
                StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTargetCached(W.Range))
                    .ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (Q.IsReady() && Settings.LaneClear.UseQInLaneClear &&
                Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ && IsPreAttack && laneMinions.Count > 3)
            {
                Q.Cast();
            }

            if (!W.IsReady() || !Settings.LaneClear.UseWInLaneClear ||
                !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW) || laneMinions.Count <= 3)
                return;

            var bestPos = W.GetBestLinearCastPosition(laneMinions);

            if (bestPos.CastPosition != Vector3.Zero)
            {
                W.Cast(bestPos.CastPosition);
            }
        }
    }
}
