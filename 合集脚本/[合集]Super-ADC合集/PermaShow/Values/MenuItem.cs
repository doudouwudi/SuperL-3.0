#region Licensing
// ---------------------------------------------------------------------
// <copyright file="MenuItem.cs" company="EloBuddy">
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
    using System;

    using EloBuddy;

    using Interfaces;

    internal class MenuItem : IValue<bool>
    {
        private bool _value;

        public string ItemName { get; set; }
        public string MenuItemName { get; set; }

        public bool Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnValueChange?.Invoke(this, value);
            }
        }

        public event EventHandler<bool> OnValueChange;

        public MenuItem(string itemName, string menuItemName)
        {
            ItemName = itemName;
            MenuItemName = menuItemName;
            Game.OnTick += Game_OnTick;
        }

        private void Game_OnTick(EventArgs args)
        {
            Value = MenuManager.MenuValues[MenuItemName];
        }

        public T Get<T>()
        {
            return (T)Convert.ChangeType(this, typeof(T));
        }
    }
}