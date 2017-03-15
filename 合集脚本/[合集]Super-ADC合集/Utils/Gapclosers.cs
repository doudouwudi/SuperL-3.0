#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Gapclosers.cs" company="EloBuddy">
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
using System;
using EloBuddy;
using SharpDX;

namespace Marksman_Master.Utils
{
    public class GapCloserEventArgs : EventArgs
    {
        public GameObject Target { get; private set; }
        public Vector3 Start { get; private set; }
        public Vector3 End { get; private set; }
        public SpellSlot SpellSlot { get; private set; }
        public GapcloserTypes GapcloserType { get; private set; }
        public float GameTime { get; private set; }
        public int Delay { get; private set; }
        public int Enemies { get; private set; }
        public int HealthPercent { get; private set; }

        public GapCloserEventArgs(GameObject target, SpellSlot spellSlot, GapcloserTypes gapcloserType, Vector3 start, Vector3 end, int delay, int enemies, int healthPercent, float gameTime)
        {
            Target = target;
            SpellSlot = spellSlot;
            GapcloserType = gapcloserType;
            Start = start;
            End = end;
            Delay = delay;
            Enemies = enemies;
            HealthPercent = healthPercent;
            GameTime = gameTime;
        }
    }
}