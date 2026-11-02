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
    public class CharacterGaugeAndCatTests
    {
        MainRunGameplaySystems game; MainCoreGameplayUI ui; RunLoopController loop;
        [UnitySetUp] public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Main");yield return null;
            game=Object.FindAnyObjectByType<MainRunGameplaySystems>();ui=Object.FindAnyObjectByType<MainCoreGameplayUI>();loop=game.RunLoop;
            loop.StartRun();loop.Pause();game.DebugStopEnemySpawns=game.DebugStopStationSpawns=true;
        }
        [UnityTest] public IEnumerator SavedBarsFollowActorsShowHealthChargeAndDuckRescueWithoutBlockingInput()
        {
            var bars=ui.HudCanvas.GetComponentsInChildren<CharacterStatusBar>(true);Assert.That(bars.Length,Is.EqualTo(4));
            CharacterStatusBar cat=null,duck=null;
            foreach(var bar in bars){if(bar.actor==game.Cat)cat=bar;if(bar.actor==game.Duck)duck=bar;}
            Assert.That(cat.GetComponentsInChildren<Text>(true),Is.Empty);
            Assert.That(cat,Is.Not.Null);game.Cat.Hurt(6);game.Cat.AddUltimateCharge(.5f);yield return null;yield return null;
            Assert.That(cat.healthFill.anchorMax.x,Is.EqualTo(.8f).Within(.01f));Assert.That(cat.chargeFill.anchorMax.x,Is.EqualTo(.5f).Within(.001f));
            Assert.That(duck.healthRoot.gameObject.activeSelf,Is.False);
            Vector3 before=cat.transform.position;game.Cat.transform.position+=Vector3.right*.3f;yield return null;yield return null;
            Assert.That(cat.transform.position.x,Is.GreaterThan(before.x));
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,cat.transform.position)},hits);
            foreach(var hit in hits)Assert.That(hit.gameObject.transform.IsChildOf(cat.transform),Is.False);
            game.Cat.AddUltimateCharge(1);yield return null;Assert.That(cat.chargeFill.anchorMax.x,Is.GreaterThanOrEqualTo(.999f));Assert.That(cat.chargeImage.color,Is.EqualTo(cat.chargeColor));
            game.Cat.SpendCharge();game.Cat.AddUltimateCharge(.9995f);yield return null;
            Assert.That(game.Cat.UltimateFull,Is.True);Assert.That(cat.chargeFill.anchorMax.x,Is.GreaterThanOrEqualTo(.999f));
            game.Cat.Eliminate();yield return null;Assert.That(cat.visibility.alpha,Is.Zero);game.Cat.Revive();yield return null;Assert.That(cat.visibility.alpha,Is.EqualTo(1));
            game.Seal.AddUltimateCharge(1);game.Pufferfish.AddUltimateCharge(.5f);game.Cat.AddUltimateCharge(.75f);
            typeof(MainCoreGameplayUI).GetMethod("ResetCasinoVisuals",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
            yield return null;yield return null;
            ui.RefreshUI();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Character-Gauges.png"),2);yield return null;yield return null;
            game.DebugSpawnEnemy();Call("EnemyHitActor",game.Enemies[0],game.Duck,1f);yield return null;yield return null;
            Assert.That(duck.healthRoot.gameObject.activeSelf,Is.True);Assert.That(duck.healthFill.anchorMax.x,Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ItemReadinessRequiresChargeAndConsumesInventoryAndGauge()
        {
            game.DebugGrantItems();
            // Locate through serialized references, preserving the current HUD hierarchy.
            var buttons=ui.HudCanvas.GetComponentsInChildren<UIResourceDragToken>(true);Button laser=null;
            foreach(var token in buttons)if(token.Payload==UIDragPayload.Item&&token.Item==FieldItemKind.LaserGun)laser=token.GetComponent<Button>();
            Assert.That(laser,Is.Not.Null);loop.Resume();loop.DebugFreezeTimer=true;ui.RefreshUI();Assert.That(laser.interactable,Is.False);
            game.Cat.AddUltimateCharge(1);ui.RefreshUI();Assert.That(laser.interactable,Is.True);
            laser.onClick.Invoke();Assert.That(game.GetItemCount(FieldItemKind.LaserGun),Is.EqualTo(4));Assert.That(game.Cat.UltimateCharge,Is.Zero);Assert.That(game.Cat.IsUltimateActive,Is.True);
            loop.Pause();ui.RefreshUI();Assert.That(laser.interactable,Is.False);
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest] public IEnumerator CatNormalUsesCellMaskAndUltimateHitsWholeActiveBoardWithEightFixedBeams()
        {
            game.DebugSetBoardSize(10);Put(game.Cat,4,4);
            game.DebugSpawnEnemy(EnemyArchetype.RoasterMelee);var inside=game.Enemies[0];Put(inside,6,5);
            game.DebugSpawnEnemy(EnemyArchetype.RoasterMelee);var corner=game.Enemies[1];Put(corner,6,6);corner.transform.position-=new Vector3(.4f,.4f);corner.RefreshCell();
            float cornerHealth=corner.Health;Call("TickCat",.01f);Assert.That(inside.Health,Is.LessThan(10));Assert.That(corner.Health,Is.EqualTo(cornerHealth));
            ui.OpenCharacter(game.Cat);yield return null;yield return null;Assert.That(ui.Feedback.RangeCellCount,Is.EqualTo(21));ui.CloseCharacter();
            game.DebugSpawnEnemy(EnemyArchetype.RoasterMelee);var far=game.Enemies[2];Put(far,9,2);far.transform.position+=new Vector3(.25f,.1f);far.RefreshCell();float farHealth=far.Health;
            game.DebugSpawnEnemy(EnemyArchetype.RoasterMelee);var outside=game.Enemies[3];outside.transform.position=game.Board.transform.TransformPoint(new Vector3(-.5f,-.5f));float outsideHealth=outside.Health;
            game.Cat.AddUltimateCharge(1);loop.Resume();Assert.That(game.ActivateUltimate(game.Cat),Is.True);loop.Pause();
            Call("TickCatLasers",.01f);Assert.That(far.Health,Is.LessThan(farHealth));Assert.That(outside.Health,Is.EqualTo(outsideHealth));
            var vfx=Object.FindAnyObjectByType<AttackVfx>();yield return null;yield return null;
            Assert.That(vfx.CatUltimateRenderers.Length,Is.EqualTo(8));
            foreach(var beam in vfx.CatUltimateRenderers){Assert.That(beam.enabled,Is.True);Assert.That(beam.GetPosition(0),Is.EqualTo(game.Cat.transform.position));}
            Vector3 end=vfx.CatUltimateRenderers[1].GetPosition(1);yield return new WaitForSecondsRealtime(.2f);Assert.That(vfx.CatUltimateRenderers[1].GetPosition(1),Is.EqualTo(end),"Lasers must remain fixed while paused.");
            loop.Resume();yield return new WaitForSeconds(.22f);loop.Pause();
            Assert.That(game.Cat.IsUltimateActive,Is.True);
            foreach(var beam in vfx.CatUltimateRenderers)Assert.That(beam.enabled,Is.False,"Each shot must disappear between Ultimate attack ticks.");
            game.DebugSetBoardSize(7);yield return null;yield return null;
            Call("TickCatLasers",.5f);yield return null;yield return null;
            foreach(var beam in vfx.CatUltimateRenderers)Assert.That(beam.enabled,Is.True,"The next attack tick should fire another short eight-way shot.");
            var min=game.Board.ActiveMinCell;
            foreach(var beam in vfx.CatUltimateRenderers){var local=game.Board.transform.InverseTransformPoint(beam.GetPosition(1));Assert.That(local.x,Is.InRange(min.x-.001f,min.x+7.001f));Assert.That(local.y,Is.InRange(min.y-.001f,min.y+7.001f));}
            ui.OpenCharacter(game.Cat);yield return null;yield return null;Assert.That(ui.Feedback.RangeCellCount,Is.EqualTo(49));ui.CloseCharacter();
            Put(game.Seal,2,2);Put(game.Pufferfish,3,5);
            typeof(MainCoreGameplayUI).GetMethod("ResetCasinoVisuals",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
            yield return new WaitForSecondsRealtime(.25f);ui.RefreshUI();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Cat-Ultimate-Gauges.png"),2);yield return null;yield return null;
            game.Cat.AdvanceRuntime(10);yield return null;foreach(var beam in vfx.CatUltimateRenderers)Assert.That(beam.enabled,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        void Put(GridActor actor,int x,int y){actor.transform.position=game.Board.CellCenter(new Vector2Int(x,y));actor.RefreshCell();}
        void Call(string name,params object[] args)=>typeof(MainRunGameplaySystems).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
    }
}
