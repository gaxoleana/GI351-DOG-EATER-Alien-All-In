using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlienAllIn
{
    public enum RunState { Placement, Running, Paused, RunComplete, Defeated }

    /// <summary>
    /// Owns the Main scene's run clock and end conditions. StageGridProgression is the
    /// single source for run duration and the prototype grid-expansion milestones.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunLoopController : MonoBehaviour
    {
        [Header("Stage configuration")]
        [SerializeField] StageGridProgression stage;

        [Header("Run participants")]
        [SerializeField, Tooltip("Registered team roster. Only GridActors marked Active Run Member count toward a defeat.")]
        GridActor[] survivingAliens = Array.Empty<GridActor>();
        [SerializeField, Tooltip("All stage actors whose positions and health/state must be restored by Restart.")]
        GridActor[] resetActors = Array.Empty<GridActor>();

        [Header("Optional reset targets")]
        [SerializeField] ActivityStation[] activities = Array.Empty<ActivityStation>();
        [SerializeField] FieldPickup[] pickups = Array.Empty<FieldPickup>();

        [Header("Live run state")]
        [SerializeField] RunState state = RunState.Placement;
        [SerializeField, Min(0f)] float elapsedSeconds;
        [SerializeField, Tooltip("Enable the temporary legacy IMGUI debug panel; MainCoreGameplayUI replaces it in Main.")]
        bool showLegacyDebugHud;

        readonly List<GridActor> survivorRoster = new List<GridActor>();
        readonly List<GridActor> actorsToReset = new List<GridActor>();
        float timeScaleBeforeFreeze = 1f;
        bool ownsTimeFreeze;
        bool outcomeResolutionQueued;
        float debugTimerAdjustmentSeconds;
        public bool DebugFreezeTimer { get; set; }
        public bool InteractionModalOpen { get; set; }
        string placementMessage = string.Empty;

        public RunState State => state;
        public float ElapsedSeconds => elapsedSeconds;
        public float RunDurationSeconds => stage ? stage.RunDurationSeconds : 0f;
        public float EffectiveDurationSeconds => Mathf.Max(0f, RunDurationSeconds + debugTimerAdjustmentSeconds);
        public float TimeRemainingSeconds => Mathf.Max(0f, EffectiveDurationSeconds - elapsedSeconds);
        public float Progress => RunDurationSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / RunDurationSeconds) : 0f;
        public int LivingAlienCount
        {
            get
            {
                int count = 0;
                foreach (GridActor alien in survivorRoster)
                    if (alien && alien.CountsAsActiveRunMember && alien.Alive) count++;
                return count;
            }
        }
        public int RegisteredAlienCount
        {
            get
            {
                int count = 0;
                foreach (GridActor alien in survivorRoster)
                    if (alien && alien.CountsAsActiveRunMember) count++;
                return count;
            }
        }
        public IReadOnlyList<GridActor> Survivors => survivorRoster;
        public bool CanPlaceTeam => state == RunState.Placement;
        public string PlacementMessage => placementMessage;
        /// <summary>Use this scaled delta for future gameplay systems that need an explicit run clock.</summary>
        public float GameplayDeltaTime => state == RunState.Running ? Time.deltaTime : 0f;

        public event Action RunRestarted;
        public event Action<RunState> RunEnded;

        void Awake()
        {
            if (!stage)
                Debug.LogError("RunLoopController requires the scene's StageGridProgression reference.", this);

            foreach (GridActor actor in resetActors) AddResetActor(actor, false);
            foreach (GridActor alien in survivingAliens) AddSurvivor(alien, false);

            elapsedSeconds = 0f;
            state = RunState.Placement;
            ApplyStageProgress();
            ResetStageObjects();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    Restart();
                    return;
                }

                if (!InteractionModalOpen && keyboard.spaceKey.wasPressedThisFrame)
                {
                    if (state == RunState.Placement) StartRun();
                    else if (state == RunState.Running) Pause();
                    else if (state == RunState.Paused) Resume();
                }
            }

            if (state != RunState.Running || !stage) return;

            if (!DebugFreezeTimer) elapsedSeconds = Mathf.Min(EffectiveDurationSeconds, elapsedSeconds + Time.deltaTime);
            ApplyStageProgress();
        }

        // Resolve after other Update methods have had a chance to report a final-frame elimination.
        // A team wipe is checked before timeout, matching GameplayController's existing tie behavior.
        void LateUpdate()
        {
            if (state != RunState.Running) return;

            if (RegisteredAlienCount > 0 && LivingAlienCount == 0)
            {
                Finish(RunState.Defeated);
                return;
            }

            if (TimeRemainingSeconds <= 0f && !outcomeResolutionQueued)
                StartCoroutine(ResolveTimedOutcomeAtFrameEnd());
        }

        IEnumerator ResolveTimedOutcomeAtFrameEnd()
        {
            outcomeResolutionQueued = true;
            yield return new WaitForEndOfFrame();
            outcomeResolutionQueued = false;
            if (state != RunState.Running || TimeRemainingSeconds > 0f) yield break;

            // The final survivor may be eliminated by another gameplay system later this frame.
            Finish(RegisteredAlienCount > 0 && LivingAlienCount == 0
                ? RunState.Defeated
                : RunState.RunComplete);
        }

        public void StartRun()
        {
            if (InteractionModalOpen || state != RunState.Placement || !stage) return;

            if (!TryValidateFormation(out placementMessage)) return;

            // The selected formation becomes the run's saved starting arrangement.
            foreach (GridActor actor in actorsToReset)
                if (actor) actor.CaptureStartingPosition();

            elapsedSeconds = 0f;
            ApplyStageProgress();
            state = RunState.Running;
            placementMessage = string.Empty;

            if (RegisteredAlienCount > 0 && LivingAlienCount == 0)
                Finish(RunState.Defeated);
        }

        bool TryValidateFormation(out string reason)
        {
            if (survivorRoster.Count == 0)
            {
                reason = "Roster missing.";
                return false;
            }

            var occupied = new HashSet<Vector2Int>();
            foreach (GridActor alien in survivorRoster)
            {
                if (!alien || !alien.Alive)
                {
                    reason = "All must be alive.";
                    return false;
                }

                alien.RefreshCell();
                if (!alien.IsOnBoard || !stage.IsInActiveArea(alien.CurrentCell))
                {
                    reason = "Use open cells.";
                    return false;
                }

                if (!occupied.Add(alien.CurrentCell))
                {
                    reason = "No duplicate cells.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public void Pause()
        {
            if (state != RunState.Running) return;
            state = RunState.Paused;
            FreezeGameTime();
        }

        public void Resume()
        {
            if (state != RunState.Paused || InteractionModalOpen) return;
            RestoreGameTime();
            state = RunState.Running;
        }

        public void Restart()
        {
            InteractionModalOpen = false;
            RestoreGameTime();
            DebugFreezeTimer = false;
            debugTimerAdjustmentSeconds = 0f;
            if (stage) stage.ClearDebugSize();
            elapsedSeconds = 0f;
            state = RunState.Placement;
            outcomeResolutionQueued = false;
            placementMessage = string.Empty;
            ApplyStageProgress();
            ResetStageObjects();
            RunRestarted?.Invoke();
        }

        /// <summary>Changes remaining time without rewinding elapsed play, spawns, or board milestones.</summary>
        public bool DebugSetRemainingTime(float seconds)
        {
            if (!stage || float.IsNaN(seconds) || float.IsInfinity(seconds) ||
                state == RunState.RunComplete || state == RunState.Defeated) return false;
            debugTimerAdjustmentSeconds = elapsedSeconds + Mathf.Max(0f, seconds) - RunDurationSeconds;
            if (seconds <= 0f && (state == RunState.Running || state == RunState.Paused))
                Finish(RegisteredAlienCount > 0 && LivingAlienCount == 0 ? RunState.Defeated : RunState.RunComplete);
            return true;
        }

        public bool DebugAddTime(float seconds) => DebugSetRemainingTime(TimeRemainingSeconds + seconds);

        public void ReportDefeat()
        {
            if (state == RunState.Running) Finish(RunState.Defeated);
        }

        /// <summary>
        /// Registers an additional survivor for team-wipe detection and restart.
        /// Duplicate references are ignored; the authored Main roster is loaded in Awake.
        /// </summary>
        public void RegisterSurvivor(GridActor alien)
        {
            AddSurvivor(alien, true);
        }

        void AddSurvivor(GridActor alien, bool resetOnRegister)
        {
            if (!alien || survivorRoster.Contains(alien)) return;
            survivorRoster.Add(alien);
            AddResetActor(alien, resetOnRegister);
        }

        void AddResetActor(GridActor actor, bool resetOnRegister)
        {
            if (!actor) return;
            if (!actorsToReset.Contains(actor)) actorsToReset.Add(actor);
            if (!actor.HasStartingPosition) actor.CaptureStartingPosition();
            if (resetOnRegister) actor.ResetActor();
        }

        void ApplyStageProgress()
        {
            if (stage) stage.ApplyRunProgress(Progress);
        }

        void ResetStageObjects()
        {
            foreach (GridActor actor in actorsToReset)
            {
                if (!actor) continue;
                actor.RestoreStartingPosition();
                actor.ResetActor();
            }

            foreach (ActivityStation activity in activities)
                if (activity) activity.ResetActivity();
            foreach (FieldPickup pickup in pickups)
                if (pickup) pickup.ResetPickup();
        }

        void Finish(RunState result)
        {
            state = result;
            FreezeGameTime();
            RunEnded?.Invoke(result);
        }

        void FreezeGameTime()
        {
            if (ownsTimeFreeze) return;
            timeScaleBeforeFreeze = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            ownsTimeFreeze = true;
        }

        void RestoreGameTime()
        {
            if (!ownsTimeFreeze) return;
            Time.timeScale = timeScaleBeforeFreeze;
            ownsTimeFreeze = false;
        }

        void OnDisable() => RestoreGameTime();
        void OnDestroy() => RestoreGameTime();

        void OnGUI()
        {
            if (!showLegacyDebugHud) return;
            const float width = 154f;
            const float height = 140f;
            GUI.Box(new Rect(16f, 16f, width, height), string.Empty);
            GUI.Label(new Rect(24f, 24f, width - 16f, 22f), $"RUN: {state}");
            GUI.Label(new Rect(24f, 48f, width - 16f, 22f), $"TIME: {FormatTime(TimeRemainingSeconds)}");

            string roster = survivorRoster.Count == 0
                ? "TEAM: 0/0"
                : $"TEAM: {LivingAlienCount}/{RegisteredAlienCount}";
            GUI.Label(new Rect(24f, 72f, width - 16f, 22f), roster);
            string controls = state == RunState.Placement ? "Space: Start" :
                state == RunState.Running ? "Space: Pause" :
                state == RunState.Paused ? "Space: Resume" : "R: Restart";
            GUI.Label(new Rect(24f, 94f, width - 16f, 18f),
                string.IsNullOrEmpty(placementMessage) ? controls : placementMessage);

            if (GUI.Button(new Rect(24f, 112f, width - 16f, 22f), "Restart (R)")) Restart();
        }

        static string FormatTime(float seconds)
        {
            int wholeSeconds = Mathf.CeilToInt(seconds);
            return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
        }
    }
}
