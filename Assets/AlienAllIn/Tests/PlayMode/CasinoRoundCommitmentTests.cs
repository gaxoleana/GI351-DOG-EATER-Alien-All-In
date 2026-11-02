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
    public class CasinoRoundCommitmentTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        MainRunGameplaySystems game;MainCoreGameplayUI ui;
        [UnitySetUp]public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Main");yield return null;
            game=Object.FindAnyObjectByType<MainRunGameplaySystems>();ui=Object.FindAnyObjectByType<MainCoreGameplayUI>();
            game.RunLoop.StartRun();game.RunLoop.Pause();game.DebugStopEnemySpawns=game.DebugStopStationSpawns=true;
            Set("casinoMissChanceByMultiplier",new[]{0f,0f,0f});game.DebugGrantChips(1000);
            Assert.That(game.TryActivateCasino(100,8),Is.True);
            game.Duck.transform.position=game.CasinoMachine.transform.position;game.Duck.RefreshCell();
            game.CasinoMachine.duration=2.2f;Call("TickDuck",.1f);
        }
        [UnityTest]public IEnumerator BetLocksImmediatelyAndCannotBeResetMidRound()
        {
            int chips=game.Chips;
            Assert.That(game.TryActivateCasino(10,2),Is.False);Assert.That(game.TryChangeCasinoBet(500,4),Is.False);
            Assert.That(game.CasinoBet,Is.EqualTo(100));Assert.That(game.CasinoMultiplier,Is.EqualTo(8));Assert.That(game.Chips,Is.EqualTo(chips));
            Call("AdvanceCasino",1f);float progress=game.CasinoMachine.Progress;chips=game.Chips;
            Assert.That(progress,Is.EqualTo(.5f).Within(.001f));Assert.That(game.TryChangeCasinoBet(100,8),Is.False);
            Assert.That(game.TryRecoverCasino(),Is.False,"A healthy machine has nothing to fix.");
            Assert.That(game.CasinoMachine.Progress,Is.EqualTo(progress));Assert.That(game.Chips,Is.EqualTo(chips));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest]public IEnumerator CompletionCapsWorkAtOneCycleAndWaitsForExplicitNextBet()
        {
            int results=0;long net=0;game.CasinoRoundResolved+=(amount,multiplier)=>{results++;net=amount;};
            int before=game.Chips;Call("AdvanceCasino",999f);
            Assert.That(game.Chips,Is.EqualTo(before+1600),"Only the two worked seconds should pay, regardless of the supplied delta.");
            Assert.That(net,Is.EqualTo(1500));Assert.That(results,Is.EqualTo(1));Assert.That(game.CasinoMachine.Progress,Is.EqualTo(1));
            Assert.That(game.CasinoActive,Is.False);Assert.That(game.CasinoWaitingForStake,Is.True);Assert.That(game.CasinoWorker,Is.Null);
            Assert.That(game.CasinoMachine.CurrentWorker,Is.Null);Assert.That(game.Duck.State,Is.EqualTo(ActorState.Idle));
            Assert.That(game.TryRecoverCasino(),Is.False,"Fix cannot restart a completed round.");
            int chips=game.Chips;Vector3 position=game.Duck.transform.position;
            for(int i=0;i<5;i++){Call("TickDuck",1f);Call("AdvanceCasino",999f);}
            Assert.That(game.Chips,Is.EqualTo(chips));Assert.That(results,Is.EqualTo(1));Assert.That(game.CasinoMachine.Progress,Is.EqualTo(1));
            Assert.That(game.Duck.transform.position,Is.EqualTo(position));Assert.That(game.Duck.State,Is.EqualTo(ActorState.Idle));
            // Repeating the same settings is permitted, but requires another explicit stake.
            Assert.That(game.TryChangeCasinoBet(100,8),Is.True);Assert.That(game.Chips,Is.EqualTo(chips-100));
            Assert.That(game.CasinoMachine.Progress,Is.Zero);Call("TickDuck",.1f);Assert.That(game.CasinoWorker,Is.EqualTo(game.Duck));
            Assert.That(game.Duck.State,Is.EqualTo(ActorState.Working));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest]public IEnumerator LockedPanelKeepsCommittedSettingsEvenWhenWalletIsEmpty()
        {
            Set("chips",0);Call("AdvanceCasino",.5f);
            ui.OpenBoardTargetAtScreen(Camera.main.WorldToScreenPoint(game.CasinoMachine.transform.position));
            Assert.That(ui.ShopOpen,Is.True);
            var slider=Get<Slider>("casinoBetSlider");var buttons=Get<Button[]>("casinoMultiplierButtons");var submit=Get<Button>("casinoActiveButton");
            Assert.That(slider.interactable,Is.False);Assert.That(slider.value,Is.EqualTo(100));
            foreach(var button in buttons)if(button && button.gameObject.activeSelf)Assert.That(button.interactable,Is.False);
            Assert.That(submit.interactable,Is.False);Assert.That(Get<Text>("shopBalance").text,Does.Contain("Bet 100 Chips  x8"));
            slider.value=1;buttons[0].onClick.Invoke();submit.onClick.Invoke();ui.RefreshUI();
            Assert.That(game.Chips,Is.Zero);Assert.That(game.CasinoBet,Is.EqualTo(100));Assert.That(game.CasinoMultiplier,Is.EqualTo(8));
            Assert.That(slider.value,Is.EqualTo(100));ui.CloseShop();
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest]public IEnumerator ClickingLosingMachineFixesSameRoundEvenWithEmptyWallet()
        {
            Call("AdvanceCasino",.4f);Set("casinoLosing",true);Set("casinoLossClock",2.9f);Set("chips",0);
            float progress=game.CasinoMachine.Progress;long net=game.CasinoRoundNet;game.RunLoop.Resume();
            var surface=Object.FindAnyObjectByType<BoardDropSurface>();Assert.That(surface,Is.Not.Null);
            surface.OnPointerClick(new PointerEventData(EventSystem.current){position=Camera.main.WorldToScreenPoint(game.CasinoMachine.transform.position)});
            Assert.That(game.CasinoLosing,Is.False);Assert.That(ui.ShopOpen,Is.False);Assert.That(game.RunLoop.State,Is.EqualTo(RunState.Running));
            Assert.That(game.CasinoActive,Is.True);Assert.That(game.CasinoBet,Is.EqualTo(100));Assert.That(game.CasinoMultiplier,Is.EqualTo(8));
            Assert.That(game.CasinoMachine.Progress,Is.EqualTo(progress));Assert.That(game.CasinoRoundNet,Is.EqualTo(net));Assert.That(game.Chips,Is.Zero);
            Assert.That(GetGame<float>("casinoTickClock"),Is.EqualTo(.4f).Within(.001f));Assert.That(GetGame<float>("casinoLossClock"),Is.Zero);
            Assert.That(game.TryChangeCasinoBet(1,2),Is.False);Assert.That(game.TryRecoverCasino(),Is.False);
            Call("AdvanceCasino",.6f);Assert.That(game.Chips,Is.EqualTo(800),"Fix keeps the pending Chip tick and resumes gains.");
            game.RunLoop.Pause();LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest]public IEnumerator PanelFixWorksWhilePausedWithoutUnlockingSettings()
        {
            Call("AdvanceCasino",.4f);Set("casinoLosing",true);Set("chips",0);game.RunLoop.Resume();ui.OpenCasinoPanel();
            float progress=game.CasinoMachine.Progress;long net=game.CasinoRoundNet;
            var slider=Get<Slider>("casinoBetSlider");var buttons=Get<Button[]>("casinoMultiplierButtons");var submit=Get<Button>("casinoActiveButton");
            Assert.That(game.RunLoop.State,Is.EqualTo(RunState.Paused));Assert.That(submit.interactable,Is.True);
            Assert.That(submit.GetComponentInChildren<Text>(true).text,Is.EqualTo("Fix"));Assert.That(slider.interactable,Is.False);
            foreach(var button in buttons)if(button && button.gameObject.activeSelf)Assert.That(button.interactable,Is.False);
            slider.value=1;buttons[0].onClick.Invoke();submit.onClick.Invoke();
            Assert.That(game.CasinoLosing,Is.False);Assert.That(ui.ShopOpen,Is.False);Assert.That(game.RunLoop.State,Is.EqualTo(RunState.Running));
            Assert.That(game.CasinoActive,Is.True);Assert.That(game.CasinoMachine.Progress,Is.EqualTo(progress));
            Assert.That(game.CasinoBet,Is.EqualTo(100));Assert.That(game.CasinoMultiplier,Is.EqualTo(8));
            Assert.That(game.CasinoRoundNet,Is.EqualTo(net));Assert.That(game.Chips,Is.Zero);
            Assert.That(GetGame<float>("casinoTickClock"),Is.EqualTo(.4f).Within(.001f));
            game.RunLoop.Pause();LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest]public IEnumerator FaultRepairButtonPreservesRoundAndChargesOnlyRepairCoins()
        {
            Call("AdvanceCasino",.4f);Set("casinoFaulted",true);game.DebugGrantCash(game.CasinoRepairCoinCost);
            int coins=game.Coins,chips=game.Chips;float progress=game.CasinoMachine.Progress;
            ui.OpenBoardTargetAtScreen(Camera.main.WorldToScreenPoint(game.CasinoMachine.transform.position));
            var submit=Get<Button>("casinoActiveButton");Assert.That(submit.interactable,Is.True);
            Assert.That(submit.GetComponentInChildren<Text>(true).text,Does.StartWith("Repair"));submit.onClick.Invoke();
            Assert.That(game.CasinoFaulted,Is.False);Assert.That(game.Coins,Is.EqualTo(coins-game.CasinoRepairCoinCost));Assert.That(game.Chips,Is.EqualTo(chips));
            Assert.That(game.CasinoMachine.Progress,Is.EqualTo(progress));Assert.That(game.CasinoBet,Is.EqualTo(100));Assert.That(game.CasinoMultiplier,Is.EqualTo(8));
            Assert.That(game.CasinoActive,Is.True);Assert.That(ui.ShopOpen,Is.False);Assert.That(game.RunLoop.State,Is.EqualTo(RunState.Paused));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest]public IEnumerator PlayerReopensCompletedMachineChoosesNextRoundAndConfirmsOnce()
        {
            Call("AdvanceCasino",999f);int chips=game.Chips;game.RunLoop.Resume();ui.OpenBoardTargetAtScreen(Camera.main.WorldToScreenPoint(game.CasinoMachine.transform.position));
            Assert.That(ui.ShopOpen,Is.True);Assert.That(game.CasinoActive,Is.False);Assert.That(game.CasinoMachine.Progress,Is.EqualTo(1));
            var slider=Get<Slider>("casinoBetSlider");var buttons=Get<Button[]>("casinoMultiplierButtons");var submit=Get<Button>("casinoActiveButton");
            Assert.That(slider.interactable,Is.True);slider.value=50;buttons[1].onClick.Invoke();Assert.That(submit.interactable,Is.True);
            Assert.That(game.Chips,Is.EqualTo(chips),"Editing the proposed bet must not spend it.");
            Assert.That(Get<Text>("shopBalance").text,Does.Contain("COMPLETE"));
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Casino-next-round.png"));yield return null;yield return null;
            submit.onClick.Invoke();Assert.That(ui.ShopOpen,Is.False);Assert.That(game.CasinoActive,Is.True);
            Assert.That(game.RunLoop.State,Is.EqualTo(RunState.Running));
            Assert.That(game.CasinoBet,Is.EqualTo(50));Assert.That(game.CasinoMultiplier,Is.EqualTo(4));Assert.That(game.Chips,Is.EqualTo(chips-50));
            Assert.That(game.CasinoMachine.Progress,Is.Zero);Assert.That(game.CasinoWaitingForStake,Is.False);
            submit.onClick.Invoke();Assert.That(game.Chips,Is.EqualTo(chips-50),"A duplicate confirmation must not spend another stake.");
            Call("TickDuck",.1f);Call("AdvanceCasino",.25f);Assert.That(game.CasinoMachine.Progress,Is.GreaterThan(0));
            game.RunLoop.Restart();Assert.That(game.CasinoActive,Is.False);Assert.That(game.CasinoWaitingForStake,Is.False);Assert.That(game.CasinoMachine.Progress,Is.Zero);
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest]public IEnumerator PanicAndInterruptionPreserveLockAndResumeExistingRound()
        {
            Call("AdvanceCasino",.5f);float progress=game.CasinoMachine.Progress;int chips=game.Chips;
            game.DebugSpawnEnemy();var enemy=game.Enemies[0];enemy.transform.position=game.Duck.transform.position+Vector3.right;
            Call("TickDuck",.1f);Call("AdvanceCasino",1f);Assert.That(game.Duck.State,Is.EqualTo(ActorState.Panic));
            Assert.That(game.CasinoMachine.Progress,Is.EqualTo(progress));Assert.That(game.CasinoActive,Is.True);Assert.That(game.TryChangeCasinoBet(1,2),Is.False);
            enemy.Eliminate();Call("TickDuck",.1f);game.CasinoMachine.Interrupt();Call("AdvanceCasino",1f);
            Assert.That(game.Chips,Is.EqualTo(chips));Assert.That(game.CasinoMachine.Progress,Is.EqualTo(progress));
            game.CasinoMachine.ClearInterruption();Call("AdvanceCasino",.5f);Assert.That(game.CasinoMachine.Progress,Is.GreaterThan(progress));
            Assert.That(game.CasinoBet,Is.EqualTo(100));Assert.That(game.CasinoMultiplier,Is.EqualTo(8));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        T Get<T>(string field)=> (T)typeof(MainCoreGameplayUI).GetField(field,Private).GetValue(ui);
        T GetGame<T>(string field)=> (T)typeof(MainRunGameplaySystems).GetField(field,Private).GetValue(game);
        void Set(string field,object value)=>typeof(MainRunGameplaySystems).GetField(field,Private).SetValue(game,value);
        void Call(string name,params object[] args)=>typeof(MainRunGameplaySystems).GetMethod(name,Private).Invoke(game,args);
    }
}
