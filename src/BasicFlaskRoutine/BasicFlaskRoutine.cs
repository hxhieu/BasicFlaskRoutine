using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TreeRoutine.DefaultBehaviors.Actions;
using TreeRoutine.DefaultBehaviors.Helpers;
using TreeRoutine.FlaskComponents;
using TreeRoutine.TreeSharp;

namespace TreeRoutine.Routine.BasicFlaskRoutine
{
    public partial class BasicFlaskRoutine : BaseTreeRoutinePlugin<BasicFlaskRoutineSettings, BaseTreeCache>
    {
        public BasicFlaskRoutine() : base()
        {

        }

        public Composite Tree { get; set; }
        private Coroutine TreeCoroutine { get; set; }
        public Object LoadedMonstersLock { get; set; } = new Object();
        public List<Entity> LoadedMonsters { get; protected set; } = new List<Entity>();


        private KeyboardHelper KeyboardHelper { get; set; } = null;

        private Stopwatch PlayerMovingStopwatch { get; set; } = new Stopwatch();

        private const string basicFlaskRoutineChecker = "BasicFlaskRoutine Checker";



        public override bool Initialise()
        {
            base.Initialise();

            Name = "BasicFlaskRoutine";
            KeyboardHelper = new KeyboardHelper(GameController);

            Tree = CreateTree();

            // Add this as a coroutine for this plugin
            Settings.Enable.OnValueChanged += (sender, b) =>
            {
                if (b)
                {
                    if (Core.ParallelRunner.FindByName(basicFlaskRoutineChecker) == null) InitCoroutine();
                    TreeCoroutine?.Resume();
                }
                else
                    TreeCoroutine?.Pause();

            };
            InitCoroutine();

            Settings.TicksPerSecond.OnValueChanged += (sender, b) =>
            {
                UpdateCoroutineWaitRender();
            };

            return true;
        }

        private void InitCoroutine()
        {
            TreeCoroutine = new Coroutine(() => TickTree(Tree), new WaitTime(1000 / Settings.TicksPerSecond), this, "BasicFlaskRoutine Tree");
            Core.ParallelRunner.Run(TreeCoroutine);
        }

        private void UpdateCoroutineWaitRender()
        {
            TreeCoroutine.UpdateCondtion(new WaitTime(1000 / Settings.TicksPerSecond));
        }

        protected override void UpdateCache()
        {
            base.UpdateCache();

            UpdatePlayerMovingStopwatch();
        }

        private void UpdatePlayerMovingStopwatch()
        {
            var player = GameController.Player.GetComponent<Actor>();
            if (player != null && player.Address != 0 && player.isMoving)
            {
                if (!PlayerMovingStopwatch.IsRunning)
                    PlayerMovingStopwatch.Start();
            }
            else
            {
                PlayerMovingStopwatch.Reset();
            }
        }

        private Composite CreateTree()
        {
            return new Decorator(x => TreeHelper.CanTick() && !PlayerHelper.isPlayerDead() && (!Cache.InHideout || Settings.EnableInHideout) && PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "grace_period" }),
                    new PrioritySelector
                    (
                        new Decorator(x => Settings.AutoFlask,
                        new PrioritySelector(
                            CreateInstantHPPotionComposite(),
                            CreateHPPotionComposite(),
                            CreateInstantManaPotionComposite(),
                            CreateManaPotionComposite()
                            )
                        ),
                        CreateAilmentPotionComposite(),
                        CreateDefensivePotionComposite(),
                        CreateSpeedPotionComposite(),
                        CreateOffensivePotionComposite()
                    )
                );
        }

        private Composite CreateInstantHPPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isHealthBelowPercentage(Settings.InstantHPPotion)),
                new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Life, true, true),
                    CreateUseFlaskAction(FlaskActions.Hybrid, true, true),
                    CreateUseFlaskAction(FlaskActions.Life),
                    CreateUseFlaskAction(FlaskActions.Hybrid)
                )
            );

        }

        private Composite CreateHPPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isHealthBelowPercentage(Settings.HPPotion)),
                new Decorator((x => PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_effect_life" })),
                 new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Life, false),
                    CreateUseFlaskAction(FlaskActions.Hybrid, false)
                 )
                )
            );
        }

        private Composite CreateInstantManaPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isManaBelowPercentage(Settings.InstantManaPotion)),
                new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Mana, true, true),
                    CreateUseFlaskAction(FlaskActions.Hybrid, true, true),
                    CreateUseFlaskAction(FlaskActions.Mana),
                    CreateUseFlaskAction(FlaskActions.Hybrid)

                )
            );
        }

        private Composite CreateManaPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isManaBelowPercentage(Settings.ManaPotion) || PlayerHelper.isManaBelowValue(Settings.MinManaFlask)),
                new Decorator((x => PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_effect_mana", "flask_effect_mana_not_removed_when_full" })),
                    new PrioritySelector(
                        CreateUseFlaskAction(FlaskActions.Mana, false),
                        CreateUseFlaskAction(FlaskActions.Hybrid, false)
                    )
                )
            );
        }

        private Composite CreateSpeedPotionComposite()
        {
            return new Decorator((x => Settings.SpeedFlaskEnable && Settings.MinMsPlayerMoving <= PlayerMovingStopwatch.ElapsedMilliseconds && (PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_bonus_movement_speed", "flask_utility_sprint" }) || (!Settings.SilverFlaskEnable || PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_utility_haste" })))),
                new PrioritySelector(
                    new Decorator((x => Settings.QuicksilverFlaskEnable), CreateUseFlaskAction(FlaskActions.Speedrun)),
                    new Decorator((x => Settings.SilverFlaskEnable), CreateUseFlaskAction(FlaskActions.OFFENSE_AND_SPEEDRUN))
                )
            );
        }

        private Composite CreateDefensivePotionComposite()
        {
            return new Decorator((x => Settings.DefensiveFlaskEnable && (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentDefensive) || PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentDefensive) || Settings.DefensiveMonsterCount > 0 && HasEnoughNearbyMonsters(Settings.DefensiveMonsterCount, Settings.DefensiveMonsterDistance, Settings.DefensiveCountNormalMonsters, Settings.DefensiveCountRareMonsters, Settings.DefensiveCountMagicMonsters, Settings.DefensiveCountUniqueMonsters, Settings.DefensiveIgnoreFullHealthUniqueMonsters))),
                new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Defense),
                    new Decorator((x => Settings.OffensiveAsDefensiveEnable), CreateUseFlaskAction(new List<FlaskActions> { FlaskActions.OFFENSE_AND_SPEEDRUN, FlaskActions.Defense }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ? new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid } : null)))
                )
            );
        }

        private Composite CreateOffensivePotionComposite()
        {
            return new PrioritySelector(
                new Decorator((x => Settings.OffensiveFlaskEnable && (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentOffensive) || PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentOffensive) || Settings.OffensiveMonsterCount > 0 && HasEnoughNearbyMonsters(Settings.OffensiveMonsterCount, Settings.OffensiveMonsterDistance, Settings.OffensiveCountNormalMonsters, Settings.OffensiveCountRareMonsters, Settings.OffensiveCountMagicMonsters, Settings.OffensiveCountUniqueMonsters, Settings.OffensiveIgnoreFullHealthUniqueMonsters))),
                    CreateUseFlaskAction(new List<FlaskActions> { FlaskActions.Offense, FlaskActions.OFFENSE_AND_SPEEDRUN }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ? new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid } : null)))
            );
        }

        private Composite CreateAilmentPotionComposite()
        {
            return new Decorator(x => Settings.RemAilment,
                new PrioritySelector(
                    new Decorator(x => Settings.RemBleed, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Bleeding, CreateUseFlaskAction(FlaskActions.BleedImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemBurning, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Burning, CreateUseFlaskAction(FlaskActions.IgniteImmune, isCleansing: true))),
                    CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Corruption, CreateUseFlaskAction(FlaskActions.BleedImmune, isCleansing: true), (() => Settings.CorruptCount)),
                    new Decorator(x => Settings.RemFrozen, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Frozen, CreateUseFlaskAction(FlaskActions.FreezeImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemPoison, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Poisoned, CreateUseFlaskAction(FlaskActions.PoisonImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemShocked, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Shocked, CreateUseFlaskAction(FlaskActions.ShockImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemCurse, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.WeakenedSlowed, CreateUseFlaskAction(FlaskActions.CurseImmune, isCleansing: true)))
                    )
                );
        }

        private Composite CreateUseFlaskAction(FlaskActions flaskAction, Boolean? instant = null, Boolean ignoreBuffs = false, Func<List<FlaskActions>> ignoreFlasksWithAction = null, Boolean isCleansing = false)
        {
            return CreateUseFlaskAction(new List<FlaskActions> { flaskAction }, instant, ignoreBuffs, ignoreFlasksWithAction, isCleansing);
        }

        private Composite CreateUseFlaskAction(List<FlaskActions> flaskActions, Boolean? instant = null, Boolean ignoreBuffs = false, Func<List<FlaskActions>> ignoreFlasksWithAction = null, Boolean isCleansing = false)
        {
            return new UseHotkeyAction(KeyboardHelper, x =>
            {
                var foundFlask = FindFlaskMatchingAnyAction(flaskActions, instant, ignoreBuffs, ignoreFlasksWithAction, isCleansing);

                if (foundFlask == null)
                {
                    return null;
                }

                return Settings.FlaskSettings[foundFlask.Index].Hotkey;
            });
        }

        private Boolean HasEnoughNearbyMonsters(int minimumMonsterCount, int maxDistance, bool countNormal, bool countRare, bool countMagic, bool countUnique, bool ignoreUniqueIfFullHealth)
        {
            Log("HXHIEU: HasEnoughNearbyMonsters", 2);
            var mobCount = 0;
            var maxDistanceSquare = maxDistance * maxDistance;

            var playerPosition = GameController.Player.Pos;

            if (LoadedMonsters != null)
            {
                List<Entity> localLoadedMonsters = null;
                lock (LoadedMonstersLock)
                {
                    localLoadedMonsters = new List<Entity>(LoadedMonsters);
                }

                // Make sure we create our own list to iterate as we may be adding/removing from the list
                foreach (var monster in localLoadedMonsters)
                {
                    if (!monster.HasComponent<Monster>() || !monster.IsValid || !monster.IsAlive || !monster.IsHostile)
                        continue;

                    var monsterType = monster.GetComponent<ObjectMagicProperties>().Rarity;

                    // Don't count this monster type if we are ignoring it
                    if (monsterType == MonsterRarity.White && !countNormal
                        || monsterType == MonsterRarity.Rare && !countRare
                        || monsterType == MonsterRarity.Magic && !countMagic
                        || monsterType == MonsterRarity.Unique && !countUnique)
                        continue;

                    if (monsterType == MonsterRarity.Unique && ignoreUniqueIfFullHealth)
                    {
                        Life monsterLife = monster.GetComponent<Life>();
                        if (monsterLife == null)
                            continue;

                        if (monsterLife.HPPercentage > 0.995)
                            continue;
                    }

                    var monsterPosition = monster.Pos;

                    var xDiff = playerPosition.X - monsterPosition.X;
                    var yDiff = playerPosition.Y - monsterPosition.Y;
                    var monsterDistanceSquare = (xDiff * xDiff + yDiff * yDiff);

                    if (monsterDistanceSquare <= maxDistanceSquare)
                    {
                        mobCount++;
                    }

                    if (mobCount >= minimumMonsterCount)
                    {
                        if (Settings.Debug)
                        {
                            Log("NearbyMonstersCondition returning true because " + mobCount + " mobs valid monsters were found nearby.", 2);
                        }
                        return true;
                    }
                }
            }
            else if (Settings.Debug)
            {
                Log("NearbyMonstersCondition returning false because mob list was invalid.", 2);
            }

            if (Settings.Debug)
            {
                Log("NearbyMonstersCondition returning false because " + mobCount + " mobs valid monsters were found nearby.", 2);
            }
            return false;
        }

        private PlayerFlask FindFlaskMatchingAnyAction(FlaskActions flaskAction, Boolean? instant = null, Boolean ignoreBuffs = false, Func<List<FlaskActions>> ignoreFlasksWithAction = null, Boolean isCleansing = false)
        {
            return FindFlaskMatchingAnyAction(new List<FlaskActions> { flaskAction }, instant, ignoreBuffs, ignoreFlasksWithAction, isCleansing);
        }

        private PlayerFlask FindFlaskMatchingAnyAction(List<FlaskActions> flaskActions, Boolean? instant = null, Boolean ignoreBuffs = false, Func<List<FlaskActions>> ignoreFlasksWithAction = null, Boolean isCleansing = false)
        {
            var allFlasks = FlaskHelper.GetAllFlaskInfo();

            // We have no flasks or settings for flasks?
            if (allFlasks == null || Settings.FlaskSettings == null)
            {
                if (Settings.Debug)
                {
                    if (allFlasks == null)
                        LogMessage(Name + ": No flasks to match against.", 5);
                    else if (Settings.FlaskSettings == null)
                        LogMessage(Name + ": Flask settings were null. Hopefully doesn't happen frequently.", 5);
                }

                return null;
            }

            if (Settings.Debug)
            {
                foreach (var flask in allFlasks)
                {
                    LogMessage($"{Name}: Flask: {flask.Name} Slot: {flask.Index} Instant: {flask.InstantType} Action1: {flask.Action1} Action2: {flask.Action2}", 5);
                }
            }

            List<FlaskActions> ignoreFlaskActions = ignoreFlasksWithAction == null ? null : ignoreFlasksWithAction();

            var flaskList = allFlasks
                    .Where(x =>
                    Settings.FlaskSettings[x.Index].Enabled
                    && FlaskHasAvailableAction(flaskActions, ignoreFlaskActions, x)
                    && FlaskHelper.CanUsePotion(x, Settings.FlaskSettings[x.Index].ReservedUses, isCleansing)
                    && FlaskMatchesInstant(x, instant)
                    && (ignoreBuffs || MissingFlaskBuff(x))
                    ).OrderByDescending(x => flaskActions.Contains(x.Action1)).ThenByDescending(x => x.TotalUses - Settings.FlaskSettings[x.Index].ReservedUses).ToList();

            if (flaskList == null || !flaskList.Any())
            {
                if (Settings.Debug)
                    LogError(Name + ": No flasks found for action: (instant:" + instant + ") " + flaskActions[0], 1);
                return null;
            }

            if (Settings.Debug)
                LogMessage(Name + ": Flask(s) found for action: " + flaskActions[0] + " Flask Count: " + flaskList.Count(), 1);

            return flaskList.FirstOrDefault();
        }

        private bool FlaskHasAvailableAction(List<FlaskActions> flaskActions, List<FlaskActions> ignoreFlaskActions, PlayerFlask flask)
        {
            return flaskActions.Any(x => x == flask.Action1 || x == flask.Action2)
                    && (ignoreFlaskActions == null || !ignoreFlaskActions.Any(x => x == flask.Action1 || x == flask.Action2));
        }

        private bool FlaskMatchesInstant(PlayerFlask playerFlask, Boolean? instant)
        {
            return instant == null
                    || instant == false && CanUseFlaskAsRegen(playerFlask)
                    || instant == true && CanUseFlaskAsInstant(playerFlask);
        }

        private bool CanUseFlaskAsInstant(PlayerFlask playerFlask)
        {
            // If the flask is instant, no special logic needed
            return playerFlask.InstantType == FlaskInstantType.Partial
                    || playerFlask.InstantType == FlaskInstantType.Full
                    || playerFlask.InstantType == FlaskInstantType.LowLife && PlayerHelper.isHealthBelowPercentage(35);
        }

        private bool CanUseFlaskAsRegen(PlayerFlask playerFlask)
        {
            return playerFlask.InstantType == FlaskInstantType.None
                    || playerFlask.InstantType == FlaskInstantType.Partial && !Settings.ForceBubblingAsInstantOnly
                    || playerFlask.InstantType == FlaskInstantType.LowLife && !Settings.ForcePanickedAsInstantOnly;
        }

        private bool MissingFlaskBuff(PlayerFlask playerFlask)
        {
            return !PlayerHelper.playerHasBuffs(new List<string> { playerFlask.BuffString1 }) || !PlayerHelper.playerHasBuffs(new List<string> { playerFlask.BuffString2 });
        }

        private Decorator CreateCurableDebuffDecorator(Dictionary<string, int> dictionary, Composite child, Func<int> minCharges = null)
        {
            return new Decorator((x =>
            {
                var buffs = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>().Buffs;
                foreach (var buff in buffs)
                {
                    if (float.IsInfinity(buff.Timer))
                        continue;

                    int filterId = 0;
                    if (dictionary.TryGetValue(buff.Name, out filterId))
                    {
                        // I'm not sure what the values are here, but this is the effective logic from the old plugin
                        return (filterId == 0 || filterId != 1) && (minCharges == null || buff.Charges >= minCharges());
                    }
                }
                return false;
            }), child);
        }

        public override void Render()
        {
            base.Render();
            if (!Settings.Enable.Value) return;
        }

        public override void EntityAdded(Entity entityWrapper)
        {
            if (entityWrapper.HasComponent<Monster>())
            {
                lock (LoadedMonstersLock)
                {
                    LoadedMonsters.Add(entityWrapper);
                }
            }
        }

        public override void EntityRemoved(Entity entityWrapper)
        {
            lock (LoadedMonstersLock)
            {
                LoadedMonsters.Remove(entityWrapper);
            }
        }
    }
}