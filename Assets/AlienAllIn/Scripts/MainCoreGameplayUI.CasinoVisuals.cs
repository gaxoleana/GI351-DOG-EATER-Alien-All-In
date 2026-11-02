using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace AlienAllIn
{
    public sealed partial class MainCoreGameplayUI
    {
        const int DamageFloatingFontSize = 28;
        const int CasinoTickFontSize = 15;
        const int CasinoResultFontSize = 20;

        [Header("Casino visual prototype")]
        [SerializeField, Range(.5f, 1.25f), Tooltip("Size of the slot-machine status bubble, including its symbol and dots.")]
        float casinoStatusVisualScale = .8f;
        [SerializeField, Min(.1f)] float casinoBounceFrequency = 2.5f;
        [SerializeField, Range(0f, .3f)] float casinoBounceAmount = .10f;
        [SerializeField, Min(.1f)] float casinoResultCountSeconds = .65f;
        [SerializeField, Min(.1f)] float casinoResultHoldSeconds = .65f;
        [SerializeField, Min(.1f)] float casinoChipFlightSeconds = .7f;
        Text casinoFloatingText;
        RectTransform casinoChipBadge;
        Transform bouncingArtwork;
        Vector3 artworkBasePosition, artworkBaseScale;
        float bounceClock, resultClock;
        long visualResult;
        int visualMultiplier;
        bool showingCasinoResult;
        Vector2 flightStart;
        bool flightStarted;
        RectTransform casinoThought;
        Text casinoThoughtSymbol;
        Texture2D thoughtTexture;
        Sprite thoughtSprite;
        readonly System.Collections.Generic.List<CasinoTickVisual> casinoTickVisuals = new System.Collections.Generic.List<CasinoTickVisual>();
        readonly System.Collections.Generic.List<DamageFloatingVisual> damageFloatingVisuals = new System.Collections.Generic.List<DamageFloatingVisual>();
        sealed class CasinoTickVisual { public Text label; public float age; public Vector3 worldOrigin; }
        sealed class DamageFloatingVisual { public Text label; public float age; public GridActor actor; public Vector3 lastWorldPosition; }

        void InitializeCasinoVisuals()
        {
            var go = new GameObject("Casino floating Chips", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(hudCanvas.transform, false);
            casinoChipBadge = go.GetComponent<RectTransform>();
            casinoChipBadge.anchorMin = casinoChipBadge.anchorMax = new Vector2(.5f, .5f);
            casinoChipBadge.sizeDelta = new Vector2(210, 64);
            var background = go.GetComponent<Image>();
            background.color = Color.clear;
            background.raycastTarget = false;
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(go.transform, false);
            casinoFloatingText = label.GetComponent<Text>();
            casinoFloatingText.font = walletText.font;
            casinoFloatingText.fontSize = CasinoResultFontSize;
            casinoFloatingText.fontStyle = FontStyle.Bold;
            casinoFloatingText.color = new Color(.15f, .9f, .25f);
            casinoFloatingText.alignment = TextAnchor.MiddleCenter;
            casinoFloatingText.horizontalOverflow = HorizontalWrapMode.Overflow;
            casinoFloatingText.verticalOverflow = VerticalWrapMode.Overflow;
            casinoFloatingText.raycastTarget = false;
            var outline = label.AddComponent<Outline>();
            outline.effectColor = new Color(0, .12f, .02f, .9f);
            outline.effectDistance = new Vector2(1, -1);
            casinoFloatingText.supportRichText = true;
            casinoFloatingText.rectTransform.anchorMin = Vector2.zero;
            casinoFloatingText.rectTransform.anchorMax = Vector2.one;
            casinoFloatingText.rectTransform.offsetMin = new Vector2(6, 2);
            casinoFloatingText.rectTransform.offsetMax = new Vector2(-6, -2);
            go.SetActive(false);
            systems.CasinoRoundResolved += ShowCasinoResult;
            systems.CasinoChipTick += ShowCasinoChipTick;
            GridActor.Damaged += ShowActorDamagePopup;
            runLoop.RunRestarted += ResetCasinoVisuals;
            CreateCasinoThought();
        }

        void DisposeCasinoVisuals()
        {
            if (systems) systems.CasinoRoundResolved -= ShowCasinoResult;
            if (systems) systems.CasinoChipTick -= ShowCasinoChipTick;
            GridActor.Damaged -= ShowActorDamagePopup;
            if (runLoop) runLoop.RunRestarted -= ResetCasinoVisuals;
            RestoreCasinoArtwork();
            if (casinoChipBadge) Destroy(casinoChipBadge.gameObject);
            if (casinoThought) Destroy(casinoThought.gameObject);
            foreach (var tick in casinoTickVisuals) if (tick.label) Destroy(tick.label.gameObject);
            casinoTickVisuals.Clear();
            foreach (var popup in damageFloatingVisuals) if (popup.label) Destroy(popup.label.gameObject);
            damageFloatingVisuals.Clear();
            if (thoughtSprite) Destroy(thoughtSprite);
            if (thoughtTexture) Destroy(thoughtTexture);
        }

        void RestoreCasinoArtwork()
        {
            if (!bouncingArtwork) return;
            bouncingArtwork.localPosition = artworkBasePosition;
            bouncingArtwork.localScale = artworkBaseScale;
        }

        void ResetCasinoVisuals()
        {
            RestoreCasinoArtwork();
            bouncingArtwork = null;
            showingCasinoResult = flightStarted = false;
            bounceClock = resultClock = 0;
            if (casinoChipBadge) casinoChipBadge.gameObject.SetActive(false);
            if (casinoThought) casinoThought.gameObject.SetActive(false);
            foreach (var tick in casinoTickVisuals) if (tick.label) Destroy(tick.label.gameObject);
            casinoTickVisuals.Clear();
            foreach (var popup in damageFloatingVisuals) if (popup.label) Destroy(popup.label.gameObject);
            damageFloatingVisuals.Clear();
        }

        void ShowCasinoResult(long amount, int multiplier)
        {
            visualResult = amount;
            visualMultiplier = multiplier;
            showingCasinoResult = true;
            flightStarted = false;
            resultClock = 0;
        }

        Vector2 WorldToHudPosition(Vector3 worldPosition, float clampMarginX = 36f, float clampMarginY = 24f)
        {
            Vector3 screen = stageCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hudCanvas.transform, screen,
                hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera, out Vector2 local);
            Rect canvas = ((RectTransform)hudCanvas.transform).rect;
            Rect boardView = screenLayout.BoardViewport;
            local.x = Mathf.Clamp(local.x, canvas.xMin + boardView.xMin * canvas.width + clampMarginX, canvas.xMin + boardView.xMax * canvas.width - clampMarginX);
            local.y = Mathf.Clamp(local.y, canvas.yMin + boardView.yMin * canvas.height + clampMarginY, canvas.yMin + boardView.yMax * canvas.height - clampMarginY);
            return local;
        }

        Vector3 TopRightWorldPosition(Vector3 centerWorldPosition, float cellFractionX = .42f, float cellFractionY = .42f)
        {
            float cell = board ? board.cellSize : 1f;
            return centerWorldPosition + new Vector3(cell * cellFractionX, cell * cellFractionY, 0f);
        }

        Vector2 CasinoLabelPosition(ActivityStation machine)
        {
            return WorldToHudPosition(TopRightWorldPosition(machine.transform.position, .45f, .45f), 42f, 24f);
        }

        Vector2 WalletLabelPosition()
        {
            RectTransform rect = walletText.rectTransform;
            // Land on the Chips (second) line, rather than the Coins line.
            Vector3 world = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.yMin + rect.rect.height * .25f, 0));
            return ((RectTransform)hudCanvas.transform).InverseTransformPoint(world);
        }

        void AdvanceCasinoVisuals()
        {
            if (!casinoFloatingText || !systems || !runLoop) return;
            float dt = runLoop.GameplayDeltaTime;
            AdvanceDamageFloatingVisuals(dt);
            ActivityStation machine = systems.CasinoMachine;
            if (!machine) { ResetCasinoVisuals(); return; }
            Transform art = machine.artwork ? machine.artwork.transform : null;
            if (art && art != bouncingArtwork)
            {
                RestoreCasinoArtwork();
                bouncingArtwork = art;
                artworkBasePosition = art.localPosition;
                artworkBaseScale = art.localScale;
            }
            bool working = systems.CasinoActive && !systems.CasinoFaulted && !systems.CasinoWaitingForStake &&
                machine.CurrentWorker && machine.CurrentWorker.Alive && machine.CurrentWorker.State == ActorState.Working && !machine.IsInterrupted;
            UpdateCasinoThought(machine, working);
            for (int i = casinoTickVisuals.Count - 1; i >= 0; i--)
            {
                var tick = casinoTickVisuals[i];
                tick.age += dt;
                float t = tick.age / 1.1f;
                if (t >= 1 || !tick.label) { if (tick.label) Destroy(tick.label.gameObject); casinoTickVisuals.RemoveAt(i); continue; }
                Vector3 basePos = systems.CasinoMachine ? TopRightWorldPosition(systems.CasinoMachine.transform.position, .44f, .44f) : tick.worldOrigin;
                Vector2 origin = WorldToHudPosition(basePos, 28f, 18f);
                tick.label.rectTransform.anchoredPosition = origin + Vector2.up * (t * 24f);
                Color tickColor = tick.label.text.StartsWith("-", StringComparison.Ordinal)
                    ? new Color(1f, .15f, .12f) : new Color(.15f, 1f, .25f);
                tickColor.a = Mathf.Min(t / .12f, (1 - t) / .45f);
                tick.label.color = tickColor;
            }
            if (dt > 0)
            {
                if (working && !systems.CasinoLosing)
                {
                    bounceClock += dt;
                    float wave = Mathf.Sin(bounceClock * casinoBounceFrequency * Mathf.PI * 2);
                    if (bouncingArtwork)
                    {
                        bouncingArtwork.localPosition = artworkBasePosition + Vector3.up * (Mathf.Max(0, wave) * casinoBounceAmount * board.cellSize);
                        bouncingArtwork.localScale = Vector3.Scale(artworkBaseScale, new Vector3(1 - wave * casinoBounceAmount, 1 + wave * casinoBounceAmount, 1));
                    }
                }
                else RestoreCasinoArtwork();
            }
            if (showingCasinoResult)
            {
                resultClock += dt;
                float countDuration = Mathf.Max(.1f, casinoResultCountSeconds);
                float holdEnd = countDuration + Mathf.Max(1.4f, casinoResultHoldSeconds);
                float end = holdEnd + Mathf.Max(.1f, casinoChipFlightSeconds);
                float t = Mathf.Clamp01(resultClock / countDuration);
                long displayed = (long)Math.Round(Math.Abs((double)visualResult) * (1 - Math.Pow(1 - t, 3)));
                casinoFloatingText.text = $"<size=11>NET RESULT</size>\n{(visualResult < 0 ? "-" : "+")}{CompactChips(displayed)} Chips (x{visualMultiplier})";
                casinoFloatingText.color = visualResult < 0 ? new Color(1f, .15f, .12f) : new Color(.15f, 1f, .25f);
                Vector2 position = CasinoLabelPosition(machine);
                if (resultClock >= holdEnd && visualResult > 0)
                {
                    if (!flightStarted) { flightStart = position; flightStarted = true; }
                    float flight = Mathf.SmoothStep(0, 1, (resultClock - holdEnd) / Mathf.Max(.1f, casinoChipFlightSeconds));
                    position = Vector2.Lerp(flightStart, WalletLabelPosition(), flight);
                    position.y += Mathf.Sin(flight * Mathf.PI) * 45;
                }
                casinoChipBadge.anchoredPosition = position;
                casinoChipBadge.gameObject.SetActive(true);
                if (resultClock >= end) { showingCasinoResult = false; casinoChipBadge.gameObject.SetActive(false); }
            }
            else
            {
                casinoChipBadge.gameObject.SetActive(false);
            }
        }

        void ShowCasinoChipTick(long amount)
        {
            if (!systems.CasinoMachine || amount == 0) return;
            var go = new GameObject("Casino Chip tick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            go.transform.SetParent(hudCanvas.transform, false);
            Text text = go.GetComponent<Text>();
            text.font = walletText.font; text.fontSize = CasinoTickFontSize; text.fontStyle = FontStyle.Bold;
            text.text = $"{(amount < 0 ? "-" : "+")}{CompactChips(Math.Abs(amount))} Chips";
            text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(.15f, 1, .25f, 0);
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0, .12f, .02f, .9f);
            outline.effectDistance = new Vector2(1, -1);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
            text.rectTransform.pivot = new Vector2(0f, .5f);
            text.rectTransform.sizeDelta = new Vector2(140, 28);
            Vector3 worldOrigin = TopRightWorldPosition(systems.CasinoMachine.transform.position, .44f, .44f);
            text.rectTransform.anchoredPosition = WorldToHudPosition(worldOrigin, 28f, 18f);
            casinoTickVisuals.Add(new CasinoTickVisual { label = text, worldOrigin = worldOrigin });
        }

        void ShowActorDamagePopup(GridActor actor, Vector3 worldPosition, float dealt)
        {
            if (!hudCanvas || !stageCamera || !screenLayout || !actor || dealt <= 0f) return;
            var go = new GameObject("Damage floating text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            go.transform.SetParent(hudCanvas.transform, false);
            Text text = go.GetComponent<Text>();
            text.font = walletText.font;
            text.fontSize = DamageFloatingFontSize;
            text.fontStyle = FontStyle.Bold;
            text.text = $"-{FormatDamage(dealt)}";
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(1f, .12f, .12f, 1f);
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(.15f, 0f, 0f, .95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
            text.rectTransform.pivot = new Vector2(0f, .5f);
            text.rectTransform.sizeDelta = new Vector2(120, 40);
            Vector3 cornerWorld = TopRightWorldPosition(worldPosition, .38f, .38f);
            text.rectTransform.anchoredPosition = WorldToHudPosition(cornerWorld, 20f, 16f);
            damageFloatingVisuals.Add(new DamageFloatingVisual
            {
                label = text,
                actor = actor,
                lastWorldPosition = worldPosition
            });
        }

        void AdvanceDamageFloatingVisuals(float dt)
        {
            for (int i = damageFloatingVisuals.Count - 1; i >= 0; i--)
            {
                var popup = damageFloatingVisuals[i];
                popup.age += dt;
                float t = popup.age / .8f;
                if (t >= 1f || !popup.label)
                {
                    if (popup.label) Destroy(popup.label.gameObject);
                    damageFloatingVisuals.RemoveAt(i);
                    continue;
                }
                if (popup.actor && popup.actor.Alive && popup.actor.gameObject.activeInHierarchy)
                    popup.lastWorldPosition = popup.actor.transform.position;
                Vector3 cornerWorld = TopRightWorldPosition(popup.lastWorldPosition, .38f, .38f);
                Vector2 baseHud = WorldToHudPosition(cornerWorld, 20f, 16f);
                popup.label.rectTransform.anchoredPosition = baseHud + new Vector2(t * 6f, t * 28f);
                float popScale = t < .18f ? Mathf.Lerp(1.25f, 1f, t / .18f) : 1f;
                popup.label.rectTransform.localScale = Vector3.one * popScale;
                Color color = new Color(1f, .12f, .12f, Mathf.Clamp01((1f - t) / .35f));
                popup.label.color = color;
            }
        }

        static string FormatDamage(float amount)
        {
            float rounded = Mathf.Round(amount * 10f) / 10f;
            return Mathf.Abs(rounded - Mathf.Round(rounded)) < .05f
                ? Mathf.RoundToInt(rounded).ToString(CultureInfo.InvariantCulture)
                : rounded.ToString("0.#", CultureInfo.InvariantCulture);
        }

        void CreateCasinoThought()
        {
            thoughtTexture = new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++) for(int x=0;x<32;x++) thoughtTexture.SetPixel(x,y, Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f)) < 15 ? Color.white : Color.clear);
            thoughtTexture.Apply();
            thoughtSprite = Sprite.Create(thoughtTexture,new Rect(0,0,32,32),new Vector2(.5f,.5f));
            var go = new GameObject("Casino thought bubble",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            go.transform.SetParent(hudCanvas.transform,false);
            casinoThought=go.GetComponent<RectTransform>(); casinoThought.anchorMin=casinoThought.anchorMax=new Vector2(.5f,.5f); casinoThought.sizeDelta=new Vector2(54,42);
            casinoThought.localScale=Vector3.one*casinoStatusVisualScale;
            go.GetComponent<Image>().sprite=thoughtSprite; go.GetComponent<Image>().raycastTarget=false;
            for(int i=0;i<2;i++)
            {
                var dot=new GameObject("Thought dot",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); dot.transform.SetParent(go.transform,false);
                dot.GetComponent<Image>().sprite=thoughtSprite; dot.GetComponent<Image>().raycastTarget=false;
                dot.GetComponent<RectTransform>().sizeDelta=Vector2.one*(i==0?10:6);
                dot.GetComponent<RectTransform>().anchoredPosition=new Vector2(12+i*7,-24-i*7);
            }
            var symbol=new GameObject("State",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)); symbol.transform.SetParent(go.transform,false);
            casinoThoughtSymbol=symbol.GetComponent<Text>(); casinoThoughtSymbol.font=walletText.font; casinoThoughtSymbol.fontSize=30; casinoThoughtSymbol.alignment=TextAnchor.MiddleCenter; casinoThoughtSymbol.raycastTarget=false;
            SetRegion(casinoThoughtSymbol.rectTransform,new Rect(0,0,1,1));
            go.SetActive(false);
        }

        void UpdateCasinoThought(ActivityStation machine,bool working)
        {
            if(!casinoThought) return;
            casinoThought.gameObject.SetActive(working);
            casinoThoughtSymbol.text=systems.CasinoLosing ? "\u2715" : "\u2713";
            casinoThoughtSymbol.color=systems.CasinoLosing ? new Color(.85f,.1f,.1f) : new Color(.05f,.55f,.15f);
            Vector3 topLeft = machine.transform.position + new Vector3(-(board ? board.cellSize : 1f) * .42f, (board ? board.cellSize : 1f) * .45f, 0f);
            casinoThought.anchoredPosition=WorldToHudPosition(topLeft, 32f, 24f);
        }

        static string CompactChips(long amount)
        {
            double value = amount;
            string suffix = "";
            if (amount >= 1000000000) { value /= 1000000000; suffix = "B"; }
            else if (amount >= 1000000) { value /= 1000000; suffix = "M"; }
            else if (amount >= 1000) { value /= 1000; suffix = "K"; }
            return value.ToString(suffix.Length == 0 ? "0" : "0.#", CultureInfo.InvariantCulture) + suffix;
        }
    }
}
