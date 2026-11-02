using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AlienAllIn
{
    /// <summary>Binds the saved Main scene HUD. Creates no UI objects during play.</summary>
    [DisallowMultipleComponent]
    public sealed partial class MainCoreGameplayUI : MonoBehaviour
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
        [Header("Chip exchange shop")]
        [SerializeField] GameObject shopPanel;
        [SerializeField] Button shopButton, shopClose, exchangeOne, exchangeAll;
        [SerializeField] Button exchangeCoinOne, exchangeCoinAll;
        [SerializeField] Text shopBalance;
        [SerializeField] GameObject casinoControlsRoot;
        [SerializeField] Slider casinoBetSlider;
        [SerializeField] Text casinoBetZeroLabel, casinoBetMaxLabel;
        [SerializeField] Button[] casinoMultiplierButtons;
        [SerializeField] Button casinoActiveButton;
        Text shopTitle, exchangeOneLabel, exchangeAllLabel, exchangeCoinOneLabel, exchangeCoinAllLabel;
        bool resumeAfterShop;
        bool casinoPanelMode;
        int selectedCasinoBet;
        int selectedCasinoMultiplier = 2;
        RectTransform shopPanelRect, shopBalanceRect;
        Vector2 shopPanelAnchorMin, shopPanelAnchorMax, shopBalanceAnchorMin, shopBalanceAnchorMax;
        Vector2[] exchangeButtonAnchorMins, exchangeButtonAnchorMaxes;
        Color[] casinoMultiplierBaseColors;
        int shopBalanceBaseFontSize;
        bool shopBalanceBaseBestFit;
        Vector2 shopTitleBaseAnchorMin, shopTitleBaseAnchorMax;
        int[] casinoMultiplierBaseFontSizes;
        bool[] casinoMultiplierBaseBestFit;
        int[] casinoMultiplierBaseMinFontSizes, casinoMultiplierBaseMaxFontSizes;
        bool updatingCasinoSlider;
        public bool ShopOpen => shopPanel && shopPanel.activeSelf;
        public BoardInteractionFeedback Feedback => feedback;
        public bool OptionsOpen => optionsPanel && optionsPanel.activeSelf;
        [SerializeField] bool enableDebugInReleaseBuild;
        [Header("Saved scene HUD")]
        [SerializeField] Canvas hudCanvas;
        [SerializeField] Text runText, walletText, scoreText, statusText, processText;
        [SerializeField] Text gridText;
        [SerializeField] RectTransform teamBarRect, boardFrameRect;
        [SerializeField] Image[] cardPortraits;
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
            shopPanel.SetActive(false);
            shopTitle = shopPanel.transform.Find("Shop Title").GetComponent<Text>();
            shopTitleBaseAnchorMin = shopTitle.rectTransform.anchorMin;
            shopTitleBaseAnchorMax = shopTitle.rectTransform.anchorMax;
            exchangeOneLabel = exchangeOne.GetComponentInChildren<Text>(true);
            exchangeAllLabel = exchangeAll.GetComponentInChildren<Text>(true);
            exchangeCoinOneLabel = exchangeCoinOne.GetComponentInChildren<Text>(true);
            exchangeCoinAllLabel = exchangeCoinAll.GetComponentInChildren<Text>(true);
            shopPanelRect = shopPanel.GetComponent<RectTransform>();
            shopBalanceRect = shopBalance.GetComponent<RectTransform>();
            shopBalanceBaseFontSize = shopBalance.fontSize;
            shopBalanceBaseBestFit = shopBalance.resizeTextForBestFit;
            shopPanelAnchorMin = shopPanelRect.anchorMin; shopPanelAnchorMax = shopPanelRect.anchorMax;
            shopBalanceAnchorMin = shopBalanceRect.anchorMin; shopBalanceAnchorMax = shopBalanceRect.anchorMax;
            Button[] exchangeButtons = { exchangeCoinOne, exchangeCoinAll, exchangeOne, exchangeAll };
            exchangeButtonAnchorMins = new Vector2[exchangeButtons.Length];
            exchangeButtonAnchorMaxes = new Vector2[exchangeButtons.Length];
            for (int i = 0; i < exchangeButtons.Length; i++)
            {
                RectTransform rect = exchangeButtons[i].GetComponent<RectTransform>();
                exchangeButtonAnchorMins[i] = rect.anchorMin;
                exchangeButtonAnchorMaxes[i] = rect.anchorMax;
            }
            casinoMultiplierBaseColors = new Color[casinoMultiplierButtons == null ? 0 : casinoMultiplierButtons.Length];
            casinoMultiplierBaseFontSizes = new int[casinoMultiplierBaseColors.Length];
            casinoMultiplierBaseBestFit = new bool[casinoMultiplierBaseColors.Length];
            casinoMultiplierBaseMinFontSizes = new int[casinoMultiplierBaseColors.Length];
            casinoMultiplierBaseMaxFontSizes = new int[casinoMultiplierBaseColors.Length];
            for (int i = 0; i < casinoMultiplierBaseColors.Length; i++)
            {
                casinoMultiplierBaseColors[i] = casinoMultiplierButtons[i] && casinoMultiplierButtons[i].image
                    ? casinoMultiplierButtons[i].image.color : Color.white;
                Text label = casinoMultiplierButtons[i] ? casinoMultiplierButtons[i].GetComponentInChildren<Text>(true) : null;
                casinoMultiplierBaseFontSizes[i] = label ? label.fontSize : 16;
                casinoMultiplierBaseBestFit[i] = label && label.resizeTextForBestFit;
                casinoMultiplierBaseMinFontSizes[i] = label ? label.resizeTextMinSize : 8;
                casinoMultiplierBaseMaxFontSizes[i] = label ? label.resizeTextMaxSize : 40;
            }
            if (casinoControlsRoot) casinoControlsRoot.SetActive(false);
            shopButton.onClick.AddListener(OpenShop);
            shopClose.onClick.AddListener(CloseShop);
            exchangeOne.onClick.AddListener(OnExchangeOnePressed);
            exchangeAll.onClick.AddListener(OnExchangeAllPressed);
            exchangeCoinOne.onClick.AddListener(OnExchangeCoinOnePressed);
            exchangeCoinAll.onClick.AddListener(OnExchangeCoinAllPressed);
            for (int i = 0; casinoMultiplierButtons != null && i < casinoMultiplierButtons.Length && i < MainRunGameplaySystems.CasinoMultiplierCount; i++)
            {
                int multiplier = MainRunGameplaySystems.GetCasinoMultiplierAt(i);
                Button multiplierButton = casinoMultiplierButtons[i];
                if (multiplierButton) multiplierButton.onClick.AddListener(() => SelectCasinoMultiplier(multiplier));
            }
            if (casinoBetSlider)
            {
                casinoBetSlider.minValue = 0;
                casinoBetSlider.wholeNumbers = true;
                casinoBetSlider.onValueChanged.AddListener(OnCasinoBetSliderChanged);
            }
            if (casinoActiveButton) casinoActiveButton.onClick.AddListener(StartOrRepairCasino);
            playAgain.onClick.AddListener(() => runLoop.Restart());
            popupOutside.onClick.AddListener(CloseCharacter);
            popupClose.onClick.AddListener(CloseCharacter);
            popupUpgrade.onClick.AddListener(UpgradeSelected);
            for (int i = 0; i < cardActors.Length; i++)
            {
                GridActor actor = cardActors[i]; cardButtons[i].onClick.AddListener(() => ClickCharacterCard(actor));
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
            InitializeCasinoVisuals();
        }

        void OnDestroy()
        {
            DisposeCasinoVisuals();
            if (runLoop) { runLoop.RunRestarted -= HandleRestart; runLoop.RunEnded -= HandleEnded; runLoop.InteractionModalOpen = false; }
            if (screenLayout) screenLayout.FocusActor(null);
        }
        void HandleRestart() { if (optionsPanel) optionsPanel.SetActive(false); shopPanel.SetActive(false); casinoPanelMode = false; ApplyCasinoPanelLayout(false); selectedCasinoBet = 0; selectedCasinoMultiplier = 2; resumeAfterShop = false; resumeAfterOptions = false; DismissCharacter(false); RefreshUI(); }
        void HandleEnded(RunState state) { shopPanel.SetActive(false); casinoPanelMode = false; ApplyCasinoPanelLayout(false); resumeAfterShop = false; DismissCharacter(false); RefreshUI(); }

        public void OpenShop()
        {
            if (ShopOpen || CharacterPopupOpen || debugPanel && debugPanel.IsOpen ||
                runLoop.State == RunState.Defeated || runLoop.State == RunState.RunComplete) return;
            if (OptionsOpen) CloseOptions();
            casinoPanelMode = false;
            ApplyCasinoPanelLayout(false);
            resumeAfterShop = runLoop.State == RunState.Running;
            if (resumeAfterShop) runLoop.Pause();
            runLoop.InteractionModalOpen = true;
            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();
            RefreshUI();
        }

        public void OpenCasinoPanel()
        {
            if (ShopOpen || CharacterPopupOpen || OptionsOpen || debugPanel && debugPanel.IsOpen ||
                runLoop.State == RunState.Defeated || runLoop.State == RunState.RunComplete || !systems.CasinoMachine) return;
            selectedCasinoBet = systems.Chips > 0
                ? (systems.CasinoActive ? Mathf.Min(systems.CasinoBet, systems.Chips) : 1)
                : 0;
            selectedCasinoMultiplier = systems.CasinoActive ? systems.CasinoMultiplier : 2;
            casinoPanelMode = true;
            ApplyCasinoPanelLayout(true);
            resumeAfterShop = runLoop.State == RunState.Running;
            if (resumeAfterShop) runLoop.Pause();
            runLoop.InteractionModalOpen = true;
            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();
            RefreshUI();
        }

        public void CloseShop()
        {
            if (!ShopOpen) return;
            shopPanel.SetActive(false);
            casinoPanelMode = false;
            ApplyCasinoPanelLayout(false);
            runLoop.InteractionModalOpen = false;
            if (resumeAfterShop && runLoop.State == RunState.Paused) runLoop.Resume();
            resumeAfterShop = false;
        }
        void ExchangeChips(int amount)
        {
            systems.TryExchangeChipsForCoins(amount);
            RefreshUI();
        }
        void ExchangeCoins(int amount)
        {
            systems.TryExchangeCoinsForChips(amount);
            RefreshUI();
        }

        void OnExchangeOnePressed()
        {
            if (casinoPanelMode) return;
            ExchangeChips(1);
        }

        void OnExchangeAllPressed()
        {
            if (casinoPanelMode) return;
            ExchangeChips(systems.Chips);
        }

        void OnExchangeCoinOnePressed()
        {
            if (casinoPanelMode) return;
            ExchangeCoins(1);
        }

        void OnExchangeCoinAllPressed()
        {
            if (casinoPanelMode) return;
            ExchangeCoins(systems.Coins);
        }

        void OnCasinoBetSliderChanged(float value)
        {
            if (!casinoPanelMode || updatingCasinoSlider) return;
            selectedCasinoBet = Mathf.Clamp(Mathf.RoundToInt(value), 0, systems.Chips);
            RefreshUI();
        }

        void SelectCasinoMultiplier(int multiplier)
        {
            if (!casinoPanelMode || systems.CasinoFaulted) return;
            selectedCasinoMultiplier = multiplier;
            RefreshUI();
        }

        void StartOrRepairCasino()
        {
            if (systems.CasinoActive)
            {
                if (systems.CasinoFaulted) systems.TryRepairCasino();
                else systems.TryChangeCasinoBet(selectedCasinoBet, selectedCasinoMultiplier);
            }
            else systems.TryActivateCasino(selectedCasinoBet, selectedCasinoMultiplier);
            RefreshUI();
        }

        void ClickCharacterCard(GridActor actor)
        {
            if (ShopOpen || OptionsOpen || CharacterPopupOpen || debugPanel && debugPanel.IsOpen || !actor) return;
            if (actor.Alive) OpenCharacter(actor);
            else { systems.TryReviveTeamMember(actor); RefreshUI(); }
        }

        public void OpenCharacter(GridActor actor)
        {
            if (ShopOpen || OptionsOpen || !actor || !actor.Alive || debugPanel && debugPanel.IsOpen ||
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
            if (ShopOpen) return;
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
            if (ShopOpen || CharacterPopupOpen || OptionsOpen) return;
            if (runLoop.State == RunState.Running) runLoop.Pause();
            else if (runLoop.State == RunState.Paused) runLoop.Resume();
        }
        void Update()
        {
            AdvanceCasinoVisuals();
            if (ShopOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseShop();
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
            SetRegion(teamBarRect, screenLayout.TeamBar);
            SetRegion(boardFrameRect, screenLayout.BoardViewport);
            SetRegion(headerRect, screenLayout.Header); SetRegion(leftRect, screenLayout.LeftColumn);
            SetRegion(rightRect, screenLayout.RightColumn); SetRegion(boardDropRect, screenLayout.BoardViewport);
            SetRegion((RectTransform)resultPanel.transform, screenLayout.BoardViewport);
            Rect view = screenLayout.BoardViewport;
            SetRegion(backgroundRects[0], Rect.MinMaxRect(0, 0, view.xMin, 1));
            SetRegion(backgroundRects[1], Rect.MinMaxRect(view.xMax, 0, 1, 1));
            SetRegion(backgroundRects[2], Rect.MinMaxRect(view.xMin, 0, view.xMax, view.yMin));
            SetRegion(backgroundRects[3], Rect.MinMaxRect(view.xMin, view.yMax, view.xMax, 1));
            int seconds = Mathf.CeilToInt(runLoop.TimeRemainingSeconds);
            runText.text = $"Timer\n{seconds / 60:00}:{seconds % 60:00}";
            gridText.text = $"Grid Scale\n{board.ActiveWidth}x{board.ActiveHeight}";
            walletText.text = $"Coins -> {systems.Coins}\nChips -> {systems.Chips}";
            if (ShopOpen && casinoPanelMode) RefreshCasinoPanel();
            else RefreshExchangePanel();
            shopButton.interactable = !CharacterPopupOpen && runLoop.State != RunState.RunComplete && runLoop.State != RunState.Defeated;
            scoreText.text = $"Score\n{systems.Score}";
            statusText.text = systems.DuckCaptured ? $"Duck kidnapped! Rescue in {systems.DuckRescueRemaining:0}s" : systems.StatusMessage;
            for (int i = 0; i < cardActors.Length; i++)
            {
                var actor = cardActors[i]; bool duck = actor.kind == ActorKind.Duck;
                cardTexts[i].text = !actor.Alive ? $"{actor.kind}\nDied\nRevive: {MainRunGameplaySystems.ReviveCoinCost} Coins" : duck ? $"Duck\n{(systems.DuckCaptured ? "KIDNAPPED" : actor.State.ToString())}" :
                    $"{actor.kind}\nHP {actor.Health:0} ATK {actor.EffectiveDamage:0.0}\n{actor.State}";
                bool inRun = runLoop.State == RunState.Running || runLoop.State == RunState.Paused;
                cardButtons[i].interactable = !ShopOpen && !OptionsOpen && !CharacterPopupOpen && (actor.Alive || inRun && systems.Coins >= MainRunGameplaySystems.ReviveCoinCost);
                if (cardPortraits != null && i < cardPortraits.Length && cardPortraits[i])
                {
                    var sprite = actor.GetComponentInChildren<SpriteRenderer>(true);
                    cardPortraits[i].sprite = sprite ? sprite.sprite : null;
                    cardPortraits[i].color = actor.Alive ? Color.white : new Color(.5f,.5f,.5f);
                }
                var bar = cardBars[i].rectTransform; bar.anchorMax = new Vector2(actor.UltimateCharge, 1);
            }
            var list = new StringBuilder("Stations\n");
            foreach (var station in systems.Stations)
            {
                if (!station) continue;
                if (station == systems.CasinoMachine)
                {
                    string casinoState = !systems.CasinoActive ? "BET TO START" : systems.CasinoFaulted ? "REPAIR NEEDED" :
                        systems.CasinoWorker ? $"BET {systems.CasinoBet} · {systems.CasinoWorker.kind}" : $"BET {systems.CasinoBet} · NO WORKER";
                    list.AppendLine($"CASINO  {station.Progress:P0}  {casinoState}");
                }
                else list.AppendLine($"{station.activityName}  {station.Progress:P0}");
            }
            processText.text = list.ToString();
            if (enemyStatusText) enemyStatusText.text = $"Enemy\n{systems.Enemies.Count}";
            for (int i = 0; i < itemButtons.Length; i++)
            {
                itemTexts[i].text = $"{new[] { "Air Pump", "Laser Gun", "Trampoline" }[i]}  x{systems.GetItemCount((FieldItemKind)i)}";
                ActorKind owner=i==0?ActorKind.Pufferfish:i==1?ActorKind.Cat:ActorKind.Seal;
                itemButtons[i].interactable=systems.CanUseItem((FieldItemKind)i,systems.GetActorForUI(owner));
            }
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
            popupDetails.text += "\n" + systems.GetAttackRangeDescription(selectedActor);
            popupAction.text = isDuck ? $"Free the Duck  ({systems.DuckRansomChips} Chips)" : $"Upgrade  ({price} Coins)";
            popupUpgrade.gameObject.SetActive(!isDuck || systems.DuckCaptured);
            popupUpgrade.interactable = isDuck ? systems.DuckCaptured && systems.Chips >= systems.DuckRansomChips : price < int.MaxValue && systems.Coins >= price;
        }
        public GridActor ActorAtScreenPoint(Vector2 screen)
        {
            if (ShopOpen) return null;
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

        public void OpenBoardTargetAtScreen(Vector2 screenPosition)
        {
            if (ShopOpen || OptionsOpen || CharacterPopupOpen || !stageCamera.pixelRect.Contains(screenPosition)) return;
            ActivityStation casino = systems.CasinoMachine;
            if (casino)
            {
                Vector3 point = stageCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -stageCamera.transform.position.z));
                if (Vector2.Distance(point, casino.transform.position) <= .55f * board.cellSize)
                {
                    if (systems.TryRecoverCasino()) return;
                    OpenCasinoPanel();
                    return;
                }
            }
            OpenCharacter(ActorAtScreenPoint(screenPosition));
        }

        void RefreshExchangePanel()
        {
            shopTitle.text = "EXCHANGE";
            exchangeCoinOneLabel.text = "1 Coin -> 1 Chip";
            exchangeCoinAllLabel.text = "All Coins -> Chips";
            exchangeOneLabel.text = "1 Chip -> 1 Coin";
            exchangeAllLabel.text = "All Chips -> Coins";
            shopBalance.text = $"Coins {systems.Coins}\nChips {systems.Chips}\n1 Chip = 1 Coin";
            exchangeOne.interactable = systems.Chips > 0 && systems.Coins < int.MaxValue;
            exchangeAll.interactable = systems.Chips > 0 && systems.Coins <= int.MaxValue - systems.Chips;
            exchangeCoinOne.interactable = systems.Coins > 0 && systems.Chips < int.MaxValue;
            exchangeCoinAll.interactable = systems.Coins > 0 && systems.Chips <= int.MaxValue - systems.Coins;
        }

        void ApplyCasinoPanelLayout(bool casino)
        {
            if (casinoControlsRoot) casinoControlsRoot.SetActive(casino);
            if (!shopPanelRect) return;
            if (casino)
            {
                SetRegion(shopTitle.rectTransform, Rect.MinMaxRect(.10f, .86f, .90f, .98f));
                shopPanelRect.anchorMin = new Vector2(.06f, .20f);
                shopPanelRect.anchorMax = new Vector2(.94f, .82f);
                shopPanelRect.offsetMin = shopPanelRect.offsetMax = Vector2.zero;
                shopBalanceRect.anchorMin = new Vector2(.08f, .43f);
                shopBalanceRect.anchorMax = new Vector2(.92f, .85f);
                shopBalanceRect.offsetMin = shopBalanceRect.offsetMax = Vector2.zero;
                shopBalance.resizeTextForBestFit = false;
                shopBalance.fontSize = 24;
                if (casinoBetSlider)
                {
                    SetRegion(casinoBetSlider.GetComponent<RectTransform>(), Rect.MinMaxRect(.14f, .385f, .86f, .425f));
                    if (casinoBetSlider.handleRect) casinoBetSlider.handleRect.sizeDelta = new Vector2(20f, 16f);
                }
                if (casinoBetZeroLabel) SetRegion(casinoBetZeroLabel.rectTransform, Rect.MinMaxRect(.07f, .37f, .13f, .44f));
                if (casinoBetMaxLabel) SetRegion(casinoBetMaxLabel.rectTransform, Rect.MinMaxRect(.87f, .37f, .97f, .44f));
                if (casinoActiveButton) SetRegion(casinoActiveButton.GetComponent<RectTransform>(), Rect.MinMaxRect(.42f, .06f, .58f, .16f));

                for (int i = 0; casinoMultiplierButtons != null && i < casinoMultiplierButtons.Length; i++)
                {
                    if (!casinoMultiplierButtons[i]) continue;
                    casinoMultiplierButtons[i].gameObject.SetActive(i < MainRunGameplaySystems.CasinoMultiplierCount);
                    if (i >= MainRunGameplaySystems.CasinoMultiplierCount) continue;
                    RectTransform rect = casinoMultiplierButtons[i].GetComponent<RectTransform>();
                    float center = .5f + (i - (MainRunGameplaySystems.CasinoMultiplierCount - 1) * .5f) * .16f;
                    rect.anchorMin = new Vector2(center - .045f, .21f);
                    rect.anchorMax = new Vector2(center + .045f, .32f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
            }
            else
            {
                SetRegion(shopTitle.rectTransform, Rect.MinMaxRect(shopTitleBaseAnchorMin.x, shopTitleBaseAnchorMin.y, shopTitleBaseAnchorMax.x, shopTitleBaseAnchorMax.y));
                shopBalance.fontSize = shopBalanceBaseFontSize;
                shopBalance.resizeTextForBestFit = shopBalanceBaseBestFit;
                shopPanelRect.anchorMin = shopPanelAnchorMin;
                shopPanelRect.anchorMax = shopPanelAnchorMax;
                shopPanelRect.offsetMin = shopPanelRect.offsetMax = Vector2.zero;
                shopBalanceRect.anchorMin = shopBalanceAnchorMin;
                shopBalanceRect.anchorMax = shopBalanceAnchorMax;
                shopBalanceRect.offsetMin = shopBalanceRect.offsetMax = Vector2.zero;
                Button[] exchangeButtons = { exchangeCoinOne, exchangeCoinAll, exchangeOne, exchangeAll };
                for (int i = 0; i < exchangeButtons.Length; i++)
                {
                    RectTransform rect = exchangeButtons[i].GetComponent<RectTransform>();
                    exchangeButtons[i].gameObject.SetActive(true);
                    rect.anchorMin = exchangeButtonAnchorMins[i];
                    rect.anchorMax = exchangeButtonAnchorMaxes[i];
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                for (int i = 0; i < casinoMultiplierBaseColors.Length; i++)
                {
                    if (casinoMultiplierButtons[i] && casinoMultiplierButtons[i].image)
                        casinoMultiplierButtons[i].image.color = casinoMultiplierBaseColors[i];
                    Text label = casinoMultiplierButtons[i] ? casinoMultiplierButtons[i].GetComponentInChildren<Text>(true) : null;
                    if (label)
                    {
                        label.fontSize = casinoMultiplierBaseFontSizes[i];
                        label.resizeTextForBestFit = casinoMultiplierBaseBestFit[i];
                        label.resizeTextMinSize = casinoMultiplierBaseMinFontSizes[i];
                        label.resizeTextMaxSize = casinoMultiplierBaseMaxFontSizes[i];
                    }
                }
            }
        }

        void RefreshCasinoPanel()
        {
            shopTitle.text = "CASINO";
            ActivityStation machine = systems.CasinoMachine;
            selectedCasinoBet = Mathf.Clamp(selectedCasinoBet, 0, systems.Chips);
            if (casinoBetSlider)
            {
                casinoBetSlider.minValue = 0;
                casinoBetSlider.maxValue = Mathf.Max(0, systems.Chips);
                casinoBetSlider.wholeNumbers = true;
                casinoBetSlider.interactable = !systems.CasinoFaulted;
                updatingCasinoSlider = true;
                casinoBetSlider.SetValueWithoutNotify(selectedCasinoBet);
                updatingCasinoSlider = false;
            }
            if (casinoBetZeroLabel) casinoBetZeroLabel.text = "0";
            if (casinoBetMaxLabel) casinoBetMaxLabel.text = $"{systems.Chips} Max";

            for (int i = 0; casinoMultiplierButtons != null && i < casinoMultiplierButtons.Length; i++)
            {
                Button button = casinoMultiplierButtons[i];
                if (!button) continue;
                if (i >= MainRunGameplaySystems.CasinoMultiplierCount) { button.gameObject.SetActive(false); continue; }
                int multiplier = MainRunGameplaySystems.GetCasinoMultiplierAt(i);
                Text label = button.GetComponentInChildren<Text>(true);
                if (label)
                {
                    label.text = $"x{multiplier}";
                    label.fontSize = 20;
                    label.resizeTextForBestFit = false;
                }
                if (button.image) button.image.color = multiplier == selectedCasinoMultiplier
                    ? new Color(1f, .43f, .08f, 1f)
                    : new Color(1f, .62f, .32f, 1f);
                button.interactable = !systems.CasinoFaulted;
            }

            bool changedBet = systems.CasinoActive &&
                (selectedCasinoBet != systems.CasinoBet || selectedCasinoMultiplier != systems.CasinoMultiplier);
            string status = !systems.CasinoActive ? "CHOOSE BET AND MULTIPLIER" : systems.CasinoLosing ? "LOSING - CLICK MACHINE TO RECOVER" :
                systems.CasinoWaitingForStake ? "WAITING FOR CHIPS" : systems.CasinoWorker ? "DUCK IS PLAYING" : "ACTIVE - SEND DUCK";
            string betLine = changedBet
                ? $"Current {systems.CasinoBet} x{systems.CasinoMultiplier}  |  Next {selectedCasinoBet} x{selectedCasinoMultiplier}"
                : $"Bet {selectedCasinoBet} Chips  x{selectedCasinoMultiplier}";
            float missChance = systems.GetCasinoMissChance(selectedCasinoMultiplier);
            shopBalance.text = $"Coins: {systems.Coins}   Chips: {systems.Chips}\n{betLine}   Loss-state chance: {missChance:P0}\nGain +{systems.CasinoGainPerTick:0.#} / Loss -{systems.CasinoLossPerTick:0.#} Chips per {systems.CasinoTickSeconds:0.#}s\nProgress: {(machine ? machine.Progress : 0f):P0}   {status}\nNet this round: {systems.CasinoRoundNet:+0;-0;0} Chips";

            if (casinoActiveButton)
            {
                Text label = casinoActiveButton.GetComponentInChildren<Text>(true);
                if (label) label.text = !systems.CasinoActive ? "Active" : systems.CasinoFaulted
                    ? $"Repair ({systems.CasinoRepairCoinCost} Coins)"
                    : systems.CasinoWaitingForStake ? "Place Bet / Reset" : "Active / Rebet";
                casinoActiveButton.interactable = systems.CasinoActive
                    ? systems.CasinoFaulted
                        ? systems.Coins >= systems.CasinoRepairCoinCost
                        : selectedCasinoBet > 0 && selectedCasinoBet <= systems.Chips && changedBet
                    : selectedCasinoBet > 0 && selectedCasinoBet <= systems.Chips;
            }
        }
        internal bool TryDropActor(GridActor actor, Vector2 screenPosition)
        {
            if (ShopOpen || OptionsOpen || CharacterPopupOpen || !stageCamera.pixelRect.Contains(screenPosition) || !actor) return false;
            Vector3 world = stageCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -stageCamera.transform.position.z));
            return board.TryWorldToCell(world, out Vector2Int cell) && systems.TryOrderMove(actor, cell);
        }
        internal bool TryDropOnActor(UIResourceDragToken token, GridActor target)
        {
            if (!token || !target || ShopOpen || OptionsOpen || CharacterPopupOpen) return false;
            if (token.Payload == UIDragPayload.Item) return systems.TryUseItem(token.Item, target);
            return token.Payload == UIDragPayload.Actor && systems.TryActivateUltimateSwitch(token.Actor, target);
        }
        internal void NotifyDrop(bool accepted) { if (!accepted) systems.SetStatusFromUI("Cannot move there."); RefreshUI(); }
        static void SetRegion(RectTransform rect, Rect bounds) { rect.anchorMin = bounds.min; rect.anchorMax = bounds.max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
