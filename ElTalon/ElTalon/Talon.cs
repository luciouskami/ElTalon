using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


namespace ElTalon
{

    internal class Talon
    {

        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public static Menu Menu;
        private static Orbwalking.Orbwalker orbwalker;
        private static Spell Q, W, E, R;
        private static List<Spell> SpellList;

        // Items
        private static Items.Item Tiamat, Hydra, Youmuu;

        // Summoner spells
        private static SpellSlot Ignite;

        //this is godlike
        private const float Jqueryluckynumber = 400f;


        #region Gameloaded 

        public static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.CharData.BaseSkinName != "Talon")
                return;


            #region Spell Data

            // set spells
            Q = new Spell(SpellSlot.Q, 0);
            W = new Spell(SpellSlot.W, 650);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 650);

            W.SetSkillshot(0.25f, 75, 2300, false, SkillshotType.SkillshotLine);

            SpellList = new List<Spell> { Q, E, W, R };

            // Ignite
            Ignite = Player.GetSpellSlot("summonerdot");

            // Items
            Tiamat = new Items.Item(3077, 400f);
            Youmuu = new Items.Item(3142, 0f);
            Hydra = new Items.Item(3074, 400f); 

            InitializeMenu();

            #endregion

            //subscribe to event
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += AfterAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            new AssassinManager();
        }

        #endregion

        #region OnGameUpdate

        private static void OnGameUpdate(EventArgs args)
        {
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
            }
        }

        #endregion

        #region LaneClear

        private static void JungleClear()
        {
            var qWaveClear = Menu.Item("WaveClearQ").GetValue<bool>();
            var wWaveClear = Menu.Item("WaveClearW").GetValue<bool>();
            var eWaveClear = Menu.Item("WaveClearE").GetValue<bool>();
            var hydraClear = Menu.Item("HydraClear").GetValue<bool>();
            var tiamatClear = Menu.Item("TiamatClear").GetValue<bool>();

            var target = MinionManager.GetMinions(Player.Position, 700, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (Player.ManaPercent >= Menu.Item("LaneClearMana").GetValue<Slider>().Value)
            {
                if (qWaveClear && Q.IsReady() && target.IsValidTarget())
                {
                    Q.Cast();
                }

                if (wWaveClear && W.IsReady() && target.IsValidTarget())
                {
                    W.Cast(target);
                }

                if (eWaveClear && E.IsReady())
                {
                    E.CastOnUnit(target);
                }
            }

            if (Items.CanUseItem(3074) && hydraClear && target.IsValidTarget(Hydra.Range))
                Items.UseItem(3074);

            if (Items.CanUseItem(3077) && tiamatClear && target.IsValidTarget(Tiamat.Range))
                Items.UseItem(3077);
        }

        private static void LaneClear()
        {
            var minion = MinionManager.GetMinions(Player.ServerPosition, W.Range).FirstOrDefault();
            if (minion == null || minion.Name.ToLower().Contains("ward")) return;

            var qWaveClear = Menu.Item("WaveClearQ").GetValue<bool>();
            var wWaveClear = Menu.Item("WaveClearW").GetValue<bool>();
            var eWaveClear = Menu.Item("WaveClearE").GetValue<bool>();
            var hydraClear = Menu.Item("HydraClear").GetValue<bool>();
            var tiamatClear = Menu.Item("TiamatClear").GetValue<bool>();
            var minions = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (Player.ManaPercent >= Menu.Item("LaneClearMana").GetValue<Slider>().Value)
            {
                if (qWaveClear && Q.IsReady() && minion.IsValidTarget())
                {
                    Q.Cast();
                }

                if (wWaveClear && W.IsReady() && minion.IsValidTarget())
                {
                    if (minions.Count > 2)
                    {
                        var farmLocation = W.GetCircularFarmLocation(minions);
                        W.Cast(farmLocation.Position);
                    }
                }

                if (eWaveClear && E.IsReady())
                {
                    E.CastOnUnit(minion);
                }
            }

            if (Items.CanUseItem(3074) && hydraClear && minion.IsValidTarget(Hydra.Range) && minions.Count() > 1)
                Items.UseItem(3074);

            if (Items.CanUseItem(3077) && tiamatClear && minion.IsValidTarget(Tiamat.Range) && minions.Count() > 1)
                Items.UseItem(3077);
        }

        #endregion  

        #region Harass

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            var qHarass = Menu.Item("HarassQ").GetValue<bool>();
            var wHarass = Menu.Item("HarassW").GetValue<bool>();
            var eHarass = Menu.Item("HarassE").GetValue<bool>();

            foreach (var spell in SpellList.Where(y => y.IsReady()))
            {
                if (Player.ManaPercent >= Menu.Item("HarassMana").GetValue<Slider>().Value)
                {                             
                   if (spell.Slot == SpellSlot.Q && qHarass && Q.IsReady() && Player.Distance(target) <= Player.AttackRange && Q.IsReady())
                    {   
                        Q.Cast();
                    }

                    if (spell.Slot == SpellSlot.W && wHarass && W.IsReady())
                    {
                        W.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                    }

                    if (spell.Slot == SpellSlot.E && eHarass && E.IsReady())
                    {
                        E.Cast(target);
                    }
                }
            }
        }

        #endregion

        #region itemusage

        private static void FightItems()
        {
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);

            var tiamatItem = Menu.Item("UseTiamat").GetValue<bool>();
            var hydraItem = Menu.Item("UseHydra").GetValue<bool>();
   
            if (Items.CanUseItem(3074) && hydraItem && Player.Distance(target) <= Jqueryluckynumber)
                Items.UseItem(3074);

            if (Items.CanUseItem(3077) && tiamatItem && Player.Distance(target) <= Jqueryluckynumber)
                Items.UseItem(3077);
        }

        #endregion

        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    if (unit.IsMe && Q.IsReady() && target is Obj_AI_Hero)
                    {
                        Q.Cast(); 
                        Orbwalking.ResetAutoAttackTimer();
                    }
                break;
            }
        }

        #region Combo

        private static void Combo()
        {
            var target = GetEnemy(W.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            var useW = Menu.Item("WCombo").GetValue<bool>();
            var useE = Menu.Item("ECombo").GetValue<bool>();
            var rCombo = Menu.Item("RCombo").GetValue<bool>();
            var onlyKill = Menu.Item("RWhenKill").GetValue<bool>();
            var smartUlt = Menu.Item("SmartUlt").GetValue<bool>();
            var youmuuitem = Menu.Item("UseYoumuu").GetValue<bool>();
            var ultCount = Menu.Item("rcount").GetValue<Slider>().Value;

            var comboDamage = GetComboDamage(target);
            var getUltComboDamage = GetUltComboDamage(target);


            var ultType = Menu.Item("ElTalon.Combo.Mode").GetValue<StringList>().SelectedIndex;

            switch (ultType)
            {
                case 0:

                    if (useE && E.IsReady())
                    {
                        E.Cast(target);
                    }
                    FightItems();
                    if (Player.Distance(target) <= Player.AttackRange)
                    {
                       Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }

                    if (Q.IsReady())
                    {
                        Q.Cast(target);
                        
                    }

                    if (useW && W.IsReady())
                    {
                        W.Cast(target);
                    }

                    

                    if (onlyKill && E.IsReady() && rCombo && Q.IsReady() && ObjectManager.Get<Obj_AI_Hero>().Count(aiHero => aiHero.IsValidTarget(R.Range)) >= ultCount)
                    {
                        if (comboDamage >= target.Health)
                        {
                            R.Cast();
                        }
                    }

                    if (onlyKill && R.IsReady() && smartUlt)
                    {
                        if (getUltComboDamage >= target.Health)
                        {
                            R.Cast();
                        }
                    }

                    if (!onlyKill && E.IsReady() && rCombo && Q.IsReady() && ObjectManager.Get<Obj_AI_Hero>().Count(aiHero => aiHero.IsValidTarget(R.Range)) >= ultCount)
                    {
                        R.Cast();
                    }


                    break;

                //R->E->W->Q
                case 1:

                    if (R.IsReady() && R.IsInRange(target))
                    {
                        R.Cast();
                    }

                    if (useE && E.IsReady())
                    {
                        E.Cast(target);
                    }

                    if (useW && W.IsReady())
                    {
                        W.Cast(target);
                    }

                    FightItems();
                    break;

                case 2:


                    if (useE && E.IsReady())
                    {
                        E.Cast(target);
                    }

                    if (R.IsReady() && R.IsInRange(target))
                    {
                        R.Cast();
                    }

                    if (useW && W.IsReady())
                    {
                        W.Cast(target);
                    }

                    FightItems();
                    break;
            }

            if (youmuuitem && Player.Distance(target) <= Jqueryluckynumber && Youmuu.IsReady())
            {
                Youmuu.Cast();
            }

            //ignite when killable
            if (Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health &&
                Menu.Item("UseIgnite").GetValue<bool>())
            {
                Player.Spellbook.CastSpell(Ignite, target);
            }
        }

        #endregion

        #region betaDamage   

        private static float BetaDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);
            }

            return (float)damage;
        }

        #endregion
        
        //new logic such OP 
        #region GetUltComboDamage   

        private static float GetUltComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);
            }

            return (float)damage;
        }

        #endregion

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var antiGapActive = Menu.Item("Antigap").GetValue<bool>();
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            if (antiGapActive && E.IsReady() && gapcloser.Sender.Distance(Player) < 700)
                E.Cast(target);
        }


        #region GetComboDamage   

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);
            }

            return (float)damage;
        }

        #endregion

        #region Ignite

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (Ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        #endregion


        private static Obj_AI_Hero GetEnemy(float vDefaultRange = 0, TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(vDefaultRange) < 0.00001)
                vDefaultRange = Q.Range;

            if (!Menu.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = Menu.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        enemy =>
                            enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            Menu.Item("Assassin" + enemy.ChampionName) != null &&
                            Menu.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            ObjectManager.Player.Distance(enemy) < assassinRange);

            if (Menu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            Obj_AI_Hero t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        #region Menu

        private static void InitializeMenu()
        {
            Menu = new Menu("ElTalon", "Talon", true);

            //Orbwalker
            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);

            //TargetSelector
            var targetSelector = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelector);
            Menu.AddSubMenu(targetSelector);

            //Combo
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", "Combo"));
            comboMenu.AddItem(new MenuItem("fsfsafsaasffsadddd111dsasd", ""));
            comboMenu.AddItem(new MenuItem("QCombo", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("WCombo", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("ECombo", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("RCombo", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("fsfsafsaasffsa", ""));
            comboMenu.AddItem(new MenuItem("RWhenKill", "Use R only when killable").SetValue(true));
            comboMenu.AddItem(new MenuItem("SmartUlt", "Use smart ult").SetValue(true));
            comboMenu.AddItem(new MenuItem("rcount", "Min target to R >= ")).SetValue(new Slider(1, 1, 5));
            comboMenu.AddItem(new MenuItem("UseIgnite", "Use Ignite in combo when killable").SetValue(true));

            comboMenu.SubMenu("Combo mode").AddItem(new MenuItem("ElTalon.Combo.Mode", "Mode").SetValue(new StringList(new[] { "Default [E->AA->Q->W->R]", "R->E->W->Q", "E->R->W->Q" })));

            comboMenu.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            comboMenu.SubMenu("Items").AddItem(new MenuItem("UseTiamat", "Use Tiamat").SetValue(true));
            comboMenu.SubMenu("Items").AddItem(new MenuItem("UseHydra", "Use Hydra").SetValue(true));
            comboMenu.SubMenu("Items").AddItem(new MenuItem("UseYoumuu", "Use Youmuu").SetValue(true));

            //Harass
            var harassMenu = Menu.AddSubMenu(new Menu("Harass", "H"));
            harassMenu.AddItem(new MenuItem("fsfsafsaasffsadddd", ""));
            harassMenu.AddItem(new MenuItem("HarassQ", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("HarassW", "Use W").SetValue(true));
            harassMenu.AddItem(new MenuItem("HarassE", "Use E").SetValue(false));

            harassMenu.SubMenu("HarassMana").AddItem(new MenuItem("HarassMana", "[Harass] Minimum Mana").SetValue(new Slider(30, 0, 100)));
            harassMenu.AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //Waveclear
            var waveClearMenu = Menu.AddSubMenu(new Menu("WaveClear", "waveclear"));
            waveClearMenu.AddItem(new MenuItem("fsfsafsaasffsadddd111", ""));
            waveClearMenu.AddItem(new MenuItem("WaveClearQ", "Use Q").SetValue(true));
            waveClearMenu.AddItem(new MenuItem("WaveClearW", "Use W").SetValue(true));
            waveClearMenu.AddItem(new MenuItem("WaveClearE", "Use E").SetValue(false));
            waveClearMenu.SubMenu("Mana").AddItem(new MenuItem("LaneClearMana", "[WaveClear] Minimum Mana").SetValue(new Slider(30, 0, 100)));
            waveClearMenu.AddItem(new MenuItem("fsfsafsaasffsadddd11sss1", ""));

            // Settings
            var settingsMenu = Menu.AddSubMenu(new Menu("Misc", "SuperSecretSettings"));
            settingsMenu.AddItem(new MenuItem("Antigap", "[BETA] Use E for gapclosers").SetValue(false));

            // item usage
            waveClearMenu.SubMenu("Items").AddItem(new MenuItem("HydraClear", "Use hydra").SetValue(true));
            waveClearMenu.SubMenu("Items").AddItem(new MenuItem("TiamatClear", "Use tiamat").SetValue(true));
            waveClearMenu.AddItem(new MenuItem("WaveClearActive", "WaveClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            //Misc
            var miscMenu = Menu.AddSubMenu(new Menu("Drawings", "Misc"));
            miscMenu.AddItem(new MenuItem("ElTalon.Drawingsoff", "Drawings off").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElTalon.DrawW", "Draw W").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElTalon.DrawE", "Draw E").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElTalon.DrawR", "Draw R").SetValue(new Circle()));

            var dmgAfterComboItem = new MenuItem("ElTalon.DrawComboDamage", "Draw combo damage").SetValue(true);
            miscMenu.AddItem(dmgAfterComboItem);

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs) { Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>(); };


            //Here comes the moneyyy, money, money, moneyyyy
            var credits = Menu.AddSubMenu(new Menu("Credits", "jQuery"));
            credits.AddItem(new MenuItem("Paypal", "if you would like to donate via paypal:"));
            credits.AddItem(new MenuItem("Email", "info@zavox.nl"));


            Menu.AddItem(new MenuItem("422442fsaafs4242f", ""));
            Menu.AddItem(new MenuItem("422442fsaafsf", "Version: 2.0.0.1"));
            Menu.AddItem(new MenuItem("fsasfafsfsafsa", "Made By jQuery"));

            Menu.AddToMainMenu();
        }

        #endregion

        #region Drawings

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawOff = Menu.Item("ElTalon.Drawingsoff").GetValue<bool>();
            var drawE = Menu.Item("ElTalon.DrawE").GetValue<Circle>();
            var drawW = Menu.Item("ElTalon.DrawW").GetValue<Circle>();
            var drawR = Menu.Item("ElTalon.DrawR").GetValue<Circle>();

            if (drawOff)
                return;

            if (drawW.Active)
                if (W.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

            if (drawE.Active)
                if (E.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (drawR.Active)
                if (R.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
        }
        #endregion
    }
}
