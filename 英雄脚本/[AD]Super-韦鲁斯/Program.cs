using System;
using SharpDX;
using EloBuddy;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Spells;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using System.Collections.Generic;
using Color = System.Drawing.Color;

namespace FrOnDaL_Varus
{
    internal class Program
    {
        private static AIHeroClient Varus => Player.Instance;
        private static Spell.Chargeable _q;
        private static Spell.Active _w;
        private static Spell.Skillshot _e, _r;
        private static Spellbook _lvl;
        private static float _dikey, _yatay;
        private static float genislik = 104;
        private static float yukseklik = 9.82f;
        private static readonly Item AutoBotrk = new Item(ItemId.Blade_of_the_Ruined_King);
        private static readonly Item AutoCutlass = new Item(ItemId.Bilgewater_Cutlass);
        private static Menu _main, _combo, _harras, _laneclear, _jungleclear, _drawings, _misc;
        internal static bool IsPreAa;
        internal static bool IsAfterAa;
        private static bool BuffW(Obj_AI_Base unit) => unit.Buffs.Any(x => x.IsActive && x.Name.Equals("varuswdebuff", StringComparison.CurrentCultureIgnoreCase));
        private static BuffInstance GoBuffW(Obj_AI_Base unit) => BuffW(unit) ? unit.Buffs.First(x => x.IsActive && x.Name.Equals("varuswdebuff", StringComparison.CurrentCultureIgnoreCase)) : null;
        private static bool SpellShield(Obj_AI_Base shield) { return shield.HasBuffOfType(BuffType.SpellShield) || shield.HasBuffOfType(BuffType.SpellImmunity); }
        private static bool SpellBuff(AIHeroClient buf)
        {
            if (buf.Buffs.Any(x => x.IsValid && (x.Name.Equals("ChronoShift", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("FioraW", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("TaricR", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("BardRStasis", StringComparison.CurrentCultureIgnoreCase) ||
                                       x.Name.Equals("JudicatorIntervention", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("UndyingRage", StringComparison.CurrentCultureIgnoreCase) || (x.Name.Equals("kindredrnodeathbuff", StringComparison.CurrentCultureIgnoreCase) && (buf.HealthPercent <= 10)))))
            { return true; }
            if (buf.ChampionName != "Poppy") return buf.IsInvulnerable;
            return EntityManager.Heroes.Allies.Any(y => !y.IsMe && y.Buffs.Any(z => (z.Caster.NetworkId == buf.NetworkId) && z.IsValid && z.DisplayName.Equals("PoppyDITarget", StringComparison.CurrentCultureIgnoreCase))) || buf.IsInvulnerable;
        }
        private static void AutoItem(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var botrkHedef = TargetSelector.GetTarget(EntityManager.Heroes.Enemies.Where(x => x != null && x.IsValidTarget() && x.IsInRange(Varus, 550)), DamageType.Physical);
            if (botrkHedef != null && _misc["botrk"].Cast<CheckBox>().CurrentValue && AutoBotrk.IsOwned() && AutoBotrk.IsReady() && Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                AutoBotrk.Cast(botrkHedef);
            }
            if (botrkHedef != null && _misc["autoCutlass"].Cast<CheckBox>().CurrentValue && AutoCutlass.IsOwned() && AutoCutlass.IsReady() && Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                AutoCutlass.Cast(botrkHedef);
            }
        }
        public static void OnLevelUpR(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args) { if (Varus.Level > 4) { _lvl.LevelSpell(SpellSlot.R); } }
        private static void Main() { Loading.OnLoadingComplete += OnLoadingComplete; }
        private static void OnLoadingComplete(EventArgs args)
        {
            if (Varus.Hero != Champion.Varus) return;
            _q = new Spell.Chargeable(SpellSlot.Q, 1000, 1600, 1300, 0, 1900, 70) { AllowedCollisionCount = int.MaxValue };
            _w = new Spell.Active(SpellSlot.W);
            _e = new Spell.Skillshot(SpellSlot.E, 925, SkillShotType.Circular, 250, 1500, 235);
            _r = new Spell.Skillshot(SpellSlot.R, 1250, SkillShotType.Linear, 250, 1950, 120) { AllowedCollisionCount = -1 };
            Orbwalker.OnPreAttack += (a, b) => IsPreAa = true;
            Orbwalker.OnPostAttack += (a, b) => { IsPreAa = false; IsAfterAa = true; };
            Game.OnTick += VarusActive;
            Gapcloser.OnGapcloser += QAndRAntiGapCloser;
            Obj_AI_Base.OnProcessSpellCast += AutoItem;
            Obj_AI_Base.OnLevelUp += OnLevelUpR;
            Drawing.OnEndScene += HasarGostergesi;
            Drawing.OnDraw += SpellDraw;
            _lvl = Varus.Spellbook;
            _main = MainMenu.AddMenu("Super韦鲁斯", "index");
            _main.AddGroupLabel("欢迎使用 Super韦鲁斯");
            _main.AddSeparator(5);
            _main.AddLabel("祝您游戏愉快！");
            _combo = _main.AddSubMenu("连招设置");
            _combo.AddGroupLabel("韦鲁斯 连招 设置");
            _combo.AddLabel("连招使用Q (On/Off)");
            _combo.Add("q", new CheckBox("使用 Q"));
            _combo.AddSeparator(5);
            _combo.Add("qlogic", new ComboBox("Q 逻辑", 0, "正常", "Super"));
            _combo.AddSeparator(5);
            _combo.AddLabel("连招使用E (On/Off)" + "                                 " + "仅在3成W使用E (On/Off)");
            _combo.Add("e", new CheckBox("使用 E"));
            _combo.Add("stackWuseE", new CheckBox("仅在3成W使用E"));
            _combo.AddSeparator(5);
            _combo.Add("EHitChance", new Slider("E 命中率 : {0}", 60));
            _combo.AddSeparator(5);
            _combo.AddLabel("使用手动R键设置");
            _combo.Add("RKey", new KeyBind("手动R热键绑定", false, KeyBind.BindTypes.HoldActive, 'T'));
            _combo.AddSeparator(5);
            _combo.Add("rlogic", new ComboBox("R 逻辑 ", 0, "正常", "Super"));
            _combo.AddSeparator(5);
            _combo.Add("RHit", new Slider("手动R敌人数量", 1, 1, 5));
            _combo.AddSeparator(5);
            _combo.Add("RHitChance", new Slider("R 命中率 : {0}", 60));
            _harras = _main.AddSubMenu("骚扰设置");
            _harras.AddGroupLabel("韦鲁斯 骚扰 设置");
            _harras.AddLabel("使用骚扰Q键设置");
            _harras.Add("harrasQ", new KeyBind("骚扰Q热键绑定", false, KeyBind.BindTypes.HoldActive, 'C'));
            _harras.AddSeparator(5);
            _harras.Add("HmanaP", new Slider("自动骚扰Q蓝量控制百分比 ({0}%) to use Q", 50, 1));
            _harras.AddSeparator(5);
            _harras.Add("QHitChance", new Slider("Q 命中率 : {0}", 60));
            _laneclear = _main.AddSubMenu("清线设置");
            _laneclear.AddGroupLabel("韦鲁斯 清线 设置");
            _laneclear.Add("LmanaP", new Slider(" 使用Q和E清线最小蓝量控制百分比({0}%)", 70, 1));
            _laneclear.AddSeparator(5);
            _laneclear.Add("q", new CheckBox("使用Q (On/Off)"));
            _laneclear.Add("qHit", new Slider("最少{0} 小兵使用Q", 3, 1, 6));
            _laneclear.AddSeparator(5);
            _laneclear.Add("e", new CheckBox("使用 E (On/Off)"));
            _laneclear.Add("eHit", new Slider("最少 {0} 小兵使用E", 3, 1, 6));
            _jungleclear = _main.AddSubMenu("打野设置");
            _jungleclear.AddGroupLabel("韦鲁斯 打野 设置");
            _jungleclear.Add("JmanaP", new Slider("使用Q和E打野最小蓝量控制百分比 ({0}%)", 30, 1));
            _jungleclear.AddSeparator(5);
            _jungleclear.AddLabel("打野使用Q和E (On/Off)");
            _jungleclear.Add("q", new CheckBox("使用 Q"));
            _jungleclear.Add("e", new CheckBox("使用 E"));
            _drawings = _main.AddSubMenu("线圈设置");
            _drawings.AddGroupLabel("韦鲁斯 线圈 设置");
            _drawings.AddLabel("画出Q-E-R线圈 (On/Off)");
            _drawings.Add("drawQ", new CheckBox("Q线圈", false));
            _drawings.Add("drawE", new CheckBox("E线圈", false));
            _drawings.Add("drawR", new CheckBox("R线圈", false));
            _drawings.AddLabel("画出损伤指示 (On/Off)");
            _drawings.Add("damageQ", new CheckBox("损伤指示"));
            _misc = _main.AddSubMenu("其他设置");
            _misc.AddLabel("自动使用破败王者之刃和水银弯刀");
            _misc.Add("botrk", new CheckBox("使用破败王者之刃 "));
            _misc.Add("autoCutlass", new CheckBox("使用水银弯刀"));
            _drawings.AddSeparator(5);
            _misc.AddLabel("使用R反突进 ");
            //_misc.Add("Qgap", new CheckBox("Use Q Anti Gap Closer (On/Off)", false));
            _misc.Add("Rgap", new CheckBox("使用R反突进", false));
        }
        private static void SpellDraw(EventArgs args)
        {
            if (_drawings["drawQ"].Cast<CheckBox>().CurrentValue) { _q.DrawRange(Color.FromArgb(130, Color.Green)); }
            if (_drawings["drawE"].Cast<CheckBox>().CurrentValue) { _e.DrawRange(Color.FromArgb(130, Color.Green)); }
            if (_drawings["drawR"].Cast<CheckBox>().CurrentValue) { _r.DrawRange(Color.FromArgb(130, Color.Green)); }
        }
        private static void VarusActive(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            { Combo(); }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            { LaneClear(); }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            { JungClear(); }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            { Harras(); }
            if (_combo["RKey"].Cast<KeyBind>().CurrentValue)
            { ManuelR(); }
        }
        private static void LaneClear()
        {
            var farmClear = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Varus.ServerPosition).Where(x => x.IsValidTarget(1500)).ToList();           
            if (!farmClear.Any()) return;
            if (_q.IsReady() && _laneclear["q"].Cast<CheckBox>().CurrentValue)
            {
                if (!_q.IsCharging && !Varus.IsUnderEnemyturret() && (Varus.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue) && (farmClear.Count >= _laneclear["qHit"].Cast<Slider>().CurrentValue) && !IsPreAa)
                {
                    if (!IsPreAa && !Orbwalker.ShouldWait)
                        _q.StartCharging();
                }
                else if (_q.IsCharging && _q.IsFullyCharged)
                {
                    _q.CastOnBestFarmPosition(1);
                }
            }
            Core.DelayAction(() => {
                if (_e.IsReady() && !Varus.IsUnderEnemyturret() && _laneclear["e"].Cast<CheckBox>().CurrentValue && (Varus.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue) && (farmClear.Count >= _laneclear["eHit"].Cast<Slider>().CurrentValue))
            {
                 _e.CastOnBestFarmPosition();              
            } }, 1000);
        }
        private static void JungClear()
        {
            var farmjung = EntityManager.MinionsAndMonsters.GetJungleMonsters(Varus.ServerPosition, Varus.GetAutoAttackRange()).FirstOrDefault();
            if (farmjung != null && _q.IsReady() && _jungleclear["q"].Cast<CheckBox>().CurrentValue)
            {
                if (!_q.IsCharging && !IsPreAa && (Varus.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue))
                {
                    _q.StartCharging();
                }
                else if (_q.IsCharging)
                {
                    _q.Cast(farmjung.ServerPosition);
                }
            }           
            if (farmjung != null && _e.IsReady() && !_q.IsCharging && _jungleclear["e"].Cast<CheckBox>().CurrentValue && (Varus.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue))
            {
                Core.DelayAction(() => { _e.Cast(farmjung.ServerPosition); }, 2000);
            }            
        }
        private static void Harras()
        {
            if (!_q.IsReady() || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) return;
            if (!_q.IsCharging && !IsPreAa && !Orbwalker.ShouldWait && (Varus.CountEnemyHeroesInRangeWithPrediction(500, 350) == 0) &&
                !Varus.IsUnderEnemyturret() && (Varus.ManaPercent >= _harras["HmanaP"].Cast<Slider>().CurrentValue) &&
                EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(_q.MaximumRange - 100)))                  
            {
                _q.StartCharging();
            }
            else if (_q.IsCharging)
            {
                foreach (var prophecyQ in EntityManager.Heroes.Enemies.Where( x => (x != null) && x.IsValidTarget(_q.Range) &&
                         ((Varus.CountEnemyChampionsInRange(Varus.GetAutoAttackRange()) > 0) ||_q.IsFullyCharged))
                    .Select(target => _q.GetPrediction(target)).Where(qPrediction => qPrediction.HitChancePercent >= _harras["QHitChance"].Cast<Slider>().CurrentValue))
                {                        
                    _q.Cast(prophecyQ.CastPosition);
                }
            }
        }
        private static void ManuelR()
        {
            if (!_r.IsReady() || !_combo["RKey"].Cast<KeyBind>().CurrentValue) return;
            var hedefR = TargetSelector.GetTarget(_r.Range - 100, DamageType.Physical);
            var rHit = EntityManager.Heroes.Enemies.Where(x => x.Distance(hedefR) <= 450f && !SpellShield(x) && !SpellBuff(x)).ToList();
            if (hedefR == null) return;
            if (_combo["rlogic"].Cast<ComboBox>().CurrentValue == 1) { var prophecyR = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput { CollisionTypes = new HashSet<CollisionType> { CollisionType.YasuoWall, CollisionType.AiHeroClient },
                Delay = .25f, From = Varus.Position, Radius = _r.Width, Range = _r.Range - 100, RangeCheckFrom = Varus.Position, Speed = _r.Speed, Target = hedefR, Type = SkillShotType.Linear });
                if ((prophecyR.HitChancePercent >= _combo["RHitChance"].Cast<Slider>().CurrentValue) && (rHit.Count >= _combo["RHit"].Cast<Slider>().CurrentValue))                   
                {
                    _r.Cast(prophecyR.CastPosition);
                } }
            else
            {
                var prophecyR = _r.GetPrediction(hedefR);
                if ((prophecyR.HitChancePercent >= _combo["RHitChance"].Cast<Slider>().CurrentValue) && (rHit.Count >= _combo["RHit"].Cast<Slider>().CurrentValue))
                {
                    _r.Cast(prophecyR.CastPosition);
                }
            }
        }
        private static void Combo()
        {
            if (_q.IsReady() && _combo["q"].Cast<CheckBox>().CurrentValue)
            {
                var qProphecy = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_q.IsCharging ? _q.Range : _q.MaximumRange) && !SpellShield(x) && !SpellBuff(x)).ToList();                
                var targetQ = TargetSelector.GetTarget(qProphecy, DamageType.Physical);
                if (targetQ != null)
                {
                    if (!_q.IsCharging && !IsPreAa && (qProphecy.Any(x => Varus.CountEnemyHeroesInRangeWithPrediction((int)Varus.GetAutoAttackRange(), 350) <= 1) || (Varus.CountEnemyHeroesInRangeWithPrediction(500, 350) == 0)))
                    {
                        _q.StartCharging(); return;
                    }
                }
                if (!_q.IsCharging) return;
                if (targetQ != null)
                {                  
                    if ((Varus.CountEnemyChampionsInRange(Varus.GetAutoAttackRange()) == 0) && !_q.IsFullyCharged) return;
                    if (_combo["qlogic"].Cast<ComboBox>().CurrentValue == 1) { var qPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput {
                        CollisionTypes = new HashSet<CollisionType> { CollisionType.YasuoWall }, Delay = 0, From = Varus.Position, Radius = 70, Range = _q.Range,
                        RangeCheckFrom = Varus.Position, Speed = _q.Speed, Target = targetQ, Type = SkillShotType.Linear });
                        if (qPrediction.HitChancePercent >= 60)
                        { _q.Cast(qPrediction.CastPosition); }
                    }
                    else
                    {
                        var qPrediction = _q.GetPrediction(targetQ);
                        if (qPrediction.HitChancePercent >= 60)
                        { _q.Cast(qPrediction.CastPosition); }
                    }
                }
                else if (Varus.CountEnemyChampionsInRange(Varus.GetAutoAttackRange()) >= 1)
                {
                    var distanceQ = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget()).OrderBy(x => x.Distance(Varus)).FirstOrDefault();
                    if (distanceQ != null)
                    { _q.CastMinimumHitchance(distanceQ, 50); }
                }
            }
            if (!_combo["e"].Cast<CheckBox>().CurrentValue || !_e.IsReady() || IsPreAa) return;
            if (EntityManager.Heroes.Enemies.Count(x => x.IsValidTarget(_e.Range)) >= 2)
            {
                _e.CastIfItWillHit();
            }
            var hedefE = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_e.Range) && !SpellShield(x) && !SpellBuff(x) && (!_combo["stackWuseE"].Cast<CheckBox>().CurrentValue || BuffW(x) && GoBuffW(x).Count == 3)).ToList();
            var hedefs = TargetSelector.GetTarget(hedefE, DamageType.Physical);
            if (hedefs != null)
            {
                _e.CastMinimumHitchance(hedefs, _combo["EHitChance"].Cast<Slider>().CurrentValue);
            }
        }
        private static void QAndRAntiGapCloser(AIHeroClient qAndr, Gapcloser.GapcloserEventArgs qAndrGap)
        {
            if (!_misc["Rgap"].Cast<CheckBox>().CurrentValue || !qAndr.IsEnemy || !qAndr.IsValidTarget(1000) || !(Varus.Mana > 200) || !(qAndrGap.End.Distance(Varus) <= 250)) return;
            var prophecyR = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput {
                CollisionTypes = new HashSet<CollisionType> { CollisionType.AiHeroClient, CollisionType.ObjAiMinion },
                Delay = .25f, From = Varus.Position, Radius = 130, Range = 1250, RangeCheckFrom = Varus.Position, Speed = _r.Speed, Target = qAndr, Type = SkillShotType.Linear });
            if (prophecyR.HitChance < HitChance.High) return;
            _r.Cast(prophecyR.CastPosition);
        }
        private static void HasarGostergesi(EventArgs args)
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsHPBarRendered && Varus.Distance(x) < 2000 && x.VisibleOnScreen))
            {
                switch (enemy.Hero)
                {
                    case Champion.Annie: _dikey = -1.8f; _yatay = -9; break;
                    case Champion.Jhin: _dikey = -4.8f; _yatay = -9; break;
                    case Champion.Darius: _dikey = 9.8f; _yatay = -2; break;
                    case Champion.XinZhao: _dikey = 10.8f; _yatay = 2; break;
                    default: _dikey = 9.8f; _yatay = 2; break;
                }
                if (!_drawings["damageQ"].Cast<CheckBox>().CurrentValue) continue;
                var damage = Varus.GetSpellDamage(enemy, SpellSlot.Q) + StacksWDamage(enemy);
                var hasarX = (enemy.TotalShieldHealth() - damage > 0 ? enemy.TotalShieldHealth() - damage : 0) / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                var hasarY = enemy.TotalShieldHealth() / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                var go = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarX * genislik), (int)enemy.HPBarPosition.Y + _dikey);
                var finish = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarY * genislik) + 1, (int)enemy.HPBarPosition.Y + _dikey);
                Drawing.DrawLine(go, finish, yukseklik, Color.FromArgb(180, Color.Green));
            }
        }
        private static float StacksWDamage(Obj_AI_Base unit)
        {
            if (!BuffW(unit)) return 0;
            float[] damageStackW = { 0, 0.02f, 0.0275f, 0.035f, 0.0425f, 0.05f };
            var stacksWCount = GoBuffW(unit).Count;
            var extraDamage = 2 * (Varus.FlatMagicDamageMod / 100);
            var damageW = unit.MaxHealth * damageStackW[_w.Level] * stacksWCount + (extraDamage - extraDamage % 2);
            var expiryDamage = Varus.CalculateDamageOnUnit(unit, DamageType.Magical, damageW > 360 && unit.GetType() != typeof(AIHeroClient) ? 360 : damageW);
            return expiryDamage;
        }
    }
}
