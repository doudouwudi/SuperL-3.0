#region Licensing
// ---------------------------------------------------------------------
// <copyright file="AxeObjectData.cs" company="EloBuddy">
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
using EloBuddy;
using SharpDX;

namespace Marksman_Master.Plugins.Draven
{
    internal class AxeObjectData
    {
        public AIHeroClient Owner { get; set; }
        public int NetworkId { get; set; }
        public float StartTick { get; set; }
        public float EndTick { get; set; }
        public Vector3 EndPosition { get; set; }
    }
}
