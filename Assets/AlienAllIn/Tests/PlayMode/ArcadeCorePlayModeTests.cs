using System.Collections;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AlienAllIn.Tests
{
    public sealed class ArcadeCorePlayModeTests
    {
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator StartsEmptyThenSpawnsArtworkAndDuckCompletesWorkWithoutEndingRun()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var systems = Object.FindAnyObjectByType<MainRunGameplaySystems>();
            var loop = systems.RunLoop;
            Assert.That(systems.Stations.Count, Is.Zero);
            Assert.That(systems.Board.ActiveWidth, Is.EqualTo(5));
            loop.StartRun();
            Assert.That(loop.State, Is.EqualTo(RunState.Running));
            // Deterministic test only: force a successful roll, keeping the real 60s interval.
            typeof(MainRunGameplaySystems).GetField("processSpawnChance", Private).SetValue(systems, 1f);
            typeof(MainRunGameplaySystems).GetMethod("AdvanceProcessSpawn", Private).Invoke(systems, new object[] { 59f });
            Assert.That(systems.Stations.Count, Is.Zero);
            typeof(MainRunGameplaySystems).GetMethod("AdvanceProcessSpawn", Private).Invoke(systems, new object[] { 1f });
            Assert.That(systems.Stations.Count, Is.EqualTo(1));
            var station = systems.Stations[0];
            Assert.That(station.artwork.sprite, Is.Not.Null);
            systems.Cat.transform.position = station.transform.position; systems.Cat.RefreshCell();
            station.Advance(1, systems.Cat, false);
            Assert.That(station.Progress, Is.Zero, "Cat is a defender, not a worker for these GDD activities.");
            systems.Duck.transform.position = station.transform.position; systems.Duck.RefreshCell();
            // Move Cat away, allowing the actual station-worker selection to find Duck.
            systems.Cat.transform.position = systems.Board.CellCenter(new Vector2Int(3, 3)); systems.Cat.RefreshCell();
            typeof(MainRunGameplaySystems).GetMethod("AdvanceProcesses", Private).Invoke(systems, new object[] { station.duration + 1 });
            Assert.That(systems.Stations.Count, Is.Zero);
            Assert.That(systems.ProcessedStations, Is.EqualTo(1));
            Assert.That(systems.Cash, Is.GreaterThan(0));
            Assert.That(systems.Score, Is.GreaterThanOrEqualTo(100));
            Assert.That(loop.State, Is.EqualTo(RunState.Running));
            // Exercise each configured prefab through the real spawners in a controlled
            // expanded-board fixture. These test overrides are never saved to Main.
            loop.Restart(); loop.StartRun();
            Object.FindAnyObjectByType<StageGridProgression>().ApplyRunProgress(.5f);
            var activityField = typeof(MainRunGameplaySystems).GetField("activitySettings", Private);
            var enemyField = typeof(MainRunGameplaySystems).GetField("enemySettings", Private);
            var originalActivities = (ActivityPrototypeSettings[])activityField.GetValue(systems);
            var originalEnemies = (EnemyPrototypeSettings[])enemyField.GetValue(systems);
            int index = 0;
            foreach (var setting in originalActivities)
            {
                activityField.SetValue(systems, new[] { setting });
                typeof(MainRunGameplaySystems).GetMethod("TrySpawnActivity", Private).Invoke(systems, null);
                var spawned = systems.Stations[systems.Stations.Count - 1];
                Assert.That(spawned.artwork.sprite, Is.EqualTo(setting.prefab.artwork.sprite));
                spawned.transform.position = systems.Board.CellCenter(new Vector2Int(3 + 2 * index++, 7));
            }
            foreach (var setting in originalEnemies)
            {
                enemyField.SetValue(systems, new[] { setting });
                typeof(MainRunGameplaySystems).GetField("enemySpawnClock", Private).SetValue(systems, 0f);
                typeof(MainRunGameplaySystems).GetMethod("AdvanceEnemySpawner", Private).Invoke(systems, new object[] { .01f });
                var enemy = systems.Enemies[systems.Enemies.Count - 1];
                Assert.That(enemy.GetComponent<SpriteRenderer>().sprite, Is.EqualTo(setting.prefab.GetComponent<SpriteRenderer>().sprite));
                enemy.Hurt(1);
            }
            typeof(MainRunGameplaySystems).GetMethod("TickEnemyActors", Private).Invoke(systems, new object[] { .01f });
            Assert.That(systems.Enemies[0].GetComponent<SpriteRenderer>().color, Is.Not.EqualTo(Color.white));
            typeof(MainRunGameplaySystems).GetMethod("TickEnemyActors", Private).Invoke(systems, new object[] { .3f });
            Assert.That(systems.Enemies[0].GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
            activityField.SetValue(systems, originalActivities); enemyField.SetValue(systems, originalEnemies);
            loop.Pause();
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Arcade-core-preview.png"));
            float deadline = Time.realtimeSinceStartup + 2;
            while (!File.Exists("Logs/Arcade-core-preview.png") && Time.realtimeSinceStartup < deadline) yield return null;
            loop.Restart();
            Assert.That(systems.Stations.Count, Is.Zero);
            Assert.That(systems.Board.ActiveWidth, Is.EqualTo(5));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EnemyAlienAndCasinoVisualFeedbackDisplayExpectedPopupAndBounce()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var systems = Object.FindAnyObjectByType<MainRunGameplaySystems>();
            var ui = Object.FindAnyObjectByType<MainCoreGameplayUI>();
            var loop = systems.RunLoop;
            loop.StartRun(); loop.Pause();
            systems.DebugStopEnemySpawns = systems.DebugStopStationSpawns = true;

            // 1. Enemy hit feedback: red flash, bouncy scale, and bold red "-x" at top-right of Enemy.
            systems.DebugSpawnEnemy(EnemyArchetype.RoasterMelee);
            var enemy = systems.Enemies[0];
            enemy.transform.position = systems.Board.CellCenter(new Vector2Int(3, 3));
            enemy.RefreshCell();
            float dealtToEnemy = enemy.Hurt(1f);
            Assert.That(dealtToEnemy, Is.EqualTo(1f).Within(.001f));
            typeof(MainRunGameplaySystems).GetMethod("TickEnemyActors", Private).Invoke(systems, new object[] { .04f });
            Assert.That(enemy.GetComponent<SpriteRenderer>().color, Is.Not.EqualTo(Color.white));
            Assert.That(enemy.transform.localScale, Is.Not.EqualTo(Vector3.one));

            var damagePopup = ui.HudCanvas.transform.Find("Damage floating text")?.GetComponent<Text>();
            Assert.That(damagePopup, Is.Not.Null);
            Assert.That(damagePopup.text, Is.EqualTo("-1"));
            Assert.That(damagePopup.fontStyle, Is.EqualTo(FontStyle.Bold));
            Assert.That(damagePopup.color.r, Is.GreaterThan(.8f));
            Assert.That(damagePopup.color.g, Is.LessThan(.3f));
            Vector2 enemyScreen = Camera.main.WorldToScreenPoint(enemy.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ui.HudCanvas.transform, enemyScreen, null, out Vector2 enemyHudPos);
            Assert.That(damagePopup.rectTransform.anchoredPosition.x, Is.GreaterThan(enemyHudPos.x));
            Assert.That(damagePopup.rectTransform.anchoredPosition.y, Is.GreaterThan(enemyHudPos.y));

            // 2. Alien hit feedback: same red flash, bouncy scale, and bold red "-x" at top-right of Alien.
            loop.Restart(); loop.StartRun(); loop.Pause();
            // Restart destroys the previous popup at the end of the frame.
            yield return null;
            systems.DebugStopEnemySpawns = systems.DebugStopStationSpawns = true;
            var seal = systems.Seal;
            seal.transform.position = systems.Board.CellCenter(new Vector2Int(2, 2));
            seal.RefreshCell();
            float dealtToAlien = seal.Hurt(3f);
            Assert.That(dealtToAlien, Is.EqualTo(3f).Within(.001f));
            seal.AdvanceRuntime(.04f);
            Assert.That(seal.GetComponent<SpriteRenderer>().color, Is.Not.EqualTo(Color.white));
            Assert.That(seal.transform.localScale, Is.Not.EqualTo(Vector3.one));

            var alienPopup = ui.HudCanvas.transform.Find("Damage floating text")?.GetComponent<Text>();
            Assert.That(alienPopup, Is.Not.Null);
            Assert.That(alienPopup.text, Is.EqualTo("-3"));
            Assert.That(alienPopup.fontStyle, Is.EqualTo(FontStyle.Bold));
            Vector2 sealScreen = Camera.main.WorldToScreenPoint(seal.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ui.HudCanvas.transform, sealScreen, null, out Vector2 sealHudPos);
            Assert.That(alienPopup.rectTransform.anchoredPosition.x, Is.GreaterThan(sealHudPos.x));
            Assert.That(alienPopup.rectTransform.anchoredPosition.y, Is.GreaterThan(sealHudPos.y));

            // 3. Casino visual feedback: smaller font than damage text and "+" at top-right of Casino machine.
            typeof(MainCoreGameplayUI).GetMethod("ShowCasinoChipTick", Private).Invoke(ui, new object[] { 4L });
            var casinoTick = ui.HudCanvas.transform.Find("Casino Chip tick")?.GetComponent<Text>();
            Assert.That(casinoTick, Is.Not.Null);
            Assert.That(casinoTick.text, Does.StartWith("+4"));
            Assert.That(casinoTick.fontSize, Is.LessThan(alienPopup.fontSize));
            Vector2 casinoScreen = Camera.main.WorldToScreenPoint(systems.CasinoMachine.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ui.HudCanvas.transform, casinoScreen, null, out Vector2 casinoHudPos);
            Assert.That(casinoTick.rectTransform.anchoredPosition.x, Is.GreaterThan(casinoHudPos.x));
            Assert.That(casinoTick.rectTransform.anchoredPosition.y, Is.GreaterThan(casinoHudPos.y));

            loop.Restart();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SessionResultAndPlayAgainResetTheArcadeRun()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var systems = Object.FindAnyObjectByType<MainRunGameplaySystems>();
            var loop = systems.RunLoop;
            loop.StartRun();
            foreach (var actor in loop.Survivors) actor.Eliminate();
            yield return null;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(loop.State, Is.EqualTo(RunState.Defeated));
            var ui = Object.FindAnyObjectByType<MainCoreGameplayUI>();
            var panel = ui.GetComponentInChildren<Canvas>().transform.Find("Session Result");
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.Find("Final Score").GetComponent<Text>().text, Does.Contain("SCORE"));
            panel.Find("PLAY AGAIN").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(loop.State, Is.EqualTo(RunState.Placement));
            Assert.That(loop.LivingAlienCount, Is.EqualTo(3));
            Assert.That(systems.Cash, Is.Zero); Assert.That(systems.Score, Is.Zero);
            Assert.That(systems.Stations.Count, Is.Zero); Assert.That(systems.Enemies.Count, Is.Zero);
            Assert.That(systems.Board.GetComponentsInChildren<BoardCell>().Length, Is.EqualTo(25));
            Assert.That(panel.gameObject.activeSelf, Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
