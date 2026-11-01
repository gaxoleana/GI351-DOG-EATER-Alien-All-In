using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AlienAllIn.Tests
{
    public sealed class MainSceneLayoutPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainHudKeepsOrderedCardsAndControlsOutsideBoard()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            Canvas.ForceUpdateCanvases();
            var ui = Object.FindAnyObjectByType<MainCoreGameplayUI>();
            var layout = Object.FindAnyObjectByType<MainSceneLayout>();
            Transform canvas = ui.GetComponentInChildren<Canvas>().transform;
            Transform cards = canvas.Find("Left Sidebar/Team Cards");
            Assert.That(cards.gameObject.activeSelf, Is.False, "Left side is reserved for future features.");
            Assert.That(canvas.Find("Right Sidebar/Process Status"), Is.Not.Null);
            foreach (string path in new[] { "Run HUD", "Items", "Right Sidebar/Process Status" })
                Assert.That(ScreenRect((RectTransform)canvas.Find(path)).Overlaps(Camera.main.pixelRect), Is.False, path);
            LogAssert.NoUnexpectedReceived();

            // Capture the actual Game view when the editor renders a frame.
            string screenshot = Path.GetFullPath("Logs/MainLayout.png");
            ScreenCapture.CaptureScreenshot(screenshot);
            float until = Time.realtimeSinceStartup + 3;
            while (!File.Exists(screenshot) && Time.realtimeSinceStartup < until) yield return null;
        }

        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
