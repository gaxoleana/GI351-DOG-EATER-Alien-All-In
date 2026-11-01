using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AlienAllIn.Tests
{
    public class ReferenceFeedbackPlayTests
    {
        [UnityTest] public IEnumerator HoverAndDragShowAndClearCorrectCellIncludingBlockedDestinations()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var ui=Object.FindAnyObjectByType<MainCoreGameplayUI>(); var game=Object.FindAnyObjectByType<MainRunGameplaySystems>();
            var feedback=ui.Feedback; var old=InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
            var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var mouse=InputSystem.AddDevice<Mouse>();
            try {
                InputSystem.QueueStateEvent(mouse,new MouseState {position=Camera.main.WorldToScreenPoint(game.Seal.transform.position)});
                yield return null; yield return null;
                Assert.That(feedback.hoverOutline.enabled,Is.True);
                Vector2Int free=default; bool found=false;
                foreach(var cell in game.Board.GetComponentsInChildren<BoardCell>()) if(game.CanOrderMove(game.Seal,cell.coordinate)&&cell.coordinate!=game.Seal.CurrentCell) {free=cell.coordinate;found=true;break;}
                Assert.That(found,Is.True);
                feedback.BeginDrag(game.Seal,Camera.main.WorldToScreenPoint(game.Board.CellCenter(free)));
                yield return null; yield return null;
                Assert.That(feedback.DragCell,Is.EqualTo(free)); Assert.That(feedback.dragOutline.enabled,Is.True);
                Assert.That(Vector4.Distance(feedback.dragOutline.startColor,feedback.validColor),Is.LessThan(.01f));
                feedback.DragTo(Camera.main.WorldToScreenPoint(game.Cat.transform.position)); yield return null; yield return null;
                Assert.That(feedback.DragCell,Is.EqualTo(game.Cat.CurrentCell)); Assert.That(Vector4.Distance(feedback.dragOutline.startColor,feedback.invalidColor),Is.LessThan(.01f));
                feedback.EndDrag(); yield return null;
                Assert.That(feedback.dragOutline.enabled,Is.False); Assert.That(feedback.DragCell,Is.Null);
                ui.OpenCharacter(game.Seal); yield return null; Assert.That(feedback.hoverOutline.enabled,Is.False); ui.CloseCharacter();
                game.RunLoop.StartRun(); ui.OpenOptions(); Assert.That(game.RunLoop.State,Is.EqualTo(RunState.Paused));
                ui.CloseOptions(); Assert.That(game.RunLoop.State,Is.EqualTo(RunState.Running));
                game.RunLoop.Pause(); ui.OpenOptions(); ui.CloseOptions(); Assert.That(game.RunLoop.State,Is.EqualTo(RunState.Paused));
            } finally {
                InputSystem.RemoveDevice(mouse); InputSystem.settings.backgroundBehavior=old;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode=editor;
#endif
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SealDamagesOnlyReferenceCellsAndReferenceLayoutRenders()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var game=Object.FindAnyObjectByType<MainRunGameplaySystems>(); var ui=Object.FindAnyObjectByType<MainCoreGameplayUI>();
            game.DebugSetBoardSize(10); game.Seal.transform.position=game.Board.CellCenter(new Vector2Int(4,4)); game.Seal.RefreshCell();
            game.DebugSpawnEnemy(); var inside=game.Enemies[0]; inside.transform.position=game.Board.CellCenter(new Vector2Int(5,5)); inside.RefreshCell();
            game.DebugSpawnEnemy(); var outside=game.Enemies[1]; outside.transform.position=game.Board.CellCenter(new Vector2Int(6,5)); outside.RefreshCell();
            var tick=typeof(MainRunGameplaySystems).GetMethod("TickSeal",BindingFlags.Instance|BindingFlags.NonPublic);
            float hp=outside.Health; tick.Invoke(game,new object[]{.01f}); tick.Invoke(game,new object[]{.2f});
            Assert.That(inside.Health,Is.LessThan(10)); Assert.That(outside.Health,Is.EqualTo(hp));
            ui.Feedback.ShowRange(game.Seal.transform.position,false,1); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(ui.Feedback.RangeCellCount,Is.EqualTo(13));
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Reference-layout.png")); yield return null; yield return null;
            ui.Feedback.ShowRange(game.Seal.transform.position,true,1); yield return null; yield return null;
            Assert.That(ui.Feedback.RangeCellCount,Is.EqualTo(21));
            // Enhanced hits follow the cell mask even at opposite sub-cell corners.
            game.Seal.transform.position=game.Board.CellCenter(new Vector2Int(4,4))-new Vector3(.44f,.44f);
            inside.transform.position=game.Board.CellCenter(new Vector2Int(6,5))+new Vector3(.44f,.44f); inside.RefreshCell();
            outside.transform.position=game.Board.CellCenter(new Vector2Int(6,6)); outside.RefreshCell();
            float before=inside.Health;
            game.RunLoop.StartRun(); game.Seal.AddUltimateCharge(1); Assert.That(game.ActivateUltimate(game.Seal),Is.True); game.RunLoop.Pause();
            typeof(MainRunGameplaySystems).GetMethod("TickSealRipples",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{.6f});
            Assert.That(inside.Health,Is.LessThan(before)); Assert.That(outside.Health,Is.EqualTo(hp));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
