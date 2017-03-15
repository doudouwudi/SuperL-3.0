#region Licensing
// ---------------------------------------------------------------------
// <copyright file="ValueBase.cs" company="EloBuddy">
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
namespace Marksman_Master.PermaShow.Values
{
    using SharpDX;

    internal class ValueBase
    {
        public string UniqueId { get; }
        public Color Color { get; }
        public Text ItemNameText { get; }
        public Text ItemValueText { get; }
        
        public ValueBase(string uniqueId, Text itemNameText, Text itemValueText, Color color)
        {
            UniqueId = uniqueId;
            ItemNameText = itemNameText;
            ItemValueText = itemValueText;
            Color = color;
        }
    }
}