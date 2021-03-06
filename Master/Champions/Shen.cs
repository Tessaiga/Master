﻿using System;
using System.Linq;
using System.Collections.Generic;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using LX_Orbwalker;

namespace Master
{
    class Shen : Program
    {
        private const String Version = "1.0.5";
        private Spell SkillP;
        private bool PingCasted = false;

        public Shen()
        {
            SkillQ = new Spell(SpellSlot.Q, 475);
            SkillW = new Spell(SpellSlot.W, 20);
            SkillE = new Spell(SpellSlot.E, 600);
            SkillR = new Spell(SpellSlot.R, 25000);
            SkillP = new Spell(Player.GetSpellSlot("ShenKiAttack", false), LXOrbwalker.GetAutoAttackRange());
            SkillQ.SetTargetted(SkillQ.Instance.SData.SpellCastTime, SkillQ.Instance.SData.MissileSpeed);
            SkillE.SetSkillshot(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.LineWidth, SkillE.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);

            Config.AddSubMenu(new Menu("Combo", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autowusage", "Use W If Hp Under").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "multieusage", "Try Use E Multi Target").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "hsettings"));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarQ", "Use Q").SetValue(true));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarE", "Use E").SetValue(true));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "harModeE", "Use E If Hp Above").SetValue(new Slider(20, 1)));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearW", "Use W").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "alert", "Alert Ally Low Hp").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autoalert", "Alert When Ally Hp Under").SetValue(new Slider(30, 1)));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "pingalert", "Ping Mode").SetValue(new StringList(new[] { "Server", "Local" })));

            Config.AddSubMenu(new Menu("Misc", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAutoE", "Use E If Enemy In Tower Range").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "lasthitQ", "Use Q To Last Hit").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAntiE", "Use E To Anti Gap Closer").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useInterE", "Use E To Interrupt").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "surviveW", "Try Use W To Survive").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "SkinID", "Skin Changer").SetValue(new Slider(6, 0, 6))).ValueChanged += SkinChanger;
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Draw", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawQ", "Q Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E Range").SetValue(true));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnCreate += OnCreate;
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#00ff00\">v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            PacketCast = Config.Item(Name + "packetCast").GetValue<bool>();
            switch (LXOrbwalker.CurrentMode)
            {
                case LXOrbwalker.Mode.Combo:
                    NormalCombo();
                    break;
                case LXOrbwalker.Mode.Harass:
                    Harass();
                    break;
                case LXOrbwalker.Mode.LaneClear:
                    LaneJungClear();
                    break;
                case LXOrbwalker.Mode.LaneFreeze:
                    LaneJungClear();
                    break;
                case LXOrbwalker.Mode.Lasthit:
                    if (Config.Item(Name + "lasthitQ").GetValue<bool>()) LastHit();
                    break;
                case LXOrbwalker.Mode.Flee:
                    if (SkillE.IsReady()) SkillE.Cast(Game.CursorPos, PacketCast);
                    break;
            }
            if (Config.Item(Name + "alert").GetValue<bool>()) UltimateAlert();
            if (Config.Item(Name + "useAutoE").GetValue<bool>()) AutoEInTower();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item(Name + "useAntiE").GetValue<bool>()) return;
            if (gapcloser.Sender.IsValidTarget(SkillE.Range) && SkillE.IsReady()) SkillE.Cast(gapcloser.Sender.Position + Vector3.Normalize(gapcloser.Sender.Position - Player.Position) * 200, PacketCast);
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item(Name + "useInterE").GetValue<bool>()) return;
            if (unit.IsValidTarget(SkillE.Range) && SkillE.IsReady()) SkillE.Cast(unit.Position + Vector3.Normalize(unit.Position - Player.Position) * 200, PacketCast);
        }

        private void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is Obj_SpellMissile && sender.IsValid && Config.Item(Name + "surviveW").GetValue<bool>() && SkillW.IsReady())
            {
                var missle = (Obj_SpellMissile)sender;
                var caster = missle.SpellCaster;
                if (caster.IsEnemy)
                {
                    var ShieldBuff = new Int32[] { 60, 100, 140, 180, 200 }[SkillW.Level - 1] + 0.6 * Player.FlatMagicDamageMod;
                    if (missle.SData.Name.Contains("BasicAttack"))
                    {
                        if (missle.Target.IsMe && Player.Health <= caster.GetAutoAttackDamage(Player, true) && Player.Health + ShieldBuff > caster.GetAutoAttackDamage(Player, true)) SkillW.Cast();
                    }
                    else if (missle.Target.IsMe || missle.EndPosition.Distance(Player.Position) <= 130)
                    {
                        if (missle.SData.Name == "summonerdot")
                        {
                            if (Player.Health <= (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite) && Player.Health + ShieldBuff > (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite)) SkillW.Cast();
                        }
                        else if (Player.Health <= (caster as Obj_AI_Hero).GetSpellDamage(Player, (caster as Obj_AI_Hero).GetSpellSlot(missle.SData.Name, false), 1) && Player.Health + ShieldBuff > (caster as Obj_AI_Hero).GetSpellDamage(Player, (caster as Obj_AI_Hero).GetSpellSlot(missle.SData.Name, false), 1)) SkillW.Cast();
                    }
                }
            }
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            IEnumerable<SpellSlot> ComboQE = new[] { SpellSlot.Q, SpellSlot.E };
            var AADmg = Player.GetAutoAttackDamage(targetObj) + (SkillP.IsReady() ? Player.CalcDamage(targetObj, Damage.DamageType.Magical, 4 + 4 * Player.Level + 0.1 * Player.ScriptHealthBonus) : 0);
            //Game.PrintChat("{0}/{1}", Player.GetAutoAttackDamage(targetObj), 4 + (4 * Player.Level) + (0.1 * Player.ScriptHealthBonus));
            if (targetObj.Health <= Player.GetComboDamage(targetObj, ComboQE) + AADmg)
            {
                if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && SkillQ.InRange(targetObj.Position) && CanKill(targetObj, SkillQ))
                {
                    SkillQ.CastOnUnit(targetObj, PacketCast);
                }
                else if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.Health <= Player.GetComboDamage(targetObj, ComboQE) && SkillE.InRange(targetObj.Position))
                {
                    SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
                    SkillQ.CastOnUnit(targetObj, PacketCast);
                }
                else
                {
                    if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillE.InRange(targetObj.Position)) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
                    if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && SkillQ.InRange(targetObj.Position)) SkillQ.CastOnUnit(targetObj, PacketCast);
                }
            }
            else
            {
                if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillE.InRange(targetObj.Position))
                {
                    if (Config.Item(Name + "multieusage").GetValue<bool>())
                    {
                        SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * ((CheckingCollision(Player, targetObj, SkillE).Count >= 2) ? SkillE.Range : 200), PacketCast);
                    }
                    else SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
                }
                if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && SkillQ.InRange(targetObj.Position)) SkillQ.CastOnUnit(targetObj, PacketCast);
            }
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && SkillE.InRange(targetObj.Position) && Player.Health * 100 / Player.MaxHealth <= Config.Item(Name + "autowusage").GetValue<Slider>().Value) SkillW.Cast(PacketCast);
            if (Config.Item(Name + "iusage").GetValue<bool>() && Items.CanUseItem(Rand) && Player.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void Harass()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "useHarE").GetValue<bool>())
            {
                if (SkillE.IsReady() && SkillE.InRange(targetObj.Position) && Player.Health * 100 / Player.MaxHealth >= Config.Item(Name + "harModeE").GetValue<Slider>().Value) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
            }
            if (Config.Item(Name + "useHarQ").GetValue<bool>() && SkillQ.IsReady() && SkillQ.InRange(targetObj.Position)) SkillQ.CastOnUnit(targetObj, PacketCast);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault();
            if (minionObj == null) return;
            if (Config.Item(Name + "useClearW").GetValue<bool>() && SkillW.IsReady() && LXOrbwalker.InAutoAttackRange(minionObj)) SkillW.Cast();
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady()) SkillQ.CastOnUnit(minionObj, PacketCast);
        }

        private void LastHit()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault(i => CanKill(i, SkillQ));
            if (minionObj != null && SkillQ.IsReady()) SkillQ.CastOnUnit(minionObj, PacketCast);
        }

        private void UltimateAlert()
        {
            if (!SkillR.IsReady()) return;
            foreach (var allyObj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsAlly && !i.IsMe && !i.IsDead && i.CountEnemysInRange(800) >= 1 && (i.Health * 100 / i.MaxHealth) <= Config.Item(Name + "autoalert").GetValue<Slider>().Value))
            {
                if (!PingCasted)
                {
                    Game.PrintChat("Use Ultimate (R) To Help: {0}", allyObj.ChampionName);
                    for (Int32 i = 0; i < 5; i++)
                    {
                        switch (Config.Item(Name + "pingalert").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                                Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(allyObj.Position.X, allyObj.Position.Y, allyObj.NetworkId, Player.NetworkId, Packet.PingType.Fallback, false)).Process();
                                break;
                            case 1:
                                Packet.C2S.Ping.Encoded(new Packet.C2S.Ping.Struct(allyObj.Position.X, allyObj.Position.Y, allyObj.NetworkId, Packet.PingType.Fallback)).Process();
                                break;
                        }
                    }
                    PingCasted = true;
                    Utility.DelayAction.Add(5000, () => PingCasted = false);
                }
            }
        }

        private void AutoEInTower()
        {
            if (Utility.UnderTurret() || !SkillE.IsReady()) return;
            var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(i => Utility.UnderTurret(i) && SkillE.InRange(i.Position));
            if (target != null) SkillE.Cast(target.Position + Vector3.Normalize(target.Position - Player.Position) * 200, PacketCast);
        }
    }
}