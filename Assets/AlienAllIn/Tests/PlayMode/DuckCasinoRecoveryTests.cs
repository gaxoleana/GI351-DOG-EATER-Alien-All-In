using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AlienAllIn.Tests
{
    public class DuckCasinoRecoveryTests
    {
        MainRunGameplaySystems game;
        [UnitySetUp] public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Main");yield return null;
            game=Object.FindAnyObjectByType<MainRunGameplaySystems>();game.RunLoop.StartRun();game.RunLoop.Pause();
            game.DebugStopEnemySpawns=game.DebugStopStationSpawns=true;game.DebugSetBoardSize(10);
            game.DebugGrantChips(100);Assert.That(game.TryActivateCasino(10,2),Is.True);
            game.Duck.transform.position=game.CasinoMachine.transform.position;game.Duck.RefreshCell();
            Call("TickDuck",.1f);Assert.That(game.CasinoWorker,Is.EqualTo(game.Duck));
        }
        [UnityTest] public IEnumerator ResumesSameBetAndProgressAfterLastNearbyEnemyDies()
        {
            Call("AdvanceCasino",1f);float progress=game.CasinoMachine.Progress;int chips=game.Chips;
            game.DebugSpawnEnemy();var enemy=game.Enemies[0];enemy.transform.position=game.Duck.transform.position+Vector3.right;
            Call("TickDuck",.1f);Assert.That(game.Duck.State,Is.EqualTo(ActorState.Panic));
            Call("AdvanceCasino",1f);Assert.That(game.CasinoMachine.Progress,Is.EqualTo(progress));Assert.That(game.Chips,Is.EqualTo(chips));
            enemy.Eliminate();Call("TickDuck",.1f);Call("AdvanceCasino",1f);
            Assert.That(game.Duck.State,Is.EqualTo(ActorState.Working));Assert.That(game.CasinoWorker,Is.EqualTo(game.Duck));
            Assert.That(game.CasinoMachine.Progress,Is.GreaterThan(progress));Assert.That(game.CasinoBet,Is.EqualTo(10));Assert.That(game.CasinoMultiplier,Is.EqualTo(2));
            Assert.That(game.Chips,Is.GreaterThan(chips));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest] public IEnumerator WaitsUntilAllNearbyEnemiesLeaveAndIgnoresDistantTargeting()
        {
            game.DebugSpawnEnemy();var first=game.Enemies[0];first.transform.position=game.Duck.transform.position+Vector3.right;
            game.DebugSpawnEnemy();var second=game.Enemies[1];second.transform.position=game.Duck.transform.position+Vector3.left;
            Call("TickDuck",.1f);first.Eliminate();Call("TickDuck",.1f);Assert.That(game.Duck.State,Is.EqualTo(ActorState.Panic));
            second.transform.position=game.Duck.transform.position+Vector3.right*4;
            // Give the distant enemy a cached Duck target, as the real enemy AI does.
            var map=(System.Collections.IDictionary)typeof(MainRunGameplaySystems).GetField("enemyRuntime",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
            object runtime=map[second];runtime.GetType().GetField("target").SetValue(runtime,game.Duck);
            Call("TickDuck",.1f);Assert.That(game.Duck.State,Is.EqualTo(ActorState.Working));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest] public IEnumerator SameCellReacquiresWorkerAndRecentersButCaptureStillBlocksWork()
        {
            typeof(MainRunGameplaySystems).GetField("casinoWorker",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,null);
            game.Duck.State=ActorState.Panic;game.Duck.transform.position+=new Vector3(.25f,0);
            for(int i=0;i<10;i++)Call("TickDuck",.1f);
            Assert.That(game.CasinoWorker,Is.EqualTo(game.Duck));Assert.That(game.Duck.State,Is.EqualTo(ActorState.Working));
            Assert.That(Vector3.Distance(game.Duck.transform.position,game.CasinoMachine.transform.position),Is.LessThan(.15f));
            game.DebugSpawnEnemy();Call("EnemyHitActor",game.Enemies[0],game.Duck,1f);float progress=game.CasinoMachine.Progress;
            Call("TickDuck",.1f);Call("AdvanceCasino",1f);Assert.That(game.DuckCaptured,Is.True);Assert.That(game.CasinoMachine.Progress,Is.EqualTo(progress));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        void Call(string name,params object[] args)=>typeof(MainRunGameplaySystems).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,args);
    }
}
