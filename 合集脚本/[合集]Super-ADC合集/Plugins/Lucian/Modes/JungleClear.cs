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

namespace Marksman_Master.Plugins.Lucian.Modes
{
    internal class JungleClear : Lucian
    {
        public static void Execute()
        {
            var jungleMinions = StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTarget(Player.Instance.GetAutoAttackRange())).ToList();

            if (!jungleMinions.Any())
                return;

            if ((!Settings.LaneClear.UseQInJungleClear || !Settings.LaneClear.UseWInJungleClear || !Settings.LaneClear.UseEInJungleClear) || HasPassiveBuff || Player.Instance.HasSheenBuff() ||
                (Player.Instance.ManaPercent < Settings.LaneClear.MinManaQ))
                return;

            var target = Orbwalker.GetTarget() as Obj_AI_Base;

            if (target == null)
                return;

            if (Settings.LaneClear.UseQInJungleClear && Q.IsReady())
            {
                Q.Cast(target);
                return;
            }
            if (Settings.LaneClear.UseWInJungleClear && W.IsReady())
            {
                W.Cast(target);
                return;
            }

            if (!Settings.LaneClear.UseEInJungleClear || !E.IsReady())
                return;

            var shortEPosition = Player.Instance.Position.Extend(Game.CursorPos, 85).To3D();
            E.Cast(shortEPosition);
        }
    }
}
