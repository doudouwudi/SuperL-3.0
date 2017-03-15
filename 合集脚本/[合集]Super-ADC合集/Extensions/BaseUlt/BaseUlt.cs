#region Licensing
// ---------------------------------------------------------------------
// <copyright file="BaseUlt.cs" company="EloBuddy">
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

namespace Marksman_Master.Extensions.BaseUlt
{
    using System;
    using System.Drawing;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    using SharpDX;
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Notifications;
    using EloBuddy.SDK.Rendering;
    using Utils;
    using Color = System.Drawing.Color;

    internal sealed class BaseUlt : ExtensionBase
    {
        public override bool IsEnabled { get; set; }

        public static bool EnabledByDefault { get; set; } = true;

        public override string Name { get; } = "BaseUlt";

        public Menu BaseUltMenu { get; private set; }

        public HashSet<Champion> SupportedChampions { get; private set; }

        public Dictionary<Champion, MissileInfo> MissileInfos { get; private set; }

        public Dictionary<int, Teleport.TeleportEventArgs> ActiveTeleports { get; private set; }

        public Dictionary<int, Teleport.TeleportEventArgs> ActiveRecalls { get; private set; }

        public Text Text { get; private set; }

        public Vector2 BarPosition { get; private set; }

        public float BarWidth { get; private set; }

        public float TextHeight { get; private set; }

        public static ColorPicker ColorPicker { get; private set; }

        public Obj_SpawnPoint SpawnPoint { get; private set; }

        public bool IsRecallTrackerEnabled
            => BaseUltMenu?["RecallTracker.Enable"] != null && BaseUltMenu["RecallTracker.Enable"].Cast<CheckBox>().CurrentValue;

        public bool IsBaseUltEnabled
            => BaseUltMenu?["BaseUlt.Enable"] != null && BaseUltMenu["BaseUlt.Enable"].Cast<CheckBox>().CurrentValue;

        public bool DisableBaseUltInComboMode
            => BaseUltMenu?["BaseUlt.DisableInComboMode"] != null && BaseUltMenu["BaseUlt.DisableInComboMode"].Cast<CheckBox>().CurrentValue;
        
        public int MaxInvisibilityTime
            => BaseUltMenu?["BaseUlt.MaxTimeout"]?.Cast<Slider>().CurrentValue ?? 0;

        public int RecallTrackerBarSize
            => BaseUltMenu?["RecallTracker.BarSize"]?.Cast<Slider>().CurrentValue ?? 0;

        public int FontSize
            => BaseUltMenu?["RecallTracker.FontSize"]?.Cast<Slider>().CurrentValue ?? 0;

        public bool IsEnabledFor(string championName) => BaseUltMenu?[$"BaseUlt.Enable.{championName}"] != null && BaseUltMenu[$"BaseUlt.Enable.{championName}"].Cast<CheckBox>().CurrentValue;

        public override void Load()
        {
            IsEnabled = true;

            BarWidth = Drawing.Height/2.7F;

            var barXPos = Drawing.Width/2F - BarWidth/2;

            BarPosition = new Vector2(barXPos, Drawing.Height*.825F);

            SupportedChampions = new HashSet<Champion>
            {
                Champion.Ashe,
                Champion.Draven,
                Champion.Ezreal,
                Champion.Jinx
            };

            MissileInfos = new Dictionary<Champion, MissileInfo>
            {
                [Champion.Ashe] = new MissileInfo(1600, .25f, 130),
                [Champion.Draven] = new MissileInfo(2000, .5f, 160),
                [Champion.Ezreal] = new MissileInfo(2000, 1, 160),
                [Champion.Jinx] = new MissileInfo(1700, .6f, 140, 0, 1700, 2500)
            };

            ActiveTeleports = new Dictionary<int, Teleport.TeleportEventArgs>();
            ActiveRecalls = new Dictionary<int, Teleport.TeleportEventArgs>();

            Text = new Text("", new Font("tahoma", 10, FontStyle.Italic));
            TextHeight = Text.Description.Height;
            
            ColorPicker = new ColorPicker("RecallTracker.BarColor", new ColorBGRA(255, 255, 255, 255));

            if (!MenuManager.ExtensionsMenu.SubMenus.Any(x => x.UniqueMenuId.Contains("Extension.BaseUlt")))
            {
                if (!MainMenu.IsOpen)
                {
                    BaseUltMenu = MenuManager.ExtensionsMenu.AddSubMenu("基地大招", "Extension.BaseUlt");
                    BuildMenu();
                }
                else MainMenu.OnClose += MainMenu_OnClose;
            }
            Teleport.OnTeleport += Teleport_OnTeleport;

            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        
        private void SubscribeToEvents()
        {
            SpawnPoint = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy);

            if (SpawnPoint == null)
                throw new Exception("Something went wrong. Baseult couldn't load.");

            Game.OnTick += Game_OnTick;

            ChampionTracker.Initialize(ChampionTrackerFlags.VisibilityTracker);
        }

        private void Game_OnTick(EventArgs args)
        {
            ActiveTeleports.Select(x => x.Key).ToList().ForEach(x =>
            {
                if (ActiveTeleports.ContainsKey(x) && ((Core.GameTickCount - ActiveTeleports[x].Start > ActiveTeleports[x].Duration) || ActiveTeleports[x].Status != TeleportStatus.Start))
                {
                    ActiveTeleports.Remove(x);
                }
            });

            ActiveRecalls.Select(x => x.Key).ToList().ForEach(x =>
            {
                if (ActiveRecalls.ContainsKey(x) && ((Core.GameTickCount - ActiveRecalls[x].Start > ActiveRecalls[x].Duration) || ActiveRecalls[x].Status != TeleportStatus.Start))
                {
                    ActiveRecalls.Remove(x);
                }
            });

            if (!IsEnabled || !IsBaseUltEnabled || !Player.Instance.Spellbook.GetSpell(SpellSlot.R).IsReady)
                return;

            if (DisableBaseUltInComboMode && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            foreach (var recall in ActiveRecalls)
            {
                var caster = EntityManager.Heroes.AllHeroes.Find(x => x.NetworkId == recall.Key);

                if (caster == null || !caster.IsEnemy || recall.Value == null || !IsEnabledFor(caster.ChampionName) || Collides(caster, SpawnPoint.Position))
                    continue;
                
                var invisibleFor = (Core.GameTickCount - caster.GetVisibilityTrackerData().LastVisibleGameTime * 1000) / 1000; // in seconds

                if (invisibleFor > MaxInvisibilityTime)
                    continue;

                var damage = GetUltDamage(caster);
                var travelTime = GetUltTravelTime(SpawnPoint.Position);
                var timeLeft = recall.Value.Start + recall.Value.Duration - Core.GameTickCount;

                if ((damage >= Damage.GetHealthAfterTime(caster, timeLeft/1000)) && (timeLeft - travelTime >= -130) && (timeLeft - travelTime <= -75))
                {   
                    Player.Instance.Spellbook.CastSpell(SpellSlot.R, SpawnPoint.Position);
                }
            }
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (!IsRecallTrackerEnabled || !ActiveTeleports.Any() || Drawing.Direct3DDevice.IsDisposed)
                return;
            
            if (ActiveTeleports.Any())
                Drawing.DrawLine(BarPosition, new Vector2(BarPosition.X + BarWidth, BarPosition.Y), RecallTrackerBarSize,
                    Color.FromArgb(ColorPicker.Color.A, ColorPicker.Color.R, ColorPicker.Color.G, ColorPicker.Color.B));
            
            foreach (
                var teleport in
                    ActiveTeleports.Where(x => x.Value != null && x.Value.Status == TeleportStatus.Start).OrderByDescending(x => x.Value.Start))
            {
                var caster = EntityManager.Heroes.AllHeroes.Find(x => x.NetworkId == teleport.Key);
                if (caster == null)
                    continue;

                var count = GetIndex(ActiveTeleports, teleport.Key) + 1;

                var endTime = teleport.Value.Start + teleport.Value.Duration;
                var percentage = Math.Max(0, Math.Min(100, ((float) endTime - Core.GameTickCount)/teleport.Value.Duration*100));
                var degree = Misc.GetNumberInRangeFromProcent(percentage, 3, 110);
                var color =
                    new Misc.HsvColor(degree, 1, 1)
                    {
                        Value =
                            Misc.GetNumberInRangeFromProcent(
                                Math.Max(0, Math.Min((GetIndex(ActiveTeleports, teleport.Key) + 1f)/ActiveTeleports.Count*100, 100)),
                                0.35f, 1)
                    }.ColorFromHsv();
                
                var endPos = new Vector2(BarPosition.X + BarWidth*percentage/100, BarPosition.Y);

                var stringBuilder = new StringBuilder();
                
                stringBuilder.Append($"{caster.Hero} ({(int)caster.Health} HP) ");
                stringBuilder.Append($"{((endTime - Core.GameTickCount)/1000F).ToString("F1")}s ");
                stringBuilder.Append($"({percentage.ToString("F1")}%)");

                Vector2[] linePos =
                {
                    new Vector2(endPos.X, BarPosition.Y + RecallTrackerBarSize/2f - (int)(TextHeight * 1.27f * count)),
                    new Vector2(endPos.X, BarPosition.Y + RecallTrackerBarSize/2f)
                };

                Text.Draw(stringBuilder.ToString(), Color.AliceBlue, new Vector2(endPos.X + 5, endPos.Y - TextHeight * 1.25f * count));

                Drawing.DrawLine(BarPosition, endPos, RecallTrackerBarSize, color);

                Drawing.DrawLine(linePos[0], linePos[1], 1, Color.AliceBlue);
            }
        }

        public int GetIndexDescending(Dictionary<int, Teleport.TeleportEventArgs> dictionary, int key)
        {
            var index = 0;

            foreach (var teleportEventArgse in dictionary.Where(x => x.Value != null && x.Value.Status == TeleportStatus.Start).OrderByDescending(x => x.Value.Start))
            {
                if (teleportEventArgse.Key == key)
                    return index;
                
                index++;
            }

            return 0;
        }

        public int GetIndex(Dictionary<int, Teleport.TeleportEventArgs> dictionary, int key)
        {
            var index = 0;

            foreach (var teleportEventArgse in dictionary.Where(x => x.Value != null && x.Value.Status == TeleportStatus.Start).OrderBy(x => x.Value.Start))
            {
                if (teleportEventArgse.Key == key)
                    return index;

                index++;
            }

            return 0;
        }

        private void Teleport_OnTeleport(Obj_AI_Base sender, Teleport.TeleportEventArgs args)
        {
            if (sender?.Type != GameObjectType.AIHeroClient)
                return;

            var hero = sender as AIHeroClient;

            if (hero == null || hero.IsMe || !hero.IsEnemy || args.Type == TeleportType.Shen)
                return;

            switch (args.Status)
            {
                case TeleportStatus.Start:
                    if (args.Type == TeleportType.Recall)
                    {
                        if (IsRecallTrackerEnabled)
                            Notifications.Show(new SimpleNotification("Recall tracker", $"{hero.Hero} ({hero.Name}) just started recalling."), 2500);

                        ActiveRecalls[hero.NetworkId] = args;
                    }
                    ActiveTeleports[hero.NetworkId] = args;
                    break;
                case TeleportStatus.Abort:
                    if (args.Type == TeleportType.Recall)
                    {
                        ActiveRecalls.Remove(hero.NetworkId);
                    }
                    ActiveTeleports.Remove(hero.NetworkId);
                    break;
                case TeleportStatus.Finish:
                    if (args.Type == TeleportType.Recall)
                    {
                        if(IsRecallTrackerEnabled)
                            Notifications.Show(new SimpleNotification("Recall tracker", $"{hero.Hero} ({hero.Name}) just finished recalling."), 2500);

                        ActiveRecalls.Remove(hero.NetworkId);
                    }

                    ActiveTeleports.Remove(hero.NetworkId);
                    break;
                case TeleportStatus.Unknown:
                    ActiveTeleports.Remove(hero.NetworkId);
                    ActiveRecalls.Remove(hero.NetworkId);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args.Status));
            }
        }

        private void MainMenu_OnClose(object sender, EventArgs args)
        {
            if (MenuManager.ExtensionsMenu.SubMenus.Any(x => x.UniqueMenuId.Contains("Extension.BaseUlt")))
                return;

            BaseUltMenu = MenuManager.ExtensionsMenu.AddSubMenu("Base Ult", "Extension.BaseUlt");
            BuildMenu();

            MainMenu.OnClose -= MainMenu_OnClose;
        }

        private void BuildMenu()
        {
            BaseUltMenu.AddGroupLabel("Recall tracker settings");
            BaseUltMenu.Add("RecallTracker.Enable", new CheckBox("Enable Recall tracker"));

            BaseUltMenu.Add("RecallTracker.BarColor", new CheckBox("Change bar color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker.Initialize(Color.DeepPink);
                a.CurrentValue = false;
            };

            var fontSize = BaseUltMenu.Add("RecallTracker.FontSize", new Slider("Font size : {0}", 10, 5, 20));
            
            fontSize.OnValueChange +=
                (sender, args) =>
                {
                    Text.ReplaceFont(new Font("tahoma", args.NewValue, FontStyle.Italic));

                    TextHeight = Text.Description.Height;
                };
            
            Text.ReplaceFont(new Font("tahoma", fontSize.CurrentValue, FontStyle.Italic));

            TextHeight = Text.Description.Height;

            BaseUltMenu.Add("RecallTracker.BarSize", new Slider("Bar thickness : {0}", 7, 1, 15));

            if (!SupportedChampions.Contains(Player.Instance.Hero))
            {
                BaseUltMenu.AddGroupLabel("Your champion is not supported !");

                Dispose();

                return;
            }

            BaseUltMenu.AddGroupLabel("Base ult settings");

            BaseUltMenu.AddLabel("Basic settings : ");
            BaseUltMenu.Add("BaseUlt.Enable", new CheckBox("Enable Base ult"));
            BaseUltMenu.Add("BaseUlt.DisableInComboMode", new CheckBox("Disable Base ult in combo mode", false));
            BaseUltMenu.Add("BaseUlt.MaxTimeout", new Slider("Maximum target invisibility time : {0} seconds", 30, 0, 120));
            BaseUltMenu.AddSeparator(2);
            BaseUltMenu.AddLabel("Health prediction is not working currently.\nPlease don't set this value too high or you will see a lot missed baseults.");
            BaseUltMenu.AddSeparator(5);

            BaseUltMenu.AddLabel("Whitelist :");

            foreach (var aiHeroClient in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero))
            {
                BaseUltMenu.Add($"BaseUlt.Enable.{aiHeroClient.ChampionName}", new CheckBox($"Enable on {aiHeroClient.ChampionName}"));
            }

            SubscribeToEvents();
        }

        public override void Dispose()
        {
            IsEnabled = false;
            
            Teleport.OnTeleport -= Teleport_OnTeleport;
            Drawing.OnEndScene -= Drawing_OnEndScene;
            Game.OnTick -= Game_OnTick;
        }
        
        public float GetUltTravelTime(Vector3 point)
        {
            switch (Player.Instance.Hero)
            {
                case Champion.Ashe:
                case Champion.Draven:
                case Champion.Ezreal:
                    return (Player.Instance.Distance(point)/MissileInfos[Player.Instance.Hero].MissileSpeed +
                            MissileInfos[Player.Instance.Hero].MissileCastTime)*1000;
                case Champion.Jinx:
                    var distance = Player.Instance.Distance(point);

                    if (distance <= 1700)
                    {
                        return (distance/MissileInfos[Player.Instance.Hero].MissileSpeed +
                                MissileInfos[Player.Instance.Hero].MissileCastTime)*1000;
                    }

                    var addition = 1700/MissileInfos[Player.Instance.Hero].MissileSpeed +
                                   MissileInfos[Player.Instance.Hero].MissileCastTime;

                    return ((distance - 1700)/2230 + addition)*1000;
                default:
                    return 0;
            }
        }
        
        public float GetUltDamage(AIHeroClient target, bool changePositionToRecallPoint = true)
        {
            switch (Player.Instance.Hero)
            {
                case Champion.Ashe:
                case Champion.Draven:
                    return Player.Instance.GetSpellDamageCached(target, SpellSlot.R);
                case Champion.Ezreal:
                    return Damage.GetEzrealRDamage(target, changePositionToRecallPoint ? SpawnPoint.Position : target.Position);
                case Champion.Jinx:
                    return Damage.GetJinxRDamage(target, changePositionToRecallPoint ? SpawnPoint.Position : target.Position);
                default:
                    return 0;
            }
        }

        public bool Collides(AIHeroClient target, Vector3 position)
        {
            var polygon = new Geometry.Polygon.Rectangle(Player.Instance.Position, position, MissileInfos[Player.Instance.Hero].Width);

            switch (Player.Instance.Hero)
            {
                case Champion.Ezreal:
                    return false;
                case Champion.Draven:
                case Champion.Ashe:
                case Champion.Jinx:
                    foreach (var hero in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.NetworkId != target.NetworkId))
                    {
                        return new Geometry.Polygon.Circle(hero.Position, hero.BoundingRadius).Points.Any(p => polygon.IsInside(p));
                    }
                    break;
                default:
                    return false;
            }
            return false;
        }
        
        public class MissileInfo
        {
            public float MissileSpeed { get; }
            public float MissileAcceleration { get; }
            public float MissileMinSpeed { get; }
            public float MissileMaxSpeed { get; }
            public float MissileCastTime { get; }
            public int Width { get; }
            public bool HasAcceleration { get; }

            public MissileInfo(float missileSpeed, float missileCastTime, int width)
            {
                MissileSpeed = missileSpeed;
                MissileCastTime = missileCastTime;
                Width = width;
            }

            public MissileInfo(float missileSpeed, float missileCastTime, int width, float missileAcceleration,
                float missileMinSpeed, float missileMaxSpeed) : this(missileSpeed, missileCastTime, width)
            {
                MissileAcceleration = missileAcceleration;
                MissileMinSpeed = missileMinSpeed;
                MissileMaxSpeed = missileMaxSpeed;
                HasAcceleration = true;
            }
        }
    }

    internal static class Damage
    {
        public static int[] RMinimalDamage { get; } = { 0, 25, 35, 45 };
        public static float RBonusAdDamageMod { get; } = 0.15f;
        public static float[] RMissingHealthBonusDamage { get; } = { 0, 0.25f, 0.3f, 0.35f };

        public static float GetJinxRDamage(AIHeroClient target, Vector3? customPosition = null)
        {
            var level = Player.Instance.Spellbook.GetSpell(SpellSlot.R).Level;

            var distance = Player.Instance.DistanceCached(customPosition ?? target.Position) > 1500 ? 1499 : Player.Instance.DistanceCached(customPosition ?? target.Position);
            distance = distance < 100 ? 100 : distance;

            var baseDamage = Misc.GetNumberInRangeFromProcent(Misc.GetProcentFromNumberRange(distance, 100, 1500),
                RMinimalDamage[level],
                RMinimalDamage[level] * 10);
            var bonusAd = Misc.GetNumberInRangeFromProcent(Misc.GetProcentFromNumberRange(distance, 100, 1500),
                RBonusAdDamageMod,
                RBonusAdDamageMod * 10);
            var percentDamage = (target.MaxHealth - GetHealthAfterTime(target, 0)) * RMissingHealthBonusDamage[level];

            var finalDamage = Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                (float)(baseDamage + percentDamage + Player.Instance.FlatPhysicalDamageMod * bonusAd));
            
            return finalDamage;
        }

        public static float GetHealthAfterTime(AIHeroClient target, int bonusTime)
        {
            if (target.IsHPBarRendered)
                return target.Health;

            var invisibleTime = Game.Time - target.GetVisibilityTrackerData().LastVisibleGameTime + bonusTime;

            var healthPerSec = 0.45f * target.Level;//bug this is not a valid solution
            var result = target.Health + healthPerSec * invisibleTime;

            return result > target.MaxHealth ? target.MaxHealth : result;
        }

        public static float GetEzrealRDamage(Obj_AI_Base target, Vector3? customPosition = null)
        {
            var polygon = new Geometry.Polygon.Rectangle(Player.Instance.Position, customPosition ?? target.Position, 160);
            var objects = ObjectManager
                    .Get<Obj_AI_Base>().Count(x => x.NetworkId != target.NetworkId && x.IsEnemy &&
                        x.IsValidTarget() &&
                        new Geometry.Polygon.Circle(Prediction.Position.PredictUnitPosition(x, 1000 + (int)(x.DistanceCached(Player.Instance) / 2000) * 1000), x.BoundingRadius).Points.Any(
                            p => polygon.IsInside(p)));

            var damage = Player.Instance.GetSpellDamageCached(target, SpellSlot.R);

            var minDamage = damage * .3f;

            for (var i = 1; i <= objects; i++)
            {
                damage *= .9f;
            }

            var finalDamage = damage >= minDamage ? damage : minDamage;
            
            return finalDamage;
        }
    }
}