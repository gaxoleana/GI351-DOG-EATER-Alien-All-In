using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AlienAllIn.Tests
{
    public class DuckStationPriorityTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        MainRunGameplaySystems game;
        MainCoreGameplayUI ui;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            game = Object.FindAnyObjectByType<MainRunGameplaySystems>();
            ui = Object.FindAnyObjectByType<MainCoreGameplayUI>();
            game.RunLoop.StartRun(); game.RunLoop.Pause();
            game.DebugStopEnemySpawns = game.DebugStopStationSpawns = true;
            game.DebugSetBoardSize(10);
            Set("casinoMissChanceByMultiplier", new[] { 0f, 0f, 0f });
            Set("duckProcessChargePerSecond", 0f);
            game.DebugGrantChips(1000);
            Assert.That(game.TryActivateCasino(100, 8), Is.True);
            Put(game.Duck, game.CasinoMachine);
            game.CasinoMachine.duration = 22f;
            Call("TickDuck", .1f); Call("AdvanceCasino", .4f);
        }

        [UnityTest]
        public IEnumerator AllStationsIncludingNewArrivalsMustFinishBeforeSameRoundResumes()
        {
            float progress = game.CasinoMachine.Progress;
            int chips = game.Chips; long net = game.CasinoRoundNet;
            float chipClock = GetGame<float>("casinoTickClock"), lossClock = GetGame<float>("casinoLossClock");
            Spawn(ActivityKind.Darts); Spawn(ActivityKind.JumpRope);
            Assert.That(game.PendingStationCount, Is.EqualTo(2));
            Assert.That(game.CasinoWorker, Is.Null); Assert.That(game.CasinoMachine.CurrentWorker, Is.Null);
            Assert.That(game.CanOrderMove(game.Duck, game.CasinoMachine.Cell), Is.False);
            Call("AdvanceCasino", 999f);
            Assert.That(game.CasinoMachine.Progress, Is.EqualTo(progress)); Assert.That(game.Chips, Is.EqualTo(chips));
            Assert.That(GetGame<float>("casinoTickClock"), Is.EqualTo(chipClock));
            Assert.That(GetGame<float>("casinoLossClock"), Is.EqualTo(lossClock));

            RunUntilStationCount(1);
            Assert.That(game.CasinoNeedsStationWork, Is.True);
            Assert.That(game.CasinoMachine.Progress, Is.EqualTo(progress));
            Spawn(ActivityKind.Darts); // Arrives while Duck is clearing the queue.
            RunUntilStationCount(0);
            Assert.That(game.Coins, Is.GreaterThan(0)); Assert.That(game.CasinoNeedsStationWork, Is.False);
            Assert.That(game.CasinoMachine.Progress, Is.EqualTo(progress));
            for (int i = 0; i < 1000 && game.CasinoMachine.Progress == progress; i++)
            { Call("TickDuck", .05f); Call("AdvanceCasino", .05f); }
            Assert.That(game.CasinoWorker, Is.EqualTo(game.Duck));
            Assert.That(game.CasinoMachine.Progress, Is.GreaterThan(progress));
            Assert.That(game.CasinoBet, Is.EqualTo(100)); Assert.That(game.CasinoMultiplier, Is.EqualTo(8));
            Assert.That(game.Chips, Is.EqualTo(chips), "Returning neither spends a new stake nor pays during travel.");
            Assert.That(game.CasinoRoundNet, Is.EqualTo(net));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator OnlyDuckCanEnterAndOperateStationsEvenWithLegacyOwnerSettings()
        {
            ActivityStation station = Spawn(ActivityKind.JumpRope);
            Assert.That(station.requiredKind, Is.EqualTo(ActorKind.Duck));
            foreach (GridActor fighter in new[] { game.Seal, game.Cat, game.Pufferfish })
            {
                Assert.That(game.CanOrderMove(fighter, station.Cell), Is.False);
                Put(fighter, station);
                Assert.That(station.Advance(5f, fighter, false), Is.Zero);
                Assert.That(station.AdvanceContinuous(5f, fighter, 1f), Is.False);
                Call("AdvanceProcesses", .1f);
                Assert.That(station.Progress, Is.Zero); Assert.That(game.Coins, Is.Zero);
                fighter.transform.position = game.Board.CellCenter(new Vector2Int((int)fighter.kind, 0));
                fighter.RefreshCell();
            }
            station.requiredKind = ActorKind.Seal; station.allowUniversalDuckWorker = false;
            Put(game.Duck, station); Call("AdvanceProcesses", 1f);
            Assert.That(station.Progress, Is.GreaterThan(0)); Assert.That(game.Coins, Is.EqualTo(1));
            Assert.That(station.CurrentWorker, Is.EqualTo(game.Duck));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator PendingStationsBlockNewBetsUntilEveryStationCompletes()
        {
            Call("AdvanceCasino", 999f);
            ActivityStation first = Spawn(ActivityKind.Darts), second = Spawn(ActivityKind.JumpRope);
            int chips = game.Chips;
            ui.OpenCasinoPanel();
            var submit = GetUI<Button>("casinoActiveButton");
            Assert.That(submit.interactable, Is.False);
            Assert.That(GetUI<Slider>("casinoBetSlider").interactable, Is.False);
            Assert.That(GetUI<Text>("shopBalance").text, Does.Contain("STATIONS FIRST (2)"));
            Assert.That(game.TryActivateCasino(50, 4), Is.False); submit.onClick.Invoke();
            Assert.That(game.Chips, Is.EqualTo(chips)); Assert.That(game.CasinoMachine.Progress, Is.EqualTo(1));
            ui.CloseShop();
            Put(game.Duck, first); Call("AdvanceProcesses", .5f);
            Assert.That(game.PendingStationCount, Is.EqualTo(2), "Partial progress does not remove a task.");
            Call("AdvanceProcesses", 5f);
            Assert.That(game.PendingStationCount, Is.EqualTo(1)); Assert.That(game.TryActivateCasino(50, 4), Is.False);
            Put(game.Duck, second); Call("AdvanceProcesses", 5f);
            Assert.That(game.PendingStationCount, Is.Zero);
            ui.OpenCasinoPanel(); Assert.That(submit.interactable, Is.True);
            GetUI<Slider>("casinoBetSlider").value = 50;
            GetUI<Button[]>("casinoMultiplierButtons")[1].onClick.Invoke(); submit.onClick.Invoke();
            Assert.That(game.CasinoActive, Is.True); Assert.That(game.CasinoBet, Is.EqualTo(50));
            Assert.That(game.CasinoMultiplier, Is.EqualTo(4)); Assert.That(game.Chips, Is.EqualTo(chips - 50));
            Assert.That(game.CasinoMachine.Progress, Is.Zero);
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator SpawnCancelsAnInFlightCasinoOrderAndRestartClearsTheQueue()
        {
            Assert.That(game.TryOrderMove(game.Duck, game.CasinoMachine.Cell), Is.True);
            Assert.That(GetGame<System.Collections.IDictionary>("manualDestinations").Contains(game.Duck), Is.True);
            ActivityStation station = Spawn(ActivityKind.Darts);
            Assert.That(GetGame<System.Collections.IDictionary>("manualDestinations").Contains(game.Duck), Is.False);
            Vector3 before = game.Duck.transform.position;
            Call("TickDuck", .1f);
            Assert.That(Vector3.Distance(game.Duck.transform.position, station.transform.position),
                Is.LessThan(Vector3.Distance(before, station.transform.position)));
            Assert.That(game.CasinoWorker, Is.Null);
            game.RunLoop.Restart();
            Assert.That(game.PendingStationCount, Is.Zero); Assert.That(game.CasinoNeedsStationWork, Is.False);
            Assert.That(game.CasinoActive, Is.False); Assert.That(GetGame<bool>("casinoReturnAfterStations"), Is.False);
            game.DebugGrantChips(1); Assert.That(game.TryActivateCasino(1, 2), Is.True);
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        [UnityTest]
        public IEnumerator CaptureStopsBothStationWorkAndCasinoUntilDuckIsFreed()
        {
            ActivityStation station = Spawn(ActivityKind.Darts);
            Put(game.Duck, station); Call("AdvanceProcesses", .2f);
            Assert.That(game.DebugSpawnEnemy(), Is.True);
            GridActor enemy = game.Enemies[0]; Call("EnemyHitActor", enemy, game.Duck, 1f);
            Assert.That(game.DuckCaptured, Is.True);
            float stationProgress = station.Progress, casinoProgress = game.CasinoMachine.Progress;
            int chips = game.Chips, coins = game.Coins;
            Call("TickDuck", 99f); Call("AdvanceProcesses", 99f);
            Assert.That(station.Progress, Is.EqualTo(stationProgress));
            Assert.That(game.CasinoMachine.Progress, Is.EqualTo(casinoProgress));
            Assert.That(game.Chips, Is.EqualTo(chips)); Assert.That(game.Coins, Is.EqualTo(coins));
            enemy.Eliminate(); Call("AdvanceOrdersAndRescue", .1f);
            Assert.That(game.DuckCaptured, Is.False);
            RunUntilStationCount(0);
            for (int i = 0; i < 1000 && game.CasinoMachine.Progress == casinoProgress; i++)
            { Call("TickDuck", .05f); Call("AdvanceCasino", .05f); }
            Assert.That(game.CasinoMachine.Progress, Is.GreaterThan(casinoProgress));
            Assert.That(game.CasinoBet, Is.EqualTo(100)); Assert.That(game.CasinoMultiplier, Is.EqualTo(8));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }

        ActivityStation Spawn(ActivityKind kind)
        {
            Assert.That(game.DebugSpawnStation(kind), Is.True);
            ActivityStation station = game.Stations[game.Stations.Count - 1];
            station.duration = 2.2f;
            return station;
        }
        void RunUntilStationCount(int count)
        {
            for (int i = 0; i < 3000 && game.PendingStationCount > count; i++)
            { Call("TickDuck", .05f); Call("AdvanceProcesses", .05f); }
            Assert.That(game.PendingStationCount, Is.EqualTo(count));
        }
        void Put(GridActor actor, ActivityStation station)
        { actor.transform.position = station.transform.position; actor.RefreshCell(); actor.State = ActorState.Idle; }
        T GetUI<T>(string field) => (T)typeof(MainCoreGameplayUI).GetField(field, Private).GetValue(ui);
        T GetGame<T>(string field) => (T)typeof(MainRunGameplaySystems).GetField(field, Private).GetValue(game);
        void Set(string field, object value) => typeof(MainRunGameplaySystems).GetField(field, Private).SetValue(game, value);
        void Call(string name, params object[] args) => typeof(MainRunGameplaySystems).GetMethod(name, Private).Invoke(game, args);
    }
}
