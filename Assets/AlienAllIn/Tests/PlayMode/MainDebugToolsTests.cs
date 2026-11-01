using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AlienAllIn.Tests
{
    public sealed class MainDebugToolsTests
    {
        MainRunGameplaySystems game;
        RunLoopController loop;
        MainDebugPanel panel;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            game = Object.FindAnyObjectByType<MainRunGameplaySystems>(); loop = game.RunLoop;
            panel = Object.FindAnyObjectByType<MainDebugPanel>();
            Assert.That(panel, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator OKeyOpensAndClosesWhilePausedAndBlocksBoardInput()
        {
            Assert.That(panel.IsOpen, Is.False);
            loop.StartRun(); loop.Pause();
            var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            // The test runner may run without Game view focus; route the virtual keyboard to gameplay.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.O));
                yield return null; yield return null;
                Assert.That(panel.IsOpen, Is.True);
                Canvas.ForceUpdateCanvases();
                Transform debugCanvas = panel.transform.Find("Debug Tools (O)");
                var hits = new List<RaycastResult>();
                var pointer = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(game.Board.ActiveCenter) };
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject.transform.IsChildOf(debugCanvas), Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.O)); yield return null; yield return null;
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(loop.State, Is.EqualTo(RunState.Paused));
                panel.Toggle();
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/DebugTools.png"));
                float until = Time.realtimeSinceStartup + 2;
                while (!File.Exists("Logs/DebugTools.png") && Time.realtimeSinceStartup < until) yield return null;
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SpawnAndKillButtonsWorkWhilePausedAndClearDoesNotAwardScore()
        {
            loop.StartRun(); loop.Pause(); panel.Toggle();
            Click("Spawn enemies/Roaster"); Click("Spawn enemies/Toaster"); Click("Spawn enemies/Pan Shield");
            Assert.That(game.Enemies.Count, Is.EqualTo(3));
            Click("Spawn stations/All three");
            Assert.That(game.Stations.Count, Is.EqualTo(3));
            Assert.That(loop.State, Is.EqualTo(RunState.Paused));
            int score = game.Score;
            int enemiesToKill = game.Enemies.Count;
            Click("Enemy actions/Kill all (+score)");
            Assert.That(game.Enemies.Count, Is.Zero);
            Assert.That(game.Score, Is.EqualTo(score + enemiesToKill * 50));
            Click("Enemy actions/Spawn 5");
            Assert.That(game.Enemies.Count, Is.EqualTo(5));
            var cells = new HashSet<Vector2Int>();
            foreach (var enemy in game.Enemies) Assert.That(cells.Add(enemy.CurrentCell), Is.True);
            score = game.Score;
            Click("Enemy actions/Clear world");
            Assert.That(game.Enemies.Count, Is.Zero); Assert.That(game.Stations.Count, Is.Zero);
            Assert.That(game.Score, Is.EqualTo(score));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator ShrinkKeepsAlliesInsideAndOverrideSurvivesTicksThenAutoAndRestartRestoreNormalBoard()
        {
            Assert.That(game.DebugSetBoardSize(10), Is.True);
            Vector2Int[] corners = { new Vector2Int(0, 0), new Vector2Int(9, 9), new Vector2Int(0, 9), new Vector2Int(9, 0) };
            int i = 0;
            foreach (var actor in loop.Survivors) { actor.transform.position = game.Board.CellCenter(corners[i++]); actor.RefreshCell(); }
            loop.StartRun(); loop.Pause();
            game.DebugSpawnStation(ActivityKind.Casino);
            game.Stations[0].transform.position = game.Board.CellCenter(Vector2Int.zero);
            game.DebugSpawnEnemy(); game.Enemies[0].transform.position = game.Board.CellCenter(new Vector2Int(0, 1));
            int score = game.Score;
            Assert.That(game.DebugSetBoardSize(1), Is.False);
            Assert.That(game.Board.ActiveWidth, Is.EqualTo(10));
            Assert.That(game.DebugSetBoardSize(3), Is.True);
            yield return null; yield return null;
            Assert.That(game.Board.ActiveWidth, Is.EqualTo(3));
            Assert.That(game.Enemies.Count, Is.Zero); Assert.That(game.Stations.Count, Is.Zero);
            Assert.That(game.Score, Is.EqualTo(score));
            AssertTeamInside();
            game.DebugUseAutomaticBoard();
            Assert.That(game.Board.ActiveWidth, Is.EqualTo(5));
            Assert.That(game.DebugBoardOverride, Is.False);
            loop.Restart(); AssertTeamInside();
            loop.Restart(); AssertTeamInside();
            Assert.That(loop.LivingAlienCount, Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TimerEditsDoNotRewindPlayAndCanExtendPastFiveMinutesOrFinishWhilePaused()
        {
            loop.StartRun(); loop.Pause();
            float elapsed = loop.ElapsedSeconds;
            Assert.That(loop.DebugAddTime(60), Is.True);
            Assert.That(loop.TimeRemainingSeconds, Is.EqualTo(360).Within(.01f));
            Assert.That(loop.DebugAddTime(-30), Is.True);
            Assert.That(loop.ElapsedSeconds, Is.EqualTo(elapsed));
            Assert.That(loop.TimeRemainingSeconds, Is.EqualTo(330).Within(.01f));
            typeof(RunLoopController).GetField("elapsedSeconds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(loop, 301f);
            loop.Resume(); loop.DebugFreezeTimer = true;
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(loop.State, Is.EqualTo(RunState.Running));
            Assert.That(loop.TimeRemainingSeconds, Is.EqualTo(29).Within(.01f));
            loop.Pause(); Assert.That(loop.DebugSetRemainingTime(0), Is.True);
            Assert.That(loop.State, Is.EqualTo(RunState.RunComplete));
            Assert.That(loop.DebugAddTime(30), Is.False);
            loop.Restart();
            Assert.That(loop.DebugFreezeTimer, Is.False);
            Assert.That(loop.TimeRemainingSeconds, Is.EqualTo(300));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ResourcesHealingAndAutomaticSpawnTogglesWorkWithoutUnpausing()
        {
            loop.StartRun(); loop.Pause();
            game.Seal.AddDamageUpgrade(3); float damage = game.Seal.EffectiveDamage;
            game.Seal.Eliminate(); game.DebugRestoreTeam();
            Assert.That(game.Seal.Alive, Is.True);
            Assert.That(game.Seal.Health, Is.EqualTo(game.Seal.EffectiveMaxHealth));
            Assert.That(game.Seal.EffectiveDamage, Is.EqualTo(damage));
            game.DebugFillUltimates();
            foreach (var actor in loop.Survivors) Assert.That(actor.UltimateFull, Is.True);
            game.DebugGrantCash(); game.DebugGrantItems(); game.DebugGrantChips(); game.DebugGrantScore();
            Assert.That(game.Cash, Is.EqualTo(100)); Assert.That(game.Score, Is.EqualTo(1000));
            foreach (FieldItemKind kind in System.Enum.GetValues(typeof(FieldItemKind))) Assert.That(game.GetItemCount(kind), Is.EqualTo(5));
            Assert.That(game.Chips, Is.EqualTo(5));
            game.DebugStopEnemySpawns = game.DebugStopStationSpawns = true;
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(MainRunGameplaySystems).GetMethod("AdvanceEnemySpawner", flags).Invoke(game, new object[] { 999f });
            typeof(MainRunGameplaySystems).GetMethod("AdvanceProcessSpawn", flags).Invoke(game, new object[] { 999f });
            Assert.That(game.Enemies.Count, Is.Zero); Assert.That(game.Stations.Count, Is.Zero);
            Assert.That(game.DebugSpawnEnemy(), Is.True); Assert.That(game.DebugSpawnStation(ActivityKind.Darts), Is.True);
            Assert.That(loop.State, Is.EqualTo(RunState.Paused));
            loop.Restart();
            Assert.That(game.DebugStopEnemySpawns || game.DebugStopStationSpawns, Is.False);
            Assert.That(game.Cash, Is.Zero); Assert.That(game.Score, Is.Zero);
            Assert.That(game.Enemies.Count, Is.Zero); Assert.That(game.Stations.Count, Is.Zero);
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        void Click(string path)
        {
            var button = panel.transform.Find("Debug Tools (O)/Input blocker/Window/Actions/Content/" + path);
            Assert.That(button, Is.Not.Null, path); button.GetComponent<Button>().onClick.Invoke();
        }

        void AssertTeamInside()
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var actor in loop.Survivors)
            {
                actor.RefreshCell(); Assert.That(actor.IsOnBoard, Is.True);
                Assert.That(cells.Add(actor.CurrentCell), Is.True);
                Assert.That(game.Board.GetCell(actor.CurrentCell).Occupants, Has.Member(actor));
            }
        }
    }
}
