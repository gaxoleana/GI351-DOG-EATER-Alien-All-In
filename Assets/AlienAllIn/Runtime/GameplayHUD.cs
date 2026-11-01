using UnityEngine;
using UnityEngine.UI;

namespace AlienAllIn
{
    [System.Serializable]
    public sealed class GameplayActivityReadout
    {
        public GameObject root;
        public Text nameText;
        public Text kindText;
        public Text progressText;
        public Image progressFill;
    }

    public sealed class GameplayHUD : MonoBehaviour
    {
        public GameplayController game;
        public Text phaseText, cashText, objectiveText, selectedText, messageText, toolText;
        public Button startButton, pauseButton, restartButton, trampolineButton;
        public Button duckButton, sealButton;
        public GameplayActivityReadout[] activityRows;
        public Image selectedHealthFill;

        GridActor styledSelection;
        bool selectionStyleInitialized;

        void Awake()
        {
            startButton.onClick.AddListener(game.StartShift);
            pauseButton.onClick.AddListener(game.TogglePause);
            restartButton.onClick.AddListener(game.ResetStage);
            trampolineButton.onClick.AddListener(() => game.UseTrampoline());
            duckButton.onClick.AddListener(() => game.Select(game.duck));
            sealButton.onClick.AddListener(() => game.Select(game.seal));
        }

        public void Refresh()
        {
            phaseText.text = game.Paused ? "PAUSED" : game.Phase == StagePhase.Placement ? "READY" : game.Phase == StagePhase.Won ? "SHIFT COMPLETE" : game.Phase == StagePhase.Lost ? "TEAM WIPED" : $"ON SHIFT   {game.Elapsed:0}s";
            cashText.text = game.Cash.ToString("000");
            objectiveText.text = "OBJECTIVES";

            if (activityRows != null)
            {
                for (int i = 0; i < activityRows.Length; i++)
                {
                    GameplayActivityReadout row = activityRows[i];
                    bool hasActivity = game.activities != null && i < game.activities.Length && game.activities[i];
                    if (row == null) continue;
                    if (row.root) row.root.SetActive(hasActivity);
                    if (!hasActivity) continue;

                    ActivityStation activity = game.activities[i];
                    float progress = Mathf.Clamp01(activity.Progress);
                    if (row.nameText) row.nameText.text = activity.activityName;
                    if (row.kindText) row.kindText.text = activity.requiredKind.ToString().ToUpperInvariant();
                    if (row.progressText) row.progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
                    if (row.progressFill)
                    {
                        row.progressFill.fillAmount = progress;
                        row.progressFill.color = activity.requiredKind == ActorKind.Duck
                            ? new Color(1f, .78f, .25f)
                            : new Color(.40f, .94f, .77f);
                    }
                }
            }

            GridActor actor = game.Selected;
            selectedText.text = actor
                ? $"{actor.kind.ToString().ToUpperInvariant()}\nHP  {actor.Health:0} / {actor.maxHealth:0}" + (actor == game.seal ? $"\nSWITCH  {actor.SwitchCharge:P0}" : "")
                : "Choose a character";
            if (selectedHealthFill)
            {
                selectedHealthFill.fillAmount = actor ? Mathf.Clamp01(actor.Health / Mathf.Max(1f, actor.maxHealth)) : 0;
                selectedHealthFill.color = actor && actor.Health / Mathf.Max(1f, actor.maxHealth) < .35f
                    ? new Color(.96f, .38f, .51f)
                    : actor && actor.kind == ActorKind.Duck ? new Color(1f, .78f, .25f) : new Color(.40f, .94f, .77f);
            }

            messageText.text = game.Message;
            toolText.text = $"USE TRAMPOLINE  x{game.Trampolines}";
            startButton.gameObject.SetActive(game.Phase == StagePhase.Placement);
            pauseButton.interactable = game.Phase == StagePhase.Running;
            pauseButton.GetComponentInChildren<Text>().text = game.Paused ? "RESUME" : "PAUSE";
            trampolineButton.interactable = game.Phase == StagePhase.Running && !game.Paused && game.Trampolines > 0;
            toolText.color = trampolineButton.interactable ? new Color(.045f, .075f, .13f) : new Color(.63f, .76f, .75f);

            if (!selectionStyleInitialized || styledSelection != actor)
            {
                SetSelectionButton(duckButton, actor == game.duck, new Color(1f, .78f, .25f));
                SetSelectionButton(sealButton, actor == game.seal, new Color(.40f, .94f, .77f));
                styledSelection = actor;
                selectionStyleInitialized = true;
            }
        }

        static void SetSelectionButton(Button button, bool selected, Color accent)
        {
            if (!button) return;
            Color inactive = new Color(.10f, .17f, .21f);
            Color baseColor = selected ? accent : inactive;
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, .12f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, .18f);
            colors.disabledColor = inactive;
            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>();
            if (label) label.color = selected ? new Color(.045f, .075f, .13f) : new Color(.70f, .82f, .81f);
        }
    }
}
