#region Licensing
// ---------------------------------------------------------------------
// <copyright file="ItemsCollection.cs" company="EloBuddy">
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
using System.Linq;
using Marksman_Master.Utils;

namespace Marksman_Master.Activator
{
    internal class ItemsCollection
    {
        public static Item[] ItemsObject = new Item[(int) Enum.GetValues(typeof(ItemsEnum)).Cast<ItemsEnum>().Max() + 1];

        public Item this[ItemsEnum index]
        {
            get { return Enum.IsDefined(typeof (ItemsEnum), index) ? ItemsObject[(int) index] : null; }

            set
            {
                if (Enum.IsDefined(typeof (ItemsEnum), index))
                {
                    ItemsObject[(int) index] = value;
                }
            }
        }
    }
}