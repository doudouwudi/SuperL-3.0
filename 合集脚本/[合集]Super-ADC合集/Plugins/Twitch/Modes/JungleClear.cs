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

namespace Marksman_Master.Plugins.Twitch.Modes
{
    internal class JungleClear : Twitch
    {
        public static void Execute()
        {
            var jungleMinions = StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTargetCached(E.Range)).ToList();

            if (!jungleMinions.Any())
                return;

            if (E.IsReady() && Settings.JungleClear.UseE && (Player.Instance.ManaPercent >= Settings.JungleClear.EMinMana))
            {
                if (StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTargetCached(E.Range))
                        .Any(
                            unit =>
                                unit.IsValidTargetCached(E.Range) && (unit.BaseSkinName.Contains("Baron") || unit.BaseSkinName.Contains("Dragon") ||
                                 unit.BaseSkinName.Contains("RiftHerald") || unit.BaseSkinName.Contains("Blue") ||
                                 unit.BaseSkinName.Contains("Red") || unit.BaseSkinName.Contains("Crab")) && !unit.BaseSkinName.Contains("Mini") &&
                                Damage.IsTargetKillableByE(unit)))
                {
                    E.Cast();
                    return;
                }
            }

            if (W.IsReady() && Settings.JungleClear.UseW && Player.Instance.ManaPercent >= Settings.JungleClear.WMinMana)
            {
                W.CastOnBestFarmPosition(1);
            }
        }
    }
}
