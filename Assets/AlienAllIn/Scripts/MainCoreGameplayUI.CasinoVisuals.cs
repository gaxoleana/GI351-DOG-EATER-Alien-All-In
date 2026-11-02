using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace AlienAllIn
{
    public sealed partial class MainCoreGameplayUI
    {
        [Header("Casino visual prototype")]
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
        sealed class CasinoTickVisual { public Text label; public float age; public Vector2 origin; }

        void InitializeCasinoVisuals()
        {
            var go = new GameObject("Casino floating Chips", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(hudCanvas.transform, false);
            casinoChipBadge = go.GetComponent<RectTransform>();
            casinoChipBadge.anchorMin = casinoChipBadge.anchorMax = new Vector2(.5f, .5f);
            casinoChipBadge.sizeDelta = new Vector2(270, 86);
            var background = go.GetComponent<Image>();
            background.color = Color.clear;
            background.raycastTarget = false;
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(go.transform, false);
            casinoFloatingText = label.GetComponent<Text>();
            casinoFloatingText.font = walletText.font;
            casinoFloatingText.fontSize = 30;
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
            casinoFloatingText.rectTransform.offsetMin = new Vector2(8, 4);
            casinoFloatingText.rectTransform.offsetMax = new Vector2(-8, -4);
            go.SetActive(false);
            systems.CasinoRoundResolved += ShowCasinoResult;
            systems.CasinoChipTick += ShowCasinoChipTick;
            runLoop.RunRestarted += ResetCasinoVisuals;
            CreateCasinoThought();
        }

        void DisposeCasinoVisuals()
        {
            if (systems) systems.CasinoRoundResolved -= ShowCasinoResult;
            if (systems) systems.CasinoChipTick -= ShowCasinoChipTick;
            if (runLoop) runLoop.RunRestarted -= ResetCasinoVisuals;
            RestoreCasinoArtwork();
            if (casinoChipBadge) Destroy(casinoChipBadge.gameObject);
            if (casinoThought) Destroy(casinoThought.gameObject);
            foreach (var tick in casinoTickVisuals) if (tick.label) Destroy(tick.label.gameObject);
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
        }

        void ShowCasinoResult(long amount, int multiplier)
        {
            visualResult = amount;
            visualMultiplier = multiplier;
            showingCasinoResult = true;
            flightStarted = false;
            resultClock = 0;
        }

        Vector2 CasinoLabelPosition(ActivityStation machine)
        {
            Vector3 screen = stageCamera.WorldToScreenPoint(machine.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hudCanvas.transform, screen,
                hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera, out Vector2 local);
            local += new Vector2(90, 90);
            Rect canvas = ((RectTransform)hudCanvas.transform).rect;
            Rect boardView = screenLayout.BoardViewport;
            local.x = Mathf.Clamp(local.x, canvas.xMin + boardView.xMin * canvas.width + 140, canvas.xMin + boardView.xMax * canvas.width - 140);
            local.y = Mathf.Clamp(local.y, canvas.yMin + boardView.yMin * canvas.height + 48, canvas.yMin + boardView.yMax * canvas.height - 48);
            return local;
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
            ActivityStation machine = systems.CasinoMachine;
            if (!machine) { ResetCasinoVisuals(); return; }
            float dt = runLoop.GameplayDeltaTime;
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
                float t = tick.age / 1.2f;
                if (t >= 1) { Destroy(tick.label.gameObject); casinoTickVisuals.RemoveAt(i); continue; }
                tick.label.rectTransform.anchoredPosition = tick.origin + Vector2.up * (t * 32);
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
                casinoFloatingText.text = $"<size=15>NET RESULT</size>\n{(visualResult < 0 ? "-" : "+")}{CompactChips(displayed)} Chips (x{visualMultiplier})";
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
            text.font = walletText.font; text.fontSize = 23; text.fontStyle = FontStyle.Bold;
            text.text = $"{(amount < 0 ? "-" : "+")}{CompactChips(Math.Abs(amount))} Chips";
            text.raycastTarget = false; text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.15f, 1, .25f, 0);
            go.GetComponent<Outline>().effectColor = new Color(0, .12f, .02f, .9f);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f,.5f);
            text.rectTransform.sizeDelta = new Vector2(200, 40);
            Vector2 origin = CasinoLabelPosition(systems.CasinoMachine) + new Vector2(0, -35);
            text.rectTransform.anchoredPosition = origin;
            casinoTickVisuals.Add(new CasinoTickVisual { label = text, origin = origin });
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
            casinoThought.anchoredPosition=CasinoLabelPosition(machine)+new Vector2(-135,-15);
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
