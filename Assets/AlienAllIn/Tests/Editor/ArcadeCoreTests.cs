using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AlienAllIn.Tests
{
    public sealed class ArcadeCoreTests
    {
        [SetUp] public void LoadMain() => EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

        [Test]
        public void FiveMinuteSessionShowsOnlyUnlockedBoardAndExpands()
        {
            var stage = Object.FindAnyObjectByType<StageGridProgression>();
            Assert.That(stage.RunDurationSeconds, Is.EqualTo(300));
            foreach (var sample in new[] { (progress: 0f, size: 5), (progress: .25f, size: 5), (progress: .5f, size: 7), (progress: .75f, size: 10) })
            {
                stage.ApplyRunProgress(sample.progress);
                Assert.That(stage.Board.ActiveWidth, Is.EqualTo(sample.size));
                Assert.That(stage.Board.GetComponentsInChildren<BoardCell>().Length, Is.EqualTo(sample.size * sample.size));
                Assert.That(stage.Board.KeepLockedCellsVisible, Is.False);
            }
        }

        [Test]
        public void GddStationRollAndDistinctVisualPrefabsAreConfigured()
        {
            var systems = Object.FindAnyObjectByType<MainRunGameplaySystems>();
            var data = new SerializedObject(systems);
            Assert.That(data.FindProperty("processSpawnIntervalSeconds").floatValue, Is.EqualTo(60));
            Assert.That(data.FindProperty("processSpawnChance").floatValue, Is.EqualTo(.25f));
            var stations = data.FindProperty("activitySettings");
            var enemies = data.FindProperty("enemySettings");
            var stationSprites = new System.Collections.Generic.HashSet<Sprite>();
            var enemySprites = new System.Collections.Generic.HashSet<Sprite>();
            for (int i = 0; i < stations.arraySize; i++)
            {
                var prefab = stations.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue as ActivityStation;
                Assert.That(prefab, Is.Not.Null);
                Assert.That(prefab.artwork, Is.Not.Null);
                Assert.That(prefab.progressFill, Is.Not.Null);
                Assert.That(prefab.allowUniversalDuckWorker, Is.True);
                Assert.That(prefab.requiredKind, Is.EqualTo(ActorKind.Duck));
                Assert.That(prefab.artwork.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                stationSprites.Add(prefab.artwork.sprite);
            }
            for (int i = 0; i < enemies.arraySize; i++)
            {
                var prefab = enemies.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue as GridActor;
                Assert.That(prefab, Is.Not.Null);
                Assert.That(prefab.maxHealth, Is.EqualTo(10));
                Assert.That(prefab.GetComponent<EnemyPresentation>(), Is.Not.Null);
                var body = prefab.GetComponent<SpriteRenderer>();
                Assert.That(body.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                enemySprites.Add(body.sprite);
            }
            Assert.That(stationSprites.Count, Is.EqualTo(3));
            Assert.That(enemySprites.Count, Is.EqualTo(3));
        }
    }
}
