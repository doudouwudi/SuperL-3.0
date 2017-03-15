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

namespace Marksman_Master.Plugins.Twitch.Modes
{
    internal class LaneClear : Twitch
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || Player.Instance.CountEnemiesInRangeCached(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            var laneMinions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTargetCached(W.Range)).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (W.IsReady() && Settings.LaneClear.UseW && Player.Instance.ManaPercent >= Settings.LaneClear.WMinMana)
            {
                W.CastOnBestFarmPosition();
            }

            if (E.IsReady() && Settings.LaneClear.UseE && Player.Instance.ManaPercent >= Settings.LaneClear.EMinMana)
            {
                var minions =
                    laneMinions.Where(
                        minion => minion.IsValidTargetCached(E.Range) &&
                            HasDeadlyVenomBuff(minion));

                if (minions.Count() >= Settings.LaneClear.EMinMinionsHit)
                {
                    E.Cast();
                }
            }
        }
    }
}
