using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AlienAllIn
{
    /// <summary>Binds the saved Main scene HUD. Creates no UI objects during play.</summary>
    [DisallowMultipleComponent]
    public sealed class MainCoreGameplayUI : MonoBehaviour
    {
        [SerializeField] RunLoopController runLoop;
        [SerializeField] MainRunGameplaySystems systems;
        [SerializeField] BoardGrid board;
        [SerializeField] Camera stageCamera;
        [SerializeField] MainSceneLayout screenLayout;
        [SerializeField] MainDebugPanel debugPanel;
        [SerializeField] BoardInteractionFeedback feedback;
        [SerializeField] RectTransform itemBarRect;
        [SerializeField] Text enemyStatusText;
        [SerializeField] GameObject optionsPanel;
        [SerializeField] Button optionsResume, optionsRestart, optionsClose;
        bool resumeAfterOptions;
        public BoardInteractionFeedback Feedback => feedback;
        public bool OptionsOpen => optionsPanel && optionsPanel.activeSelf;
        [SerializeField] bool enableDebugInReleaseBuild;
        [Header("Saved scene HUD")]
        [SerializeField] Canvas hudCanvas;
        [SerializeField] Text runText, walletText, scoreText, statusText, processText;
        [SerializeField] Button startButton, pauseButton, restartButton;
        [SerializeField] RectTransform headerRect, leftRect, rightRect, boardDropRect;
        [SerializeField] RectTransform[] backgroundRects;
        [SerializeField] GridActor[] cardActors;
        [SerializeField] Button[] cardButtons;
        [SerializeField] Text[] cardTexts;
        [SerializeField] Image[] cardBars;
        [SerializeField] Button[] itemButtons;
        [SerializeField] Text[] itemTexts;
        [Header("Character popup (enable in Hierarchy to preview)")]
        [SerializeField] GameObject popupOverlay;
        [SerializeField] RectTransform popupBox;
        [SerializeField] Button popupOutside, popupClose, popupUpgrade;
        [SerializeField] Text popupName, popupStats, popupDetails, popupAction;
        [SerializeField] Image popupPortrait;
        [Header("Session result")]
        [SerializeField] GameObject resultPanel;
        [SerializeField] Text resultText;
        [SerializeField] Button playAgain;
        GridActor selectedActor;
        bool resumeAfterPopup;
        float refreshClock;
        public bool CharacterPopupOpen => popupOverlay && popupOverlay.activeSelf;
        public GridActor SelectedActor => selectedActor;
        public Canvas HudCanvas => hudCanvas;

        void Awake()
        {
            if (!hudCanvas || !popupOverlay) { Debug.LogError("Main HUD needs its saved scene references.", this); enabled = false; return; }
            popupOverlay.SetActive(false); resultPanel.SetActive(false);
            startButton.onClick.AddListener(() => runLoop.StartRun());
            pauseButton.onClick.AddListener(TogglePause);
            restartButton.onClick.AddListener(OpenOptions);
            optionsPanel.SetActive(false);
            optionsResume.onClick.AddListener(CloseOptions);
            optionsClose.onClick.AddListener(CloseOptions);
            optionsRestart.onClick.AddListener(() => runLoop.Restart());
            playAgain.onClick.AddListener(() => runLoop.Restart());
            popupOutside.onClick.AddListener(CloseCharacter);
            popupClose.onClick.AddListener(CloseCharacter);
            popupUpgrade.onClick.AddListener(UpgradeSelected);
            for (int i = 0; i < cardActors.Length; i++)
            {
                GridActor actor = cardActors[i]; cardButtons[i].onClick.AddListener(() => OpenCharacter(actor));
            }
            for (int i = 0; i < itemButtons.Length; i++)
            {
                var kind = (FieldItemKind)i; itemButtons[i].onClick.AddListener(() => UseItem(kind));
            }
            boardDropRect.GetComponent<BoardDropSurface>().Configure(this);
            foreach (var target in hudCanvas.GetComponentsInChildren<AlienDropTarget>(true))
                target.Configure(this, target.GetComponent<UIResourceDragToken>().Actor);
            runLoop.RunRestarted += HandleRestart;
            runLoop.RunEnded += HandleEnded;
            if (debugPanel) debugPanel.Configure(runLoop, systems, enableDebugInReleaseBuild);
            RefreshUI();
        }

        void OnDestroy()
        {
            if (runLoop) { runLoop.RunRestarted -= HandleRestart; runLoop.RunEnded -= HandleEnded; runLoop.InteractionModalOpen = false; }
            if (screenLayout) screenLayout.FocusActor(null);
        }
        void HandleRestart() { if (optionsPanel) optionsPanel.SetActive(false); resumeAfterOptions = false; DismissCharacter(false); RefreshUI(); }
        void HandleEnded(RunState state) { DismissCharacter(false); RefreshUI(); }

        public void OpenCharacter(GridActor actor)
        {
            if (OptionsOpen || !actor || !actor.Alive || debugPanel && debugPanel.IsOpen ||
                runLoop.State == RunState.Defeated || runLoop.State == RunState.RunComplete) return;
            if (!CharacterPopupOpen) resumeAfterPopup = runLoop.State == RunState.Running;
            selectedActor = actor;
            if (runLoop.State == RunState.Running) runLoop.Pause();
            runLoop.InteractionModalOpen = true;
            popupOverlay.SetActive(true); screenLayout.FocusActor(actor); RefreshUI();
        }
        public void CloseCharacter() => DismissCharacter(true);
        void DismissCharacter(bool restoreRun)
        {
            if (!popupOverlay) return;
            bool resume = CharacterPopupOpen && resumeAfterPopup && restoreRun;
            popupOverlay.SetActive(false); selectedActor = null; resumeAfterPopup = false;
            runLoop.InteractionModalOpen = false; screenLayout.FocusActor(null);
            if (resume && runLoop.State == RunState.Paused) runLoop.Resume();
        }
        void UpgradeSelected()
        {
            if (!selectedActor) return;
            if (selectedActor.kind == ActorKind.Duck) systems.TryFreeDuck();
            else systems.TryBuyAttackUpgrade(selectedActor);
            RefreshUI();
        }
        void UseItem(FieldItemKind kind)
        {
            ActorKind owner = kind == FieldItemKind.AirPump ? ActorKind.Pufferfish : kind == FieldItemKind.LaserGun ? ActorKind.Cat : ActorKind.Seal;
            systems.TryUseItem(kind, systems.GetActorForUI(owner)); RefreshUI();
        }
        public void OpenOptions()
        {
            CloseCharacter(); resumeAfterOptions = runLoop.State == RunState.Running;
            if (resumeAfterOptions) runLoop.Pause();
            runLoop.InteractionModalOpen = true; optionsPanel.SetActive(true);
        }
        public void CloseOptions()
        {
            optionsPanel.SetActive(false); runLoop.InteractionModalOpen = false;
            if (resumeAfterOptions) runLoop.Resume(); resumeAfterOptions = false;
        }
        void TogglePause()
        {
            if (runLoop.State == RunState.Running) runLoop.Pause();
            else if (runLoop.State == RunState.Paused) runLoop.Resume();
        }
        void Update()
        {
            if (OptionsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseOptions();
            if (CharacterPopupOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseCharacter();
            refreshClock -= Time.unscaledDeltaTime;
            if (refreshClock > 0) return;
            refreshClock = .1f; RefreshUI();
        }
        public void RefreshUI()
        {
            if (!runLoop || !systems || !hudCanvas) return;
            SetRegion(itemBarRect, screenLayout.ItemBar);
            SetRegion(headerRect, screenLayout.Header); SetRegion(leftRect, screenLayout.LeftColumn);
            SetRegion(rightRect, screenLayout.RightColumn); SetRegion(boardDropRect, screenLayout.BoardViewport);
            SetRegion((RectTransform)resultPanel.transform, screenLayout.BoardViewport);
            Rect view = screenLayout.BoardViewport;
            SetRegion(backgroundRects[0], Rect.MinMaxRect(0, 0, view.xMin, 1));
            SetRegion(backgroundRects[1], Rect.MinMaxRect(view.xMax, 0, 1, 1));
            SetRegion(backgroundRects[2], Rect.MinMaxRect(view.xMin, 0, view.xMax, view.yMin));
            SetRegion(backgroundRects[3], Rect.MinMaxRect(view.xMin, view.yMax, view.xMax, 1));
            int seconds = Mathf.CeilToInt(runLoop.TimeRemainingSeconds);
            runText.text = $"Grid {board.ActiveWidth}x{board.ActiveHeight}    {seconds / 60:00}:{seconds % 60:00}";
            walletText.text = $"Coins  {systems.Coins}     Chips  {systems.Chips}";
            scoreText.text = $"Score  {systems.Score}";
            statusText.text = systems.DuckCaptured ? $"Duck kidnapped! Rescue in {systems.DuckRescueRemaining:0}s" : systems.StatusMessage;
            for (int i = 0; i < cardActors.Length; i++)
            {
                var actor = cardActors[i]; bool duck = actor.kind == ActorKind.Duck;
                cardTexts[i].text = duck ? $"Duck\n{(systems.DuckCaptured ? "KIDNAPPED" : actor.State.ToString())}" :
                    $"{actor.kind}\nHP {actor.Health:0}/{actor.EffectiveMaxHealth:0}  ATK {actor.EffectiveDamage:0.0}\n{actor.State}";
                cardButtons[i].interactable = actor.Alive;
                var bar = cardBars[i].rectTransform; bar.anchorMax = new Vector2(actor.UltimateCharge, 1);
            }
            var list = new StringBuilder("STATIONS\n");
            foreach (var station in systems.Stations) if (station) list.AppendLine($"{station.activityName}  {station.Progress:P0}");
            processText.text = list.ToString();
            if (enemyStatusText) enemyStatusText.text = $"ENEMY\n{systems.Enemies.Count}";
            for (int i = 0; i < itemButtons.Length; i++)
                itemTexts[i].text = $"{new[] { "Air Pump", "Laser Gun", "Trampoline" }[i]}  x{systems.GetItemCount((FieldItemKind)i)}";
            bool ended = runLoop.State == RunState.Defeated || runLoop.State == RunState.RunComplete;
            resultPanel.SetActive(ended);
            if (ended) resultText.text = $"{(runLoop.State == RunState.Defeated ? "GAME OVER" : "TIME UP")}\n\nSCORE  {systems.Score}\nCoins {systems.Coins}   Chips {systems.Chips}";
            if (!CharacterPopupOpen || !selectedActor) return;
            bool isDuck = selectedActor.kind == ActorKind.Duck;
            popupName.text = selectedActor.kind.ToString();
            var renderer = selectedActor.GetComponentInChildren<SpriteRenderer>();
            popupPortrait.sprite = renderer ? renderer.sprite : null;
            popupStats.text = isDuck ? (systems.DuckCaptured ? "Kidnapped" : "Station worker") : $"HP: {selectedActor.Health:0}/{selectedActor.EffectiveMaxHealth:0}   ATK: {selectedActor.EffectiveDamage:0.0}";
            int price = systems.GetAttackUpgradeCost(selectedActor);
            popupDetails.text = isDuck ? (systems.DuckCaptured ? $"Rescue within {systems.DuckRescueRemaining:0}s\nRansom: {systems.DuckRansomChips} Chips" : "Duck cannot take HP damage.\nEnemies can kidnap it.") :
                $"ATK +{systems.AttackUpgradeAmount:0.0}    Level {selectedActor.DamageUpgradeRank}\nCoins: {systems.Coins}   Next: {(price == int.MaxValue ? "MAX" : price.ToString())}";
            popupAction.text = isDuck ? $"Free the Duck  ({systems.DuckRansomChips} Chips)" : $"Upgrade  ({price} Coins)";
            popupUpgrade.gameObject.SetActive(!isDuck || systems.DuckCaptured);
            popupUpgrade.interactable = isDuck ? systems.DuckCaptured && systems.Chips >= systems.DuckRansomChips : price < int.MaxValue && systems.Coins >= price;
        }
        public GridActor ActorAtScreenPoint(Vector2 screen)
        {
            if (!stageCamera.pixelRect.Contains(screen)) return null;
            Vector3 point = stageCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -stageCamera.transform.position.z));
            GridActor nearest = null; float best = .55f * board.cellSize;
            foreach (var actor in runLoop.Survivors)
            {
                if (!actor || !actor.Alive) continue;
                float distance = Vector2.Distance(point, actor.transform.position);
                if (distance >= best) continue;
                nearest = actor; best = distance;
            }
            return nearest;
        }
        internal bool TryDropActor(GridActor actor, Vector2 screenPosition)
        {
            if (OptionsOpen || CharacterPopupOpen || !stageCamera.pixelRect.Contains(screenPosition) || !actor) return false;
            Vector3 world = stageCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -stageCamera.transform.position.z));
            return board.TryWorldToCell(world, out Vector2Int cell) && systems.TryOrderMove(actor, cell);
        }
        internal bool TryDropOnActor(UIResourceDragToken token, GridActor target)
        {
            if (!token || !target || OptionsOpen || CharacterPopupOpen) return false;
            if (token.Payload == UIDragPayload.Item) return systems.TryUseItem(token.Item, target);
            return token.Payload == UIDragPayload.Actor && systems.TryActivateUltimateSwitch(token.Actor, target);
        }
        internal void NotifyDrop(bool accepted) { if (!accepted) systems.SetStatusFromUI("Cannot move there."); RefreshUI(); }
        static void SetRegion(RectTransform rect, Rect bounds) { rect.anchorMin = bounds.min; rect.anchorMax = bounds.max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
