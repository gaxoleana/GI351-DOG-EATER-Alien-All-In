using System;
using UnityEngine;
using UnityEngine.UI;

namespace AlienAllIn
{
    public sealed class PlacementScreen
    {
        readonly TeamPlacementGame game;
        readonly Button[,] tileButtons = new Button[BoardGrid.BoardSize, BoardGrid.BoardSize];
        readonly Text[,] tileLabels = new Text[BoardGrid.BoardSize, BoardGrid.BoardSize];
        readonly Button[] characterButtons = new Button[4];
        readonly Text statusText;
        readonly Button startButton;

        public PlacementScreen(TeamPlacementGame game, Transform parent)
        {
            this.game = game;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform canvas = CreateCanvas(parent);

            Text title = CreateText("ALIEN ALL IN     /     TEAM PLACEMENT", canvas, font, 30);
            SetAnchors(title.rectTransform, new Vector2(.04f, .9f), new Vector2(.96f, .98f));

            RectTransform board = CreatePanel("Board", canvas, new Vector2(.04f, .08f), new Vector2(.64f, .87f), new Color(.08f, .11f, .16f));
            CreateBoard(board, font);

            RectTransform controls = CreatePanel("Team Controls", canvas, new Vector2(.68f, .08f), new Vector2(.96f, .87f), new Color(.08f, .11f, .16f));
            Text help = CreateText("Select a character, then choose a tile.\nMove a placed character by selecting it again.", controls, font, 19);
            SetAnchors(help.rectTransform, new Vector2(.04f, .81f), new Vector2(.96f, .99f));
            CreateCharacterButtons(controls, font);

            statusText = CreateText(game.StatusMessage, controls, font, 19);
            SetAnchors(statusText.rectTransform, new Vector2(.04f, .18f), new Vector2(.96f, .36f));
            startButton = CreateButton("START SHIFT", controls, font, new Color(.12f, .46f, .39f), () =>
            {
                game.StartShift();
                Refresh();
            });
            SetAnchors((RectTransform)startButton.transform, new Vector2(.04f, .08f), new Vector2(.96f, .16f));
            Refresh();
        }

        void CreateBoard(Transform parent, Font font)
        {
            var gridObject = new GameObject("Tiles", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridObject.transform.SetParent(parent, false);
            var rect = (RectTransform)gridObject.transform;
            SetAnchors(rect, new Vector2(.02f, .02f), new Vector2(.98f, .98f));
            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = BoardGrid.BoardSize;
            layout.spacing = new Vector2(3, 3);
            layout.cellSize = new Vector2(54, 54);
            gridObject.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            gridObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            for (int row = 0; row < BoardGrid.BoardSize; row++)
            for (int column = 0; column < BoardGrid.BoardSize; column++)
            {
                int x = column;
                int y = BoardGrid.BoardSize - 1 - row;
                Button tile = CreateButton("", gridObject.transform, font, new Color(.16f, .21f, .29f), () => PlaceCharacter(x, y));
                tileButtons[x, y] = tile;
                tileLabels[x, y] = tile.GetComponentInChildren<Text>();
                tileLabels[x, y].fontSize = 14;
                tileLabels[x, y].text = (x + 1) + "," + (y + 1);
            }
        }

        void CreateCharacterButtons(Transform parent, Font font)
        {
            for (int i = 0; i < game.Team.Count; i++)
            {
                CharacterType type = game.Team[i].Type;
                Button button = CreateButton(game.Team[i].DisplayName, parent, font, new Color(.16f, .25f, .36f), () =>
                {
                    game.SelectCharacter(type);
                    Refresh();
                });
                SetAnchors((RectTransform)button.transform, new Vector2(.04f, .7f - i * .105f), new Vector2(.96f, .78f - i * .105f));
                characterButtons[i] = button;
            }
        }

        void PlaceCharacter(int x, int y)
        {
            game.TryPlaceSelectedCharacter(new Vector2Int(x, y));
            Refresh();
        }

        void Refresh()
        {
            for (int y = 0; y < BoardGrid.BoardSize; y++)
            for (int x = 0; x < BoardGrid.BoardSize; x++)
            {
                GameCharacter character = game.Board.GetCharacterAt(new Vector2Int(x, y));
                tileLabels[x, y].text = character == null ? (x + 1) + "," + (y + 1) : ShortName(character.Type);
                tileButtons[x, y].image.color = character == null ? new Color(.16f, .21f, .29f) : new Color(.18f, .43f, .48f);
                tileButtons[x, y].interactable = game.Phase == GamePhase.Placement;
            }

            for (int i = 0; i < characterButtons.Length; i++)
            {
                GameCharacter character = game.Team[i];
                string marker = character == game.SelectedCharacter ? "  >  " : "";
                string placed = character.IsPlaced ? "  [PLACED]" : "";
                characterButtons[i].GetComponentInChildren<Text>().text = marker + character.DisplayName + placed;
                characterButtons[i].interactable = game.Phase == GamePhase.Placement;
            }

            statusText.text = game.StatusMessage;
            startButton.interactable = game.Phase == GamePhase.Placement && AllCharactersPlaced();
            startButton.GetComponentInChildren<Text>().text = game.Phase == GamePhase.Running ? "SHIFT STARTED" : "START SHIFT";
        }

        bool AllCharactersPlaced()
        {
            foreach (GameCharacter character in game.Team)
                if (!character.IsPlaced) return false;
            return true;
        }

        static string ShortName(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.Seal: return "SEAL";
                case CharacterType.Pufferfish: return "PUFF";
                case CharacterType.Cat: return "CAT";
                default: return "DUCK";
            }
        }

        static RectTransform CreateCanvas(Transform parent)
        {
            var canvasObject = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            return (RectTransform)canvasObject.transform;
        }

        static RectTransform CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var panel = CreateRect(name, parent, min, max);
            panel.gameObject.AddComponent<Image>().color = color;
            return panel;
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = (RectTransform)gameObject.transform;
            SetAnchors(rect, min, max);
            return rect;
        }

        static Text CreateText(string value, Transform parent, Font font, int fontSize)
        {
            var text = CreateRect("Text", parent, Vector2.zero, Vector2.one).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = value;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Button CreateButton(string text, Transform parent, Font font, Color color, Action onClick)
        {
            RectTransform rect = CreateRect(string.IsNullOrEmpty(text) ? "Board Tile" : text, parent, Vector2.zero, Vector2.one);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(text, rect, font, 20);
            button.onClick.AddListener(() => onClick());
            return button;
        }

        static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
