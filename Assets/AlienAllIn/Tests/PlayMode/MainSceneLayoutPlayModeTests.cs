using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
            Transform cards = canvas.Find("Team Status Bar");
            Assert.That(cards.gameObject.activeSelf, Is.True);
            Assert.That(cards.GetComponentsInChildren<UIResourceDragToken>().Length, Is.EqualTo(4));
            Assert.That(canvas.Find("Left Sidebar/Process Status"), Is.Not.Null);
            Assert.That(canvas.Find("Board Reference Frame"), Is.Not.Null);
            Assert.That(layout.BoardViewport.x, Is.EqualTo(134f/992f).Within(.0001f));
            Assert.That(layout.BoardViewport.y, Is.EqualTo(83f/558f).Within(.0001f));
            Assert.That(layout.BoardViewport.width, Is.EqualTo(724f/992f).Within(.0001f));
            Assert.That(layout.BoardViewport.height, Is.EqualTo(389f/558f).Within(.0001f));
            var currency = canvas.Find("Currency Panel");
            Assert.That(currency.gameObject.activeInHierarchy, Is.True);
            Assert.That(ScreenRect((RectTransform)currency).yMin, Is.GreaterThanOrEqualTo(Camera.main.pixelRect.yMax - .5f));
            Assert.That(ScreenRect((RectTransform)currency).yMax, Is.LessThanOrEqualTo(ScreenRect((RectTransform)canvas.Find("Run HUD")).yMin + .5f));
            Assert.That(ScreenRect((RectTransform)canvas.Find("Shop")).yMin, Is.GreaterThan(ScreenRect((RectTransform)canvas.Find("Items")).yMax));
            foreach (string path in new[] { "Run HUD", "Items", "Left Sidebar/Process Status", "Team Status Bar" })
                Assert.That(ScreenRect((RectTransform)canvas.Find(path)).Overlaps(Camera.main.pixelRect), Is.False, path);
            LogAssert.NoUnexpectedReceived();

            // Capture the actual Game view when the editor renders a frame.
            string screenshot = Path.GetFullPath("Logs/MainLayout.png");
            ScreenCapture.CaptureScreenshot(screenshot);
            float until = Time.realtimeSinceStartup + 3;
            while (!File.Exists(screenshot) && Time.realtimeSinceStartup < until) yield return null;
        }

        [UnityTest]
        public IEnumerator DeadCharacterCardCostsTenCoinsAndRevivesWithoutResettingUpgradesOrDuplicatingRoster()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var game=Object.FindAnyObjectByType<MainRunGameplaySystems>();
            var ui=Object.FindAnyObjectByType<MainCoreGameplayUI>();
            var loop=game.RunLoop; loop.StartRun(); loop.Pause();
            game.DebugGrantCash(100); Assert.That(game.TryBuyAttackUpgrade(game.Seal),Is.True);
            float upgradedDamage=game.Seal.EffectiveDamage;
            int registered=loop.RegisteredAlienCount;
            game.Seal.AddUltimateCharge(1); game.Seal.BeginUltimate(10);
            var oldCell=game.Seal.CurrentCell; game.Seal.Eliminate();
            // A friend occupying the corpse's old position must not be overlapped on revival.
            game.Cat.transform.position=game.Board.CellCenter(oldCell); game.Cat.RefreshCell();
            ui.RefreshUI();
            var card=ui.HudCanvas.transform.Find("Team Status Bar/Seal Card").GetComponent<Button>();
            Assert.That(card.interactable,Is.True);
            Assert.That(card.GetComponentInChildren<Text>().text,Does.Contain("10 Coins"));
            int coins=game.Coins; card.onClick.Invoke();
            Assert.That(game.Coins,Is.EqualTo(coins-10));
            Assert.That(game.Seal.Alive,Is.True); Assert.That(game.Seal.gameObject.activeSelf,Is.True);
            Assert.That(game.Seal.Health,Is.EqualTo(game.Seal.EffectiveMaxHealth));
            Assert.That(game.Seal.State,Is.EqualTo(ActorState.Idle));
            Assert.That(game.Seal.UltimateCharge,Is.Zero); Assert.That(game.Seal.IsUltimateActive,Is.False);
            Assert.That(game.Seal.EffectiveDamage,Is.EqualTo(upgradedDamage));
            Assert.That(game.Seal.CurrentCell,Is.Not.EqualTo(game.Cat.CurrentCell));
            Assert.That(game.Board.Contains(game.Seal.CurrentCell),Is.True);
            Assert.That(loop.RegisteredAlienCount,Is.EqualTo(registered));
            Assert.That(game.TryReviveTeamMember(game.Seal),Is.False);
            Assert.That(game.Coins,Is.EqualTo(coins-10));
            loop.Restart(); Assert.That(game.Coins,Is.Zero); Assert.That(game.Seal.Alive,Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RevivalRejectsInsufficientCoinsAndFinishedRunsAndHudExchangeUpdatesVisibleWallet()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var game=Object.FindAnyObjectByType<MainRunGameplaySystems>();
            var ui=Object.FindAnyObjectByType<MainCoreGameplayUI>(); var loop=game.RunLoop;
            loop.StartRun(); loop.Pause(); game.Seal.Eliminate(); game.DebugGrantCash(9);
            ui.RefreshUI(); var card=ui.HudCanvas.transform.Find("Team Status Bar/Seal Card").GetComponent<Button>();
            Assert.That(card.interactable,Is.False); Assert.That(game.TryReviveTeamMember(game.Seal),Is.False);
            Assert.That(game.Coins,Is.EqualTo(9)); Assert.That(game.Seal.Alive,Is.False);
            loop.Resume();
            ui.HudCanvas.transform.Find("Shop").GetComponent<Button>().onClick.Invoke();
            Assert.That(ui.ShopOpen,Is.True); Assert.That(ui.OptionsOpen,Is.False); Assert.That(loop.State,Is.EqualTo(RunState.Paused));
            var wallet=ui.HudCanvas.transform.Find("Currency Panel/Wallet").GetComponent<Text>();
            ui.HudCanvas.transform.Find("Chip Exchange Shop/Exchange Coin One").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Coins,Is.EqualTo(8)); Assert.That(game.Chips,Is.EqualTo(1));
            Assert.That(wallet.text,Does.Contain("Coins : 8")); Assert.That(wallet.text,Does.Contain("Chips : 1"));
            ui.HudCanvas.transform.Find("Chip Exchange Shop/Exchange One").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Coins,Is.EqualTo(9)); Assert.That(game.Chips,Is.Zero);
            Assert.That(wallet.text,Does.Contain("Coins : 9")); Assert.That(wallet.text,Does.Contain("Chips : 0"));
            ui.CloseShop(); Assert.That(loop.State,Is.EqualTo(RunState.Running));
            loop.Pause(); game.DebugGrantCash(1); loop.DebugSetRemainingTime(0);
            Assert.That(loop.State,Is.EqualTo(RunState.RunComplete)); Assert.That(game.TryReviveTeamMember(game.Seal),Is.False);
            Assert.That(game.Coins,Is.EqualTo(10));
            loop.Restart(); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ExchangeLabelsRenderAndAllFourButtonsReceiveScreenPointerClicks()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var game=Object.FindAnyObjectByType<MainRunGameplaySystems>();
            var ui=Object.FindAnyObjectByType<MainCoreGameplayUI>(); var loop=game.RunLoop;
            loop.StartRun(); loop.Pause(); game.DebugGrantCash(200);
            var canvas=ui.HudCanvas.transform;
            var open=canvas.Find("Shop").GetComponent<Button>(); ClickVisible(open);
            yield return null; Canvas.ForceUpdateCanvases();
            var panel=canvas.Find("Chip Exchange Shop");
            var oneChip=panel.Find("Exchange One").GetComponent<Button>();
            var allChips=panel.Find("Exchange All").GetComponent<Button>();
            var oneCoin=panel.Find("Exchange Coin One").GetComponent<Button>();
            var allCoins=panel.Find("Exchange Coin All").GetComponent<Button>();
            foreach(var button in new[]{oneChip,allChips,oneCoin,allCoins})
            {
                var label=button.GetComponentInChildren<Text>();
                Assert.That(button.gameObject.activeInHierarchy,Is.True);
                Assert.That(label.cachedTextGenerator.vertexCount,Is.GreaterThan(0),button.name+" must render its label");
                Assert.That(button.GetComponent<Image>().color,Is.Not.EqualTo(panel.GetComponent<Image>().color),button.name+" must contrast with the panel");
            }
            Assert.That(oneChip.interactable,Is.False); Assert.That(allChips.interactable,Is.False);
            Assert.That(oneCoin.interactable,Is.True); Assert.That(allCoins.interactable,Is.True);
            ClickVisible(oneCoin); Assert.That(game.Coins,Is.EqualTo(199)); Assert.That(game.Chips,Is.EqualTo(1));
            ClickVisible(oneChip); Assert.That(game.Coins,Is.EqualTo(200)); Assert.That(game.Chips,Is.Zero);
            ClickVisible(allCoins); Assert.That(game.Coins,Is.Zero); Assert.That(game.Chips,Is.EqualTo(200));
            ClickVisible(allChips); Assert.That(game.Coins,Is.EqualTo(200)); Assert.That(game.Chips,Is.Zero);
            Assert.That(canvas.Find("Currency Panel/Wallet").GetComponent<Text>().text,Does.Contain("Coins : 200"));
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/ExchangeFixed.png")); yield return null; yield return null;
            var close=panel.Find("Close").GetComponent<Button>(); ClickVisible(close);
            Assert.That(ui.ShopOpen,Is.False); Assert.That(loop.State,Is.EqualTo(RunState.Paused));
            loop.Resume(); ClickVisible(open); Assert.That(loop.State,Is.EqualTo(RunState.Paused));
            ClickVisible(oneCoin); Assert.That(game.Coins,Is.EqualTo(199)); Assert.That(game.Chips,Is.EqualTo(1));
            ClickVisible(close); Assert.That(loop.State,Is.EqualTo(RunState.Running));
            loop.Restart(); Assert.That(game.Coins,Is.Zero); Assert.That(game.Chips,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        static void ClickVisible(Button button)
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(button.IsActive() && button.IsInteractable(),Is.True,button.name);
            var pointer=new PointerEventData(EventSystem.current) {position=ScreenRect((RectTransform)button.transform).center,button=PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),button.name+" must receive a screen raycast");
            var target=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(target,Is.EqualTo(button.gameObject),button.name+" must not be covered by another graphic");
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerClickHandler);
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
