using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AlienAllIn.Tests
{
    public sealed class CharacterInteractionTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        MainRunGameplaySystems game;
        RunLoopController loop;
        MainCoreGameplayUI ui;
        Transform popup;
        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            game = Object.FindAnyObjectByType<MainRunGameplaySystems>(); loop = game.RunLoop;
            ui = Object.FindAnyObjectByType<MainCoreGameplayUI>(); popup = ui.HudCanvas.transform.Find("Character Popup");
            loop.StartRun(); loop.Pause(); game.DebugStopEnemySpawns = game.DebugStopStationSpawns = true;
        }

        [UnityTest]
        public IEnumerator BoardClickOpensSavedPopupPausesZoomsAndBothClosePathsRestoreCorrectState()
        {
            int uiCount = ui.GetComponentsInChildren<RectTransform>(true).Length;
            loop.Resume(); float originalSize = Camera.main.orthographicSize;
            var surface = ui.HudCanvas.transform.Find("Board Drop Surface").gameObject;
            var pointer = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(game.Cat.transform.position) };
            ExecuteEvents.Execute(surface, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(ui.CharacterPopupOpen, Is.True); Assert.That(ui.SelectedActor, Is.EqualTo(game.Cat));
            Assert.That(loop.State, Is.EqualTo(RunState.Paused));
            loop.Resume(); Assert.That(loop.State, Is.EqualTo(RunState.Paused), "Space/Resume cannot bypass a modal pause.");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Camera.main.orthographicSize, Is.LessThan(originalSize * .98f));
            Assert.That(Camera.main.orthographicSize, Is.GreaterThan(originalSize * .8f));
            popup.Find("Upgrade Box/Close").GetComponent<Button>().onClick.Invoke();
            Assert.That(loop.State, Is.EqualTo(RunState.Running));
            loop.Pause(); ui.OpenCharacter(game.Seal);
            popup.Find("Click Outside").GetComponent<Button>().onClick.Invoke();
            Assert.That(ui.CharacterPopupOpen, Is.False); Assert.That(loop.State, Is.EqualTo(RunState.Paused));
            Assert.That(ui.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(uiCount));
            ui.OpenCharacter(game.Seal); game.DebugGrantCash(); ui.RefreshUI();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Character-popup.png"));
            yield return null; yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SingleUpgradeAddsOnlyPointOneAtkAndDoublesIndependentCoinPrices()
        {
            game.DebugGrantCash(); game.DebugGrantChips(20); ui.OpenCharacter(game.Seal);
            float health = game.Seal.EffectiveMaxHealth, cadence = game.Seal.EffectiveAttackInterval, atk = game.Seal.EffectiveDamage;
            var button = popup.Find("Upgrade Box/Upgrade").GetComponent<Button>();
            Assert.That(game.GetAttackUpgradeCost(game.Seal), Is.EqualTo(10));
            button.onClick.Invoke();
            Assert.That(game.Coins, Is.EqualTo(90)); Assert.That(game.Seal.EffectiveDamage, Is.EqualTo(atk + .1f).Within(.001f));
            Assert.That(game.GetAttackUpgradeCost(game.Seal), Is.EqualTo(20));
            button.onClick.Invoke(); Assert.That(game.GetAttackUpgradeCost(game.Seal), Is.EqualTo(40));
            Assert.That(game.GetAttackUpgradeCost(game.Cat), Is.EqualTo(10));
            Assert.That(game.Seal.EffectiveMaxHealth, Is.EqualTo(health)); Assert.That(game.Seal.EffectiveAttackInterval, Is.EqualTo(cadence));
            Assert.That(game.Chips, Is.EqualTo(20)); Assert.That(loop.State, Is.EqualTo(RunState.Paused));
            Assert.That(game.TryBuyAttackUpgrade(game.Duck), Is.False);
            button.onClick.Invoke(); Assert.That(game.Coins, Is.EqualTo(30));
            Assert.That(game.TryBuyAttackUpgrade(game.Seal), Is.False); Assert.That(game.Coins, Is.EqualTo(30));
            ui.CloseCharacter(); loop.Restart(); Assert.That(game.GetAttackUpgradeCost(game.Seal), Is.EqualTo(10));
            Assert.That(game.Chips, Is.Zero); Assert.That(game.Coins, Is.Zero);
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator DefeatedEnemyAwardsOneToTenSharedChipsExactlyOnceClearAwardsNone()
        {
            for (int i = 0; i < 12; i++)
            {
                game.DebugSpawnEnemy(); int before = game.Chips;
                game.Enemies[0].Hurt(999); Call("CleanupRuntimeObjects");
                Assert.That(game.Chips - before, Is.InRange(1, 10));
                int after = game.Chips; Call("CleanupRuntimeObjects"); Assert.That(game.Chips, Is.EqualTo(after));
            }
            game.DebugSpawnEnemy(); int chips = game.Chips; game.DebugKillEnemies(false);
            Assert.That(game.Chips, Is.EqualTo(chips));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator MovingOntoMatchingStationEarnsCoinsWithoutSpendingUltimateGauge()
        {
            game.DebugSpawnStation(ActivityKind.Casino); var station = game.Stations[0];
            Assert.That(game.TryOrderMove(game.Seal, station.Cell), Is.True);
            for (int i = 0; i < 200; i++) Call("AdvanceOrdersAndRescue", .05f);
            Assert.That(game.Seal.CurrentCell, Is.EqualTo(station.Cell));
            Call("AdvanceProcesses", 2f);
            Assert.That(game.Coins, Is.EqualTo(2)); Assert.That(game.Seal.UltimateCharge, Is.Zero);
            Assert.That(station.Progress, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator PufferLocksOneTargetHitsBouncesTwiceStopsAndStartsAnotherCycle()
        {
            game.DebugSetBoardSize(10); int i = 0;
            foreach (var actor in loop.Survivors) { actor.transform.position = game.Board.CellCenter(new Vector2Int(i++, 0)); actor.RefreshCell(); }
            Put(game.Pufferfish, 4, 4); game.DebugSpawnEnemy(EnemyArchetype.RoasterMelee); var target = game.Enemies[0]; Put(target, 6, 4);
            Call("TickPufferCombatState", .01f); Assert.That(game.PufferPhase, Is.EqualTo(PufferAttackPhase.Locked));
            game.DebugSpawnEnemy(); var alternate = game.Enemies[1]; Put(alternate, 4, 6);
            Call("TickPufferCombatState", .6f); Assert.That(game.PufferTarget, Is.EqualTo(target));
            Assert.That(game.PufferPhase, Is.EqualTo(PufferAttackPhase.Launched));
            float health = target.Health;
            for (int step = 0; step < 120 && game.PufferBounceCount < 2; step++) Call("TickPufferMovement", .05f);
            Assert.That(target.Health, Is.LessThan(health)); Assert.That(game.PufferBounceCount, Is.EqualTo(2));
            Assert.That(game.PufferPhase, Is.EqualTo(PufferAttackPhase.Stationary));
            Vector3 stopped = game.Pufferfish.transform.position; Call("TickPufferMovement", 1f);
            Assert.That(game.Pufferfish.transform.position, Is.EqualTo(stopped));
            Put(alternate, game.Pufferfish.CurrentCell.x + 1, game.Pufferfish.CurrentCell.y);
            Call("TickPufferCombatState", game.Pufferfish.EffectiveAttackInterval + .01f);
            Assert.That(game.PufferPhase, Is.Not.EqualTo(PufferAttackPhase.Stationary));
            Assert.That(game.Board.Contains(game.Pufferfish.CurrentCell), Is.True);
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator SealTelegraphMatchesDamageRadiusAndCatBeamConnectsToEnemy()
        {
            game.DebugSetBoardSize(10); Put(game.Seal, 4, 4); Put(game.Cat, 2, 2);
            game.DebugSpawnEnemy(); var target = game.Enemies[0]; Put(target, 5, 4);
            var vfx = Object.FindAnyObjectByType<AttackVfx>(); float health = target.Health;
            Call("TickSeal", .01f); yield return null;
            Assert.That(ui.Feedback.RangeCellCount, Is.EqualTo(9));
            Assert.That(target.Health, Is.EqualTo(health)); Call("TickSeal", .2f); Assert.That(target.Health, Is.LessThan(health));
            Put(target, 3, 2); health = target.Health; Call("TickCat", .01f);
            Assert.That(vfx.CatRenderer.enabled, Is.True); Assert.That(target.Health, Is.LessThan(health));
            Assert.That(vfx.CatRenderer.GetPosition(0), Is.EqualTo(game.Cat.transform.position));
            Assert.That(vfx.CatRenderer.GetPosition(1), Is.EqualTo(target.transform.position));
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Attack-vfx.png"));
            yield return null; yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PufferContinuesAfterDestroyingFirstContactAndCancelsALostLock()
        {
            game.DebugSetBoardSize(10); int i = 0;
            foreach (var actor in loop.Survivors) Put(actor, i++, 0);
            Put(game.Pufferfish, 4, 4); game.DebugSpawnEnemy(); var target = game.Enemies[0]; Put(target, 6, 4);
            Call("TickPufferCombatState", .01f); target.Eliminate(); Call("CleanupRuntimeObjects");
            Call("TickPufferCombatState", .6f); Assert.That(game.PufferPhase, Is.EqualTo(PufferAttackPhase.Stationary));
            game.DebugSpawnEnemy(EnemyArchetype.RoasterMelee); target = game.Enemies[0]; Put(target, 6, 4); target.Hurt(9);
            Call("TickPufferCombatState", 1f);
            for (int step = 0; step < 60 && game.PufferBounceCount == 0; step++) Call("TickPufferMovement", .03f);
            Assert.That(target.Alive, Is.False); Call("CleanupRuntimeObjects");
            yield return null; yield return null;
            Call("TickPufferMovement", .1f);
            Assert.That(game.PufferPhase, Is.EqualTo(PufferAttackPhase.Bouncing));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DuckHasNoDamageCaptureCanBePaidOrCaptorKilledAndExpiredRansomLoses()
        {
            float health = game.Duck.Health; Assert.That(game.Duck.Hurt(9999), Is.Zero); Assert.That(game.Duck.Health, Is.EqualTo(health));
            game.Duck.Eliminate(); Assert.That(game.Duck.Alive, Is.True);
            game.DebugSpawnEnemy(); var captor = game.Enemies[0];
            Call("EnemyHitActor", captor, game.Duck, 999f); Assert.That(game.DuckCaptured, Is.True);
            Assert.That(game.TryFreeDuck(), Is.False); game.DebugGrantChips(5);
            ui.OpenCharacter(game.Duck); ui.RefreshUI();
            Assert.That(popup.Find("Upgrade Box/HP and ATK").GetComponent<Text>().text, Does.Not.Contain("HP"));
            popup.Find("Upgrade Box/Upgrade").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.DuckCaptured, Is.False); Assert.That(game.Chips, Is.Zero); ui.CloseCharacter();
            Call("AdvanceOrdersAndRescue", 4f); Call("EnemyHitActor", captor, game.Duck, 999f);
            captor.Hurt(999); Call("CleanupRuntimeObjects"); Assert.That(game.DuckCaptured, Is.False);
            Call("AdvanceOrdersAndRescue", 4f); game.DebugSpawnEnemy(); captor = game.Enemies[0];
            Call("EnemyHitActor", captor, game.Duck, 999f);
            loop.Resume(); Call("AdvanceOrdersAndRescue", 16f);
            Assert.That(loop.State, Is.EqualTo(RunState.Defeated)); Assert.That(game.Duck.Alive, Is.True);
            loop.Restart(); Assert.That(game.DuckCaptured, Is.False);
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        void Call(string method, params object[] args) => typeof(MainRunGameplaySystems).GetMethod(method, Private).Invoke(game, args);
        void Put(GridActor actor, int x, int y) { actor.transform.position = game.Board.CellCenter(new Vector2Int(x, y)); actor.RefreshCell(); }
    }
}
