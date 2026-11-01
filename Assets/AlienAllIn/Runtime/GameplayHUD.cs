using UnityEngine;
using UnityEngine.UI;

namespace AlienAllIn
{
    public sealed class GameplayHUD : MonoBehaviour
    {
        public GameplayController game;
        public Text phaseText, cashText, objectiveText, selectedText, messageText, toolText;
        public Button startButton, pauseButton, restartButton, trampolineButton, duckButton, sealButton;

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
            phaseText.text = game.Paused ? "PAUSED" : game.Phase == StagePhase.Placement ? "01 / PREPARE" : game.Phase == StagePhase.Won ? "SHIFT COMPLETE" : game.Phase == StagePhase.Lost ? "TEAM WIPED" : $"01 / LIVE   {game.Elapsed:0}s";
            cashText.text = $"CASH  {game.Cash}";
            objectiveText.text = "OBJECTIVES\n\n";
            foreach (var activity in game.activities)
                objectiveText.text += $"{activity.activityName}  {activity.Progress:P0}\n{activity.requiredKind} / {(char)('A' + activity.Cell.x)}{activity.Cell.y + 1}\n\n";
            GridActor actor = game.Selected;
            selectedText.text = actor ? $"{actor.kind.ToString().ToUpper()}\nHP {actor.Health:0}/{actor.maxHealth:0}\n" + (actor == game.seal ? $"Switch {actor.SwitchCharge:P0}" : "") : "Select a character,\nthen choose a cell.";
            messageText.text = game.Message;
            toolText.text = $"TRAMPOLINE  x{game.Trampolines}";
            startButton.gameObject.SetActive(game.Phase == StagePhase.Placement);
            pauseButton.interactable = game.Phase == StagePhase.Running;
            pauseButton.GetComponentInChildren<Text>().text = game.Paused ? "RESUME" : "PAUSE";
            trampolineButton.interactable = game.Phase == StagePhase.Running && !game.Paused && game.Trampolines > 0;
        }
    }
}
