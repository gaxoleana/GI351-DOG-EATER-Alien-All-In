using AlienAllIn.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AlienAllIn.Tests
{
    public sealed class GameplaySceneTests
    {
        GameplayController game;

        [SetUp]
        public void LoadAuthoredScene()
        {
            EditorSceneManager.OpenScene(GameplaySceneBuilder.ScenePath);
            game = Object.FindAnyObjectByType<GameplayController>();
            game.ResetStage();
        }

        [Test]
        public void BoardHas100EditableCellsSpacedOneWorldUnitApart()
        {
            Assert.That(game.board.GetComponentsInChildren<BoardCell>().Length, Is.EqualTo(100));
            Assert.That(Vector3.Distance(game.board.CellCenter(Vector2Int.zero), game.board.CellCenter(Vector2Int.right)), Is.EqualTo(1).Within(.0001));
            foreach (var cell in game.board.GetComponentsInChildren<BoardCell>())
            {
                Assert.That(game.board.TryWorldToCell(cell.transform.position, out var coordinate), Is.True);
                Assert.That(coordinate, Is.EqualTo(cell.coordinate));
            }
        }

        [Test]
        public void CellLookupHandlesBoundariesAndTransformedBoard()
        {
            var board = game.board;
            Assert.That(board.TryWorldToCell(board.transform.TransformPoint(new Vector3(-.001f, 0)), out _), Is.False);
            Assert.That(board.TryWorldToCell(board.transform.TransformPoint(new Vector3(10, 0)), out _), Is.False);
            Assert.That(board.TryWorldToCell(board.transform.TransformPoint(new Vector3(0, 10)), out _), Is.False);
            Assert.That(board.TryWorldToCell(board.transform.TransformPoint(new Vector3(1, 1)), out var edge), Is.True);
            Assert.That(edge, Is.EqualTo(new Vector2Int(1, 1)));
            board.transform.SetPositionAndRotation(new Vector3(12, 8), Quaternion.Euler(0, 0, 30));
            board.transform.localScale = Vector3.one * 2;
            Assert.That(board.TryWorldToCell(board.CellCenter(new Vector2Int(9, 9)), out var coordinate), Is.True);
            Assert.That(coordinate, Is.EqualTo(new Vector2Int(9, 9)));
        }

        [Test]
        public void OccupancyFollowsActualWorldPositionAndClearsOldCell()
        {
            var previous = game.board.GetCell(game.duck.CurrentCell);
            Assert.That(previous.Occupants, Has.Member(game.duck));
            game.duck.transform.position = game.board.CellCenter(new Vector2Int(8, 3));
            game.RefreshBoard();
            Assert.That(previous.Occupants, Has.No.Member(game.duck));
            Assert.That(game.duck.CurrentCell, Is.EqualTo(new Vector2Int(8, 3)));
            Assert.That(game.board.GetCell(new Vector2Int(8, 3)).Occupants, Has.Member(game.duck));
            game.duck.Hurt(1000); game.RefreshBoard();
            Assert.That(game.board.GetCell(new Vector2Int(8, 3)).Occupants, Is.Empty);
        }

        [Test]
        public void PlacementRejectsOccupiedAndOutsideCells()
        {
            game.Select(game.duck);
            Assert.That(game.CommandCell(game.seal.CurrentCell), Is.False);
            Assert.That(game.CommandCell(new Vector2Int(-1, 0)), Is.False);
            Assert.That(game.CommandCell(new Vector2Int(1, 1)), Is.True);
            Assert.That(game.duck.CurrentCell, Is.EqualTo(new Vector2Int(1, 1)));
        }

        [Test]
        public void DefaultStageIsWinnableWithCollectionCombatAndBothActivities()
        {
            game.StartShift();
            for (int i = 0; i < 2400 && game.Phase == StagePhase.Running; i++) game.Advance(.05f);
            Assert.That(game.Phase, Is.EqualTo(StagePhase.Won), $"Elapsed {game.Elapsed}; Duck {game.duck.Health}; Seal {game.seal.Health}; Objectives {game.activities[0].Progress}, {game.activities[1].Progress}");
            Assert.That(game.pickups[0].Collected, Is.True);
            Assert.That(game.Trampolines, Is.EqualTo(1));
            Assert.That(game.DefeatedEnemies, Is.GreaterThan(0));
            Assert.That(game.Cash, Is.GreaterThan(0));
            Assert.That(game.activities[0].Complete && game.activities[1].Complete, Is.True);
        }

        [Test]
        public void DuckFleesThreatAndCannotBeGrabbedMidFlight()
        {
            game.StartShift();
            var enemy = game.SpawnEnemy();
            enemy.transform.position = game.duck.transform.position + Vector3.left * 1.2f;
            game.Advance(.02f);
            Assert.That(game.duck.State, Is.EqualTo(ActorState.Fleeing));
            Assert.That(game.duck.CanPickUp, Is.False);
        }

        [Test]
        public void PauseFreezesSimulationAndRestartRestoresAuthoredPositions()
        {
            Vector3 initialPosition = game.duck.transform.position;
            game.StartShift(); game.Advance(1); game.TogglePause();
            float elapsed = game.Elapsed;
            Vector3 position = game.duck.transform.position;
            game.Advance(10);
            Assert.That(game.Elapsed, Is.EqualTo(elapsed));
            Assert.That(game.duck.transform.position, Is.EqualTo(position));
            game.ResetStage();
            Assert.That(game.Phase, Is.EqualTo(StagePhase.Placement));
            Assert.That(game.duck.transform.position, Is.EqualTo(initialPosition));
            Assert.That(game.Trampolines, Is.Zero);
            Assert.That(game.Cash, Is.Zero);
            Assert.That(game.Enemies, Is.Empty);
            Assert.That(game.activities[0].Progress, Is.Zero);
        }

        [Test]
        public void TeamWipeLosesAndEnemyPrefabHas10HP()
        {
            Assert.That(game.enemyPrefab.maxHealth, Is.EqualTo(10));
            game.StartShift(); game.duck.Hurt(1000); game.seal.Hurt(1000); game.Advance(.02f);
            Assert.That(game.Phase, Is.EqualTo(StagePhase.Lost));
        }

        [Test]
        public void CollectedTrampolineIsConsumedOnceAndSwitchRequiresCharge()
        {
            game.firstEnemyDelay = 100;
            game.StartShift(); game.Select(game.seal);
            Assert.That(game.CommandCell(new Vector2Int(5, 4)), Is.False);
            for (int i = 0; i < 100; i++) game.Advance(.05f);
            Assert.That(game.Trampolines, Is.EqualTo(1));
            Assert.That(game.UseTrampoline(), Is.True);
            Assert.That(game.UseTrampoline(), Is.False);
            Assert.That(game.Trampolines, Is.Zero);
            game.seal.AddCharge(30);
            Assert.That(game.CommandCell(new Vector2Int(5, 4)), Is.True);
            Assert.That(game.seal.CurrentCell, Is.EqualTo(new Vector2Int(5, 4)));
            Assert.That(game.seal.SwitchCharge, Is.Zero);
        }

        [Test]
        public void WrongSpeciesDoesNotAdvanceAnActivity()
        {
            var casino = game.activities[0];
            game.seal.transform.position = casino.transform.position;
            game.RefreshBoard();
            Assert.That(casino.Advance(2, game.seal, false), Is.Zero);
            Assert.That(casino.Progress, Is.Zero);
        }
    }
}
