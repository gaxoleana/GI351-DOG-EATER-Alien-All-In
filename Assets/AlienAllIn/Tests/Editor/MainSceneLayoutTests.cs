using AlienAllIn.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AlienAllIn.Tests
{
    public sealed class MainSceneLayoutTests
    {
        MainSceneLayout layout;
        BoardGrid board;
        Camera camera;

        [SetUp]
        public void LoadMain()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            layout = Object.FindAnyObjectByType<MainSceneLayout>();
            board = Object.FindAnyObjectByType<BoardGrid>();
            camera = Camera.main;
            Assert.That(layout, Is.Not.Null);
        }

        [TestCase(16f / 9f)]
        [TestCase(16f / 10f)]
        [TestCase(4f / 3f)]
        [TestCase(21f / 9f)]
        public void WholeVisibleBoardFitsBetweenHudRegionsAtEveryExpansion(float windowAspect)
        {
            camera.aspect = windowAspect * layout.BoardViewport.width / layout.BoardViewport.height;
            var progression = Object.FindAnyObjectByType<StageGridProgression>();
            float previousSize = 0;
            foreach (float progress in new[] { 0f, .5f, 1f })
            {
                progression.ApplyRunProgress(progress);
                layout.RefreshCamera();
                AssertBoardFits();
                Assert.That(camera.orthographicSize, Is.GreaterThanOrEqualTo(previousSize), "Expansion must zoom out inside the reserved viewport.");
                previousSize = camera.orthographicSize;
            }
            Assert.That(layout.BoardViewport.Overlaps(layout.Header), Is.False);
            Assert.That(layout.BoardViewport.Overlaps(layout.LeftColumn), Is.False);
            Assert.That(layout.BoardViewport.Overlaps(layout.RightColumn), Is.False);
        }

        [Test]
        public void CameraRefitsWhenMaximumBoardGrowsOrMoves()
        {
            layout.FrameMaximumBoard = true;
            board.width = 18; board.height = 14;
            board.transform.position += new Vector3(3, -2);
            board.transform.localScale = new Vector3(1.2f, .8f, 1);
            layout.RefreshCamera();
            AssertBoardFits();
        }

        [Test]
        public void AuthoredMainBoardUsesNeutralGrayPalette()
        {
            var cells = board.GetComponentsInChildren<BoardCell>(true);
            Assert.That(cells.Length, Is.EqualTo(100));
            foreach (var cell in cells)
            {
                Assert.That(cell.baseColor.r, Is.EqualTo(cell.baseColor.g));
                Assert.That(cell.baseColor.g, Is.EqualTo(cell.baseColor.b));
                Assert.That(cell.surface.color.r, Is.EqualTo(cell.surface.color.g));
                Assert.That(cell.surface.color.g, Is.EqualTo(cell.surface.color.b));
            }
        }

        void AssertBoardFits()
        {
            Vector2Int min = layout.FrameMaximumBoard ? Vector2Int.zero : board.ActiveMinCell;
            int width = layout.FrameMaximumBoard ? board.width : board.ActiveWidth;
            int height = layout.FrameMaximumBoard ? board.height : board.ActiveHeight;
            Vector3 offset = new Vector3(min.x * board.cellSize, min.y * board.cellSize);
            foreach (Vector3 corner in new[] { offset, offset + new Vector3(width * board.cellSize, 0), offset + new Vector3(0, height * board.cellSize), offset + new Vector3(width * board.cellSize, height * board.cellSize) })
            {
                Vector3 viewport = camera.WorldToViewportPoint(board.transform.TransformPoint(corner));
                Assert.That(viewport.x, Is.InRange(.001f, .999f));
                Assert.That(viewport.y, Is.InRange(.001f, .999f));
            }
        }
    }
}
