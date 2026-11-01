using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AlienAllIn.Tests
{
    public sealed class GameplayPlayModeTests
    {
        [UnityTest]
        public IEnumerator SavedSceneStartsThroughHUDAndCompletesARealRun()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay");
            yield return null;
            var game = Object.FindAnyObjectByType<GameplayController>();
            Assert.That(game.Phase, Is.EqualTo(StagePhase.Placement));
            Assert.That(game.hud.startButton.gameObject.activeSelf, Is.True);
            game.hud.startButton.onClick.Invoke();
            yield return null;
            Assert.That(game.Phase, Is.EqualTo(StagePhase.Running));
            Assert.That(game.hud.startButton.gameObject.activeSelf, Is.False);
            // Fast-forward the actual runtime simulation, not a separate model/mock.
            for (int i = 0; i < 2400 && game.Phase == StagePhase.Running; i++) game.Advance(.05f);
            Assert.That(game.Phase, Is.EqualTo(StagePhase.Won));
            Assert.That(game.DefeatedEnemies, Is.GreaterThan(0));
            Assert.That(game.hud.phaseText.text, Is.EqualTo("SHIFT COMPLETE"));
            game.hud.restartButton.onClick.Invoke();
            yield return null;
            Assert.That(game.Phase, Is.EqualTo(StagePhase.Placement));
            Assert.That(game.hud.startButton.gameObject.activeSelf, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator HudPauseAndDuckDestinationWorkDuringLiveFrames()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay");
            yield return null;
            var game = Object.FindAnyObjectByType<GameplayController>();
            game.hud.startButton.onClick.Invoke();
            game.hud.duckButton.onClick.Invoke();
            var destination = new Vector2Int(1, 2);
            Assert.That(game.CommandCell(destination), Is.True);
            for (int i = 0; i < 20; i++) game.Advance(.025f);
            Assert.That(game.duck.CurrentCell, Is.EqualTo(destination));
            game.hud.pauseButton.onClick.Invoke();
            float elapsed = game.Elapsed;
            yield return null;
            Assert.That(game.Elapsed, Is.EqualTo(elapsed));
            Assert.That(game.Paused, Is.True);
            game.hud.pauseButton.onClick.Invoke();
            yield return null;
            Assert.That(game.Elapsed, Is.GreaterThan(elapsed));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
