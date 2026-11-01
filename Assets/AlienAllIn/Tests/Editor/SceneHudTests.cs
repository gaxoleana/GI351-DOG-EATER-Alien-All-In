using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AlienAllIn.Tests
{
    public sealed class SceneHudTests
    {
        [Test]
        public void HudPopupDebugAndVfxAreSavedEditableSceneObjects()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            var ui = Object.FindAnyObjectByType<MainCoreGameplayUI>();
            var canvas = ui.HudCanvas.transform;
            Assert.That(canvas.Find("Run HUD/Wallet").GetComponent<Text>().text, Does.Contain("Chips"));
            Assert.That(canvas.Find("Character Popup/Upgrade Box/Close").GetComponent<Button>(), Is.Not.Null);
            Assert.That(canvas.Find("Character Popup/Upgrade Box/Upgrade").GetComponent<Button>(), Is.Not.Null);
            Assert.That(canvas.Find("Right Sidebar/Cash Upgrades"), Is.Null);
            Assert.That(canvas.Find("Right Sidebar/Chip Shop"), Is.Null);
            Assert.That(ui.transform.Find("Debug Tools (O)/Input blocker/Window/Actions/Content/Spawn enemies"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<EventSystem>(), Is.Not.Null);
            var data = new SerializedObject(ui);
            foreach (string field in new[] { "runLoop", "systems", "hudCanvas", "popupOverlay", "popupOutside", "popupClose", "popupUpgrade", "debugPanel" })
                Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var vfx = Object.FindAnyObjectByType<AttackVfx>();
            Assert.That(Object.FindAnyObjectByType<BoardInteractionFeedback>(), Is.Not.Null);
            Assert.That(vfx.CatRenderer.positionCount, Is.EqualTo(2));
            foreach (var actor in Object.FindObjectsByType<GridActor>())
                if (actor.kind == ActorKind.Duck) { Assert.That(actor.invulnerable, Is.True); Assert.That(actor.CountsAsActiveRunMember, Is.False); }
        }
    }
}
