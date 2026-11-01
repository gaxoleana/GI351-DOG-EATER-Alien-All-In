using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AlienAllIn
{
    /// <summary>Basic runtime uGUI for placement, run control, resources, upgrades, shop and drop targets.</summary>
    [DisallowMultipleComponent]
    public sealed class MainCoreGameplayUI : MonoBehaviour
    {
        [SerializeField] RunLoopController runLoop;
        [SerializeField] MainRunGameplaySystems systems;
        [SerializeField] BoardGrid board;
        [SerializeField] Camera stageCamera;

        readonly List<Text> teamCardTexts = new List<Text>();
        readonly List<Image> teamCardBars = new List<Image>();
        readonly List<Text> processTexts = new List<Text>();
        readonly Dictionary<ActorKind, Text> chipBuyTexts = new Dictionary<ActorKind, Text>();
        readonly Dictionary<UpgradeKind, Text> upgradeTexts = new Dictionary<UpgradeKind, Text>();
        Text runText;
        Text cashText;
        Text scoreText;
        Text statusText;
        Text itemText;
        Text shopText;
        GameObject shopPanel;
        GridActor selectedActor;
        TeamPlacementController placement;
        float refreshClock;

        void Awake()
        {
            ResolveReferences();
            if (runLoop) runLoop.RunRestarted += HandleRunRestarted;
            EnsureEventSystem();
            BuildUI();
        }

        void OnDestroy()
        {
            if (runLoop) runLoop.RunRestarted -= HandleRunRestarted;
        }

        void HandleRunRestarted()
        {
            selectedActor = null;
            RefreshUI();
        }

        void ResolveReferences()
        {
            if (!systems) systems = FindAnyObjectByType<MainRunGameplaySystems>();
            if (!runLoop) runLoop = systems ? systems.RunLoop : FindAnyObjectByType<RunLoopController>();
            if (!board) board = systems ? systems.Board : FindAnyObjectByType<BoardGrid>();
            if (!stageCamera) stageCamera = Camera.main ? Camera.main : FindAnyObjectByType<Camera>();
            if (!placement) placement = FindAnyObjectByType<TeamPlacementController>();
        }

        void EnsureEventSystem()
        {
            EventSystem current = EventSystem.current;
            if (!current)
            {
                GameObject eventSystem = new GameObject("Runtime Event System");
                current = eventSystem.AddComponent<EventSystem>();
            }
            if (!current.GetComponent<BaseInputModule>())
            {
                InputSystemUIInputModule input = current.gameObject.AddComponent<InputSystemUIInputModule>();
                input.AssignDefaultActions();
            }
        }

        void BuildUI()
        {
            GameObject canvasObject = new GameObject("Main Core Gameplay UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;

            GameObject boardDrop = CreateImage("Board Drop Surface", canvasObject.transform, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            boardDrop.AddComponent<BoardDropSurface>().Configure(this);
            boardDrop.GetComponent<Image>().raycastTarget = true;

            GameObject top = CreateImage("Run HUD", canvasObject.transform, new Color(.045f, .065f, .09f, .94f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, 112));
            runText = CreateText("Run State", top.transform, new Vector2(24, -18), new Vector2(750, 28), 22, TextAnchor.MiddleLeft);
            cashText = CreateText("Cash", top.transform, new Vector2(24, -52), new Vector2(350, 25), 18, TextAnchor.MiddleLeft);
            scoreText = CreateText("Score", top.transform, new Vector2(390, -52), new Vector2(340, 25), 18, TextAnchor.MiddleLeft);
            statusText = CreateText("Status", top.transform, new Vector2(24, -82), new Vector2(740, 22), 15, TextAnchor.MiddleLeft);

            CreateButton("Start Run", top.transform, new Vector2(800, -54), new Vector2(116, 34), () => runLoop.StartRun(), new Color(.16f, .43f, .28f));
            CreateButton("Pause / Resume", top.transform, new Vector2(930, -54), new Vector2(140, 34), TogglePause, new Color(.22f, .32f, .47f));
            CreateButton("Restart (R)", top.transform, new Vector2(1085, -54), new Vector2(120, 34), () => runLoop.Restart(), new Color(.44f, .23f, .24f));
            CreateButton("Shop", top.transform, new Vector2(1220, -54), new Vector2(90, 34), () => systems.ToggleShop(), new Color(.42f, .34f, .17f));

            GameObject processPanel = CreateImage("Process Status", canvasObject.transform, new Color(.045f, .065f, .09f, .9f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(290, 350));
            processPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-18, -130);
            CreateText("Process Header", processPanel.transform, new Vector2(12, -10), new Vector2(260, 25), 17, TextAnchor.MiddleLeft).text = "PROCESS / ENEMIES";
            for (int i = 0; i < 13; i++) processTexts.Add(CreateText("Process Row " + i, processPanel.transform, new Vector2(12, -40 - i * 23), new Vector2(266, 21), 14, TextAnchor.MiddleLeft));

            GameObject itemPanel = CreateImage("Items", canvasObject.transform, new Color(.045f, .065f, .09f, .9f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(220, 148));
            itemPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(18, -130);
            CreateText("Items Header", itemPanel.transform, new Vector2(10, -8), new Vector2(195, 24), 16, TextAnchor.MiddleLeft).text = "ITEMS (drag to Alien)";
            CreateItemToken(itemPanel.transform, FieldItemKind.AirPump, "Air Pump", new Vector2(10, -40));
            CreateItemToken(itemPanel.transform, FieldItemKind.LaserGun, "Laser Gun", new Vector2(10, -72));
            CreateItemToken(itemPanel.transform, FieldItemKind.Trampoline, "Trampoline", new Vector2(10, -104));

            GameObject upgradePanel = CreateImage("Cash Upgrades", canvasObject.transform, new Color(.045f, .065f, .09f, .9f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(330, 130));
            upgradePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(18, -290);
            CreateText("Upgrade Header", upgradePanel.transform, new Vector2(10, -8), new Vector2(305, 22), 16, TextAnchor.MiddleLeft).text = "CASH UPGRADE (select Alien)";
            upgradeTexts[UpgradeKind.Damage] = CreateButton("Damage Upgrade", upgradePanel.transform, new Vector2(10, -40), new Vector2(300, 24), () => BuySelectedUpgrade(UpgradeKind.Damage), new Color(.16f, .25f, .34f), 13).GetComponentInChildren<Text>();
            upgradeTexts[UpgradeKind.CastSpeed] = CreateButton("Cast Speed Upgrade", upgradePanel.transform, new Vector2(10, -68), new Vector2(300, 24), () => BuySelectedUpgrade(UpgradeKind.CastSpeed), new Color(.16f, .25f, .34f), 13).GetComponentInChildren<Text>();
            upgradeTexts[UpgradeKind.Health] = CreateButton("HP Upgrade", upgradePanel.transform, new Vector2(10, -96), new Vector2(300, 24), () => BuySelectedUpgrade(UpgradeKind.Health), new Color(.16f, .25f, .34f), 13).GetComponentInChildren<Text>();

            BuildTeamCards(canvasObject.transform);
            BuildShop(canvasObject.transform);
            RefreshUI();
        }

        void BuildTeamCards(Transform parent)
        {
            GameObject row = CreateImage("Team Cards", parent, new Color(0, 0, 0, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 20));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(1280, 192);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;

            foreach (ActorKind kind in new[] { ActorKind.Seal, ActorKind.Cat, ActorKind.Pufferfish, ActorKind.Duck })
            {
                GridActor actor = systems.GetActorForUI(kind);
                GameObject card = CreateImage(kind + " Card", row.transform, new Color(.06f, .09f, .13f, .96f), Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero);
                card.AddComponent<Button>().onClick.AddListener(() => SelectActor(actor));
                card.AddComponent<AlienDropTarget>().Configure(this, actor);
                UIResourceDragToken drag = card.AddComponent<UIResourceDragToken>();
                drag.SetActor(actor);
                Text info = CreateText(kind + " Info", card.transform, new Vector2(8, -8), new Vector2(270, 115), 15, TextAnchor.UpperLeft);
                Image bar = CreateImage(kind + " Gauge", card.transform, new Color(.3f, .82f, .55f, 1), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 8)).GetComponent<Image>();
                bar.rectTransform.anchoredPosition = new Vector2(0, 8);
                card.AddComponent<LayoutElement>().preferredWidth = 310;
                teamCardTexts.Add(info); teamCardBars.Add(bar);
            }
        }

        void BuildShop(Transform parent)
        {
            shopPanel = CreateImage("Chip Shop", parent, new Color(.055f, .08f, .11f, .97f), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(310, 248));
            shopPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-18, -55);
            shopText = CreateText("Shop Header", shopPanel.transform, new Vector2(12, -10), new Vector2(280, 45), 16, TextAnchor.UpperLeft);
            CreateChipShopRow(shopPanel.transform, ActorKind.Seal, new Vector2(12, -68));
            CreateChipShopRow(shopPanel.transform, ActorKind.Cat, new Vector2(12, -114));
            CreateChipShopRow(shopPanel.transform, ActorKind.Pufferfish, new Vector2(12, -160));
            CreateText("Shop Note", shopPanel.transform, new Vector2(12, -208), new Vector2(280, 32), 12, TextAnchor.UpperLeft).text = "Drag a Chip token onto the matching team card.";
            shopPanel.SetActive(false);
        }

        void CreateChipShopRow(Transform parent, ActorKind kind, Vector2 position)
        {
            GameObject buy = CreateButton("Buy " + kind + " Chip", parent, position, new Vector2(142, 32), () => systems.TryBuyChip(kind), new Color(.2f, .32f, .42f), 13);
            chipBuyTexts[kind] = buy.GetComponentInChildren<Text>();
            GameObject token = CreateButton("Chip Token " + kind, parent, position + new Vector2(150, 0), new Vector2(124, 32), () => { }, new Color(.37f, .25f, .44f), 13);
            token.AddComponent<UIResourceDragToken>().SetChip(kind);
        }

        void CreateItemToken(Transform parent, FieldItemKind kind, string label, Vector2 position)
        {
            GameObject token = CreateButton("Item Token " + kind, parent, position, new Vector2(195, 26), () => UseItemOnSpecies(kind), new Color(.18f, .25f, .31f), 13);
            token.AddComponent<UIResourceDragToken>().SetItem(kind);
            token.GetComponentInChildren<Text>(true).text = label;
        }

        void UseItemOnSpecies(FieldItemKind kind)
        {
            ActorKind actorKind = kind == FieldItemKind.AirPump ? ActorKind.Pufferfish : kind == FieldItemKind.LaserGun ? ActorKind.Cat : ActorKind.Seal;
            systems.TryUseItem(kind, systems.GetActorForUI(actorKind));
            RefreshUI();
        }

        void BuySelectedUpgrade(UpgradeKind kind)
        {
            if (!selectedActor) { systems.SetStatusFromUI("Select a living Alien before buying an upgrade."); return; }
            systems.TryBuyUpgrade(selectedActor, kind);
            RefreshUI();
        }

        void SelectActor(GridActor actor)
        {
            selectedActor = actor;
            TeamPlacementController placement = FindAnyObjectByType<TeamPlacementController>();
            if (placement && runLoop && runLoop.State == RunState.Placement) placement.SelectActorFromUI(actor);
        }

        void TogglePause()
        {
            if (runLoop.State == RunState.Running) runLoop.Pause();
            else if (runLoop.State == RunState.Paused) runLoop.Resume();
        }

        void Update()
        {
            refreshClock -= Time.unscaledDeltaTime;
            if (refreshClock > 0f) return;
            refreshClock = .1f;
            RefreshUI();
        }

        void RefreshUI()
        {
            if (!runLoop || !systems) return;
            runText.text = $"RUN: {runLoop.State}    TIME: {FormatTime(runLoop.TimeRemainingSeconds)}    TEAM: {runLoop.LivingAlienCount}/{runLoop.RegisteredAlienCount}";
            cashText.text = $"Cash: {systems.Cash}    Shop restock: {systems.ShopRestockSecondsRemaining}s";
            scoreText.text = $"Score: {systems.Score}    Processed: {systems.ProcessedStations}";
            statusText.text = runLoop.CanPlaceTeam
                ? !string.IsNullOrEmpty(runLoop.PlacementMessage) ? runLoop.PlacementMessage : placement ? placement.CurrentMessage : systems.StatusMessage
                : systems.StatusMessage;
            shopPanel.SetActive(systems.ShopOpen);
            if (shopText) shopText.text = $"CHIP SHOP  |  Stock refills in {systems.ShopRestockSecondsRemaining}s";
            if (selectedActor)
            {
                foreach (UpgradeKind upgrade in new[] { UpgradeKind.Damage, UpgradeKind.CastSpeed, UpgradeKind.Health })
                {
                    if (!upgradeTexts.TryGetValue(upgrade, out Text text) || !text) continue;
                    int cost = systems.GetUpgradeCost(selectedActor, upgrade);
                    text.text = cost == int.MaxValue ? $"{upgrade}  MAX" : $"{upgrade}  -  {cost} Cash";
                }
            }
            foreach (ActorKind kind in new[] { ActorKind.Seal, ActorKind.Cat, ActorKind.Pufferfish })
                if (chipBuyTexts.TryGetValue(kind, out Text buyText) && buyText)
                    buyText.text = $"{kind} Chip  {systems.GetChipStock(kind)}/{systems.ChipStockCapacity}  {systems.GetChipPrice(kind)}c";

            int index = 0;
            foreach (ActorKind kind in new[] { ActorKind.Seal, ActorKind.Cat, ActorKind.Pufferfish, ActorKind.Duck })
            {
                GridActor actor = systems.GetActorForUI(kind);
                if (index < teamCardTexts.Count && actor)
                {
                    string status = actor.State.ToString();
                    teamCardTexts[index].text = $"{(selectedActor == actor ? "> " : "")}{kind}\nHP {Mathf.CeilToInt(actor.Health)}/{Mathf.CeilToInt(actor.EffectiveMaxHealth)}\nUltimate {Mathf.RoundToInt(actor.UltimateCharge * 100)}%\nState {status}\n{(kind == ActorKind.Duck ? "No Attack" : $"Damage {actor.EffectiveDamage:0.#}")}";
                    RectTransform bar = teamCardBars[index].rectTransform;
                    bar.sizeDelta = new Vector2(280 * Mathf.Clamp01(actor.UltimateCharge), 8);
                    teamCardTexts[index].color = actor.Alive ? Color.white : new Color(1, .4f, .4f);
                }
                index++;
            }

            string[] rows = new string[processTexts.Count];
            int row = 0;
            foreach (ActivityStation station in systems.Stations)
            {
                if (row >= rows.Length) break;
                if (!station) continue;
                rows[row++] = $"{station.activityName}: {station.Progress * 100:0}%{(station.IsInterrupted ? " PAUSED" : "")}";
            }
            foreach (GridActor enemy in systems.Enemies)
            {
                if (row >= rows.Length) break;
                if (!enemy || !enemy.Alive) continue;
                rows[row++] = $"{enemy.name}: HP {Mathf.CeilToInt(enemy.Health)}";
            }
            for (int i = 0; i < processTexts.Count; i++) processTexts[i].text = i < row ? rows[i] : string.Empty;
            SetTokenLabel(FieldItemKind.AirPump, $"Air Pump  x{systems.GetItemCount(FieldItemKind.AirPump)}");
            SetTokenLabel(FieldItemKind.LaserGun, $"Laser Gun  x{systems.GetItemCount(FieldItemKind.LaserGun)}");
            SetTokenLabel(FieldItemKind.Trampoline, $"Trampoline  x{systems.GetItemCount(FieldItemKind.Trampoline)}");
            SetChipLabel(ActorKind.Seal); SetChipLabel(ActorKind.Cat); SetChipLabel(ActorKind.Pufferfish);
        }

        void SetTokenLabel(FieldItemKind kind, string value)
        {
            UIResourceDragToken token = GetComponentsInChildren<UIResourceDragToken>(true).FindToken(kind);
            if (token) token.SetLabel(value, systems.GetItemCount(kind) > 0);
        }

        void SetChipLabel(ActorKind kind)
        {
            UIResourceDragToken token = GetComponentsInChildren<UIResourceDragToken>(true).FindToken(kind);
            if (token) token.SetLabel($"Drag x{systems.GetChipCount(kind)}", systems.GetChipCount(kind) > 0);
        }

        internal bool TryDropActor(GridActor actor, Vector2 screenPosition)
        {
            if (!stageCamera || !board || !actor) return false;
            Vector3 world = stageCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -stageCamera.transform.position.z));
            if (!board.TryWorldToCell(world, out Vector2Int cell)) return false;
            if (systems.TryUltimateSwitchAt(actor, cell)) return true;
            return systems.TryMoveActor(actor, cell);
        }

        internal bool TryDropOnActor(UIResourceDragToken token, GridActor target)
        {
            if (!token || !target || !systems) return false;
            switch (token.Payload)
            {
                case UIDragPayload.Item: return systems.TryUseItem(token.Item, target);
                case UIDragPayload.Chip: return systems.TryApplyChip(token.ChipOwner, target);
                case UIDragPayload.Actor:
                    if (token.Actor == target) return false;
                    if (systems.TryActivateUltimateSwitch(token.Actor, target)) return true;
                    return false;
                default: return false;
            }
        }

        internal void NotifyDrop(bool accepted)
        {
            systems.SetStatusFromUI(accepted ? "Drop accepted." : "Drop rejected by current run rules.");
            RefreshUI();
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }

        static GameObject CreateImage(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; rect.sizeDelta = size;
            Image image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = color.a > 0f;
            return go;
        }

        static Text CreateText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            Text text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.color = Color.white; text.alignment = alignment; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; text.raycastTarget = false;
            return text;
        }

        static GameObject CreateButton(string label, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Color color, int fontSize = 15)
        {
            GameObject go = CreateImage(label, parent, color, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), size);
            go.GetComponent<RectTransform>().anchoredPosition = position;
            Button button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(action);
            Text text = CreateText(label + " Label", go.transform, Vector2.zero, size, fontSize, TextAnchor.MiddleCenter); text.raycastTarget = false; text.text = label;
            return go;
        }

    }

    static class ResourceTokenSearchExtensions
    {
        public static UIResourceDragToken FindToken(this UIResourceDragToken[] tokens, FieldItemKind kind)
        { foreach (UIResourceDragToken token in tokens) if (token && token.Payload == UIDragPayload.Item && token.Item == kind) return token; return null; }
        public static UIResourceDragToken FindToken(this UIResourceDragToken[] tokens, ActorKind kind)
        { foreach (UIResourceDragToken token in tokens) if (token && token.Payload == UIDragPayload.Chip && token.ChipOwner == kind) return token; return null; }
    }
}
