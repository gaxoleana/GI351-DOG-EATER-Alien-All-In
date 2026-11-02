using UnityEngine;

namespace AlienAllIn
{
    public sealed partial class MainRunGameplaySystems
    {
        [Header("Prototype permanent Casino")]
        [SerializeField] ActivityStation casinoMachine;
        [SerializeField, Range(0f, 1f), Tooltip("Prototype miss chance by multiplier, ordered x2, x4, x8. Legacy extra entries are ignored.")]
        float[] casinoMissChanceByMultiplier = { .05f, .10f, .20f };
        [SerializeField, Min(.1f), Tooltip("Prototype working time between Casino malfunction rolls.")]
        float casinoFaultCheckIntervalSeconds = 20f;
        [SerializeField, Range(0f, 1f), Tooltip("Prototype malfunction chance at each fault check.")]
        float casinoFaultChancePerCheck = .2f;
        [SerializeField, Min(1)] int casinoRepairCoinCost = 20;
        [SerializeField, Min(.1f)] float casinoChipTickSeconds = 1f;
        [SerializeField, Min(.1f)] float casinoLossCheckSeconds = 3f;
        [SerializeField, Min(.01f)] float casinoGainPerBetChip = 1f;
        bool casinoLosing;
        float casinoTickClock, casinoLossClock;
        long casinoRoundNet;
        public bool CasinoLosing => casinoLosing;
        public long CasinoRoundNet => casinoRoundNet;
        public double CasinoGainPerTick => CasinoBet * (double)casinoGainPerBetChip * CasinoMultiplier;
        public double CasinoLossPerTick => CasinoGainPerTick;
        public float CasinoTickSeconds => Mathf.Max(.1f, casinoChipTickSeconds);
        public event System.Action<long> CasinoChipTick;

        GridActor casinoWorker;
        Vector3 casinoStartingPosition;
        float casinoFaultClock;
        bool casinoPositionCaptured;
        bool casinoActive;
        bool casinoFaulted;
        bool casinoWaitingForStake;
        string casinoLastResult = "READY";
        static readonly int[] casinoMultiplierOptions = { 2, 4, 8 };

        public ActivityStation CasinoMachine => casinoMachine;
        public GridActor CasinoWorker => casinoWorker;
        public int CasinoBet { get; private set; }
        public int CasinoMultiplier { get; private set; } = 2;
        public int CasinoRepairCoinCost => casinoRepairCoinCost;
        public bool CasinoActive => casinoActive;
        public bool CasinoFaulted => casinoFaulted;
        public bool CasinoWaitingForStake => casinoWaitingForStake;
        public string CasinoLastResult => casinoLastResult;
        public event System.Action<long, int> CasinoRoundResolved;
        public static int CasinoMultiplierCount => casinoMultiplierOptions.Length;
        public static int GetCasinoMultiplierAt(int index) => casinoMultiplierOptions[Mathf.Clamp(index, 0, casinoMultiplierOptions.Length - 1)];

        public float GetCasinoMissChance(int multiplier)
        {
            for (int i = 0; i < casinoMultiplierOptions.Length; i++)
                if (casinoMultiplierOptions[i] == multiplier)
                    return casinoMissChanceByMultiplier != null && i < casinoMissChanceByMultiplier.Length
                        ? Mathf.Clamp01(casinoMissChanceByMultiplier[i])
                        : Mathf.Lerp(.05f, .20f, i / (float)(casinoMultiplierOptions.Length - 1));
            return 1f;
        }

        void InitializePersistentCasino()
        {
            if (!casinoMachine)
            {
                RefreshBoardOccupancy();
                if (!TryChooseOpenCell(false, out Vector2Int spawnCell))
                {
                    Debug.LogError("MainRunGameplaySystems could not find an open cell for the permanent Casino.", this);
                    return;
                }

                ActivityPrototypeSettings settings = null;
                if (activitySettings != null)
                    foreach (ActivityPrototypeSettings candidate in activitySettings)
                        if (candidate != null && candidate.kind == ActivityKind.Casino) { settings = candidate; break; }

                ActivityStation prefab = settings != null && settings.prefab ? settings.prefab : casinoPrefab;
                casinoMachine = Instantiate(prefab, board.CellCenter(spawnCell), Quaternion.identity, processContainer);
                casinoMachine.name = "Permanent Casino";
            }

            ActivityPrototypeSettings casinoSettings = null;
            if (activitySettings != null)
                foreach (ActivityPrototypeSettings candidate in activitySettings)
                    if (candidate != null && candidate.kind == ActivityKind.Casino) { casinoSettings = candidate; break; }

            casinoMachine.activityKind = ActivityKind.Casino;
            casinoMachine.activityName = "CASINO";
            casinoMachine.requiredKind = ActorKind.Duck;
            casinoMachine.board = board;
            casinoMachine.allowUniversalDuckWorker = false;
            casinoMachine.SetContinuousProgress(true);
            if (casinoSettings != null)
            {
                casinoMachine.duration = Mathf.Max(1f, casinoSettings.durationSeconds);
                casinoMachine.cashTickSeconds = Mathf.Max(.05f, casinoSettings.cashTickSeconds);
                casinoMachine.scoreTickSeconds = Mathf.Max(0f, casinoSettings.processScoreTickSeconds);
                casinoMachine.workColor = casinoSettings.markerColor;
                if (casinoMachine.marker) casinoMachine.marker.color = casinoSettings.markerColor;
            }
            casinoMachine.ResetActivity();
            casinoStartingPosition = casinoMachine.transform.position;
            casinoPositionCaptured = true;
            if (!stations.Contains(casinoMachine)) stations.Add(casinoMachine);
            ResetCasinoForNewRun();
            statusMessage = "Casino ready. Bet Chips to activate it.";
            RunDataChanged?.Invoke();
        }

        void ResetStationsForNewRun()
        {
            foreach (ActivityStation station in stations)
            {
                if (!station || station == casinoMachine) continue;
                if (Application.isPlaying) Destroy(station.gameObject);
                else DestroyImmediate(station.gameObject);
            }
            stations.Clear();
            if (casinoMachine) stations.Add(casinoMachine);
        }

        void ClearTransientStations()
        {
            foreach (ActivityStation station in stations)
            {
                if (!station || station == casinoMachine) continue;
                if (Application.isPlaying) Destroy(station.gameObject);
                else DestroyImmediate(station.gameObject);
            }
            stations.Clear();
            if (casinoMachine) stations.Add(casinoMachine);
        }

        void ResetCasinoForNewRun()
        {
            if (casinoWorker && casinoWorker.State == ActorState.Working) casinoWorker.State = ActorState.Idle;
            casinoActive = false;
            casinoFaulted = false;
            CasinoBet = 0;
            CasinoMultiplier = 2;
            casinoWorker = null;
            casinoFaultClock = 0f;
            casinoWaitingForStake = false;
            casinoLastResult = "READY";
            ResetCasinoTickState(0);
            if (!casinoMachine) return;
            if (casinoPositionCaptured) casinoMachine.transform.position = casinoStartingPosition;
            casinoMachine.SetContinuousProgress(true);
            casinoMachine.ClearInterruption();
            casinoMachine.ResetActivity();
        }

        public bool IsCasinoCell(Vector2Int cell) => casinoMachine && casinoMachine.Cell == cell;

        public bool TryActivateCasino(int betAmount, int multiplier)
        {
            if (!casinoMachine || casinoActive || !runLoop ||
                runLoop.State == RunState.RunComplete || runLoop.State == RunState.Defeated) return false;
            if (betAmount < 1 || betAmount > chips)
            {
                SetStatusFromUI("Choose a Casino bet between 1 Chip and your available Chips.");
                return false;
            }
            if (!IsCasinoMultiplier(multiplier)) return false;

            chips -= betAmount;
            ResetCasinoTickState(-betAmount);
            CasinoBet = betAmount;
            CasinoMultiplier = multiplier;
            casinoActive = true;
            casinoFaulted = false;
            casinoWorker = null;
            casinoFaultClock = 0f;
            casinoWaitingForStake = false;
            casinoLastResult = "BET PLACED - SEND DUCK";
            casinoMachine.ResetActivity();
            SetStatusFromUI($"Casino active: {betAmount} Chips on x{multiplier}. Only Duck can play.");
            return true;
        }

        public bool TryChangeCasinoBet(int betAmount, int multiplier)
        {
            if (!casinoMachine || !casinoActive || casinoFaulted || !runLoop ||
                runLoop.State == RunState.RunComplete || runLoop.State == RunState.Defeated) return false;
            if (betAmount < 1 || betAmount > chips)
            {
                SetStatusFromUI("Choose a new Casino bet between 1 Chip and your available Chips.");
                return false;
            }
            if (!IsCasinoMultiplier(multiplier)) return false;
            if (betAmount == CasinoBet && multiplier == CasinoMultiplier)
            {
                SetStatusFromUI("Choose a different bet or multiplier to reset Progress.");
                return false;
            }

            chips -= betAmount;
            ResetCasinoTickState(-betAmount);
            CasinoBet = betAmount;
            CasinoMultiplier = multiplier;
            casinoFaultClock = 0f;
            casinoWaitingForStake = false;
            casinoLastResult = $"NEW BET PLACED - {betAmount} Chips on x{multiplier}";
            casinoMachine.ResetActivity();
            SetStatusFromUI($"Casino bet changed to {betAmount} Chips on x{multiplier}. Progress reset to 0%.");
            return true;
        }

        public bool TryRepairCasino()
        {
            if (!casinoMachine || !casinoActive || !casinoFaulted || !runLoop ||
                runLoop.State == RunState.RunComplete || runLoop.State == RunState.Defeated) return false;
            if (coins < casinoRepairCoinCost)
            {
                SetStatusFromUI($"Casino repair needs {casinoRepairCoinCost} Coins.");
                return false;
            }

            coins -= casinoRepairCoinCost;
            casinoFaulted = false;
            casinoFaultClock = 0f;
            casinoMachine.ClearInterruption();
            SetStatusFromUI("Casino repaired. Work can continue.");
            return true;
        }

        void AdvanceCasino(float dt)
        {
            if (!casinoMachine || !casinoActive || casinoFaulted) return;
            if (casinoWorker && (!casinoWorker.Alive || casinoWorker == duck && DuckCaptured)) casinoWorker = null;
            if (casinoWorker && casinoWorker.State == ActorState.Panic) return;

            float workRate = GetProcessingRate(casinoWorker);
            bool roundComplete = casinoMachine.Progress + dt * Mathf.Max(.01f, workRate) /
                Mathf.Max(.01f, casinoMachine.duration) >= 1f;
            bool worked = casinoMachine.AdvanceContinuous(dt, casinoWorker, workRate);
            if (!worked) return;

            if (casinoWorker == duck) duck.AddUltimateCharge(duckProcessChargePerSecond * dt);
            casinoLossClock += dt;
            while (casinoLossClock >= Mathf.Max(.1f, casinoLossCheckSeconds))
            {
                casinoLossClock -= Mathf.Max(.1f, casinoLossCheckSeconds);
                if (!casinoLosing && UnityEngine.Random.value < GetCasinoMissChance(CasinoMultiplier)) casinoLosing = true;
            }
            casinoTickClock += dt;
            while (casinoTickClock >= Mathf.Max(.1f, casinoChipTickSeconds))
            {
                casinoTickClock -= Mathf.Max(.1f, casinoChipTickSeconds);
                double amount = CasinoGainPerTick;
                long requested = (long)System.Math.Min(int.MaxValue, System.Math.Max(1, System.Math.Round(amount)));
                long next = casinoLosing ? System.Math.Max(0, chips - requested) : System.Math.Min(int.MaxValue, chips + requested);
                long actual = next - chips;
                chips = (int)next;
                casinoRoundNet += actual;
                CasinoChipTick?.Invoke(actual);
                RunDataChanged?.Invoke();
            }
            if (roundComplete) ResolveCasinoRound();
        }

        void ResolveCasinoRound()
        {
            long result = casinoRoundNet;
            casinoLastResult = $"NET {result:+0;-0;0} CHIPS (x{CasinoMultiplier})";
            statusMessage = casinoLastResult;
            casinoMachine.ResetActivity();
            casinoRoundNet = 0;
            RunDataChanged?.Invoke();
            CasinoRoundResolved?.Invoke(result, CasinoMultiplier);
        }

        void ResetCasinoTickState(long initialNet)
        {
            casinoLosing = false;
            casinoTickClock = casinoLossClock = 0;
            casinoRoundNet = initialNet;
        }

        public bool TryRecoverCasino()
        {
            if (!casinoActive || !casinoLosing || !runLoop || runLoop.State != RunState.Running) return false;
            casinoLosing = false;
            casinoLossClock = casinoTickClock = 0;
            SetStatusFromUI("Casino recovered. Chips are increasing again.");
            return true;
        }

        static bool IsCasinoMultiplier(int multiplier)
        {
            foreach (int option in casinoMultiplierOptions)
                if (option == multiplier) return true;
            return false;
        }
    }
}
