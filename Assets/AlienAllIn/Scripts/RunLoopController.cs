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
        [SerializeField, Tooltip("Aliens whose elimination ends the run. Can be wired by a future roster/placement system.")]
        GridActor[] survivingAliens = Array.Empty<GridActor>();
        [SerializeField, Tooltip("All stage actors whose positions and health/state must be restored by Restart.")]
        GridActor[] resetActors = Array.Empty<GridActor>();

        [Header("Optional reset targets")]
        [SerializeField] ActivityStation[] activities = Array.Empty<ActivityStation>();
        [SerializeField] FieldPickup[] pickups = Array.Empty<FieldPickup>();

        [Header("Live run state")]
        [SerializeField] RunState state = RunState.Placement;
        [SerializeField, Min(0f)] float elapsedSeconds;

        readonly List<GridActor> survivorRoster = new List<GridActor>();
        readonly List<GridActor> actorsToReset = new List<GridActor>();
        readonly Dictionary<GridActor, Vector3> authoredPositions = new Dictionary<GridActor, Vector3>();
        float timeScaleBeforeFreeze = 1f;
        bool ownsTimeFreeze;
        bool outcomeResolutionQueued;

        public RunState State => state;
        public float ElapsedSeconds => elapsedSeconds;
        public float RunDurationSeconds => stage ? stage.RunDurationSeconds : 0f;
        public float TimeRemainingSeconds => Mathf.Max(0f, RunDurationSeconds - elapsedSeconds);
        public float Progress => RunDurationSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / RunDurationSeconds) : 0f;
        public int LivingAlienCount
        {
            get
            {
                int count = 0;
                foreach (GridActor alien in survivorRoster)
                    if (alien && alien.Alive) count++;
                return count;
            }
        }
        public int RegisteredAlienCount => survivorRoster.Count;
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

                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    if (state == RunState.Placement) StartRun();
                    else if (state == RunState.Running) Pause();
                    else if (state == RunState.Paused) Resume();
                }
            }

            if (state != RunState.Running || !stage) return;

            elapsedSeconds = Mathf.Min(RunDurationSeconds, elapsedSeconds + Time.deltaTime);
            ApplyStageProgress();
        }

        // Resolve after other Update methods have had a chance to report a final-frame elimination.
        // A team wipe is checked before timeout, matching GameplayController's existing tie behavior.
        void LateUpdate()
        {
            if (state != RunState.Running) return;

            if (survivorRoster.Count > 0 && LivingAlienCount == 0)
            {
                Finish(RunState.Defeated);
                return;
            }

            if (elapsedSeconds >= RunDurationSeconds && !outcomeResolutionQueued)
                StartCoroutine(ResolveTimedOutcomeAtFrameEnd());
        }

        IEnumerator ResolveTimedOutcomeAtFrameEnd()
        {
            outcomeResolutionQueued = true;
            yield return new WaitForEndOfFrame();
            outcomeResolutionQueued = false;
            if (state != RunState.Running) yield break;

            // The final survivor may be eliminated by another gameplay system later this frame.
            Finish(survivorRoster.Count > 0 && LivingAlienCount == 0
                ? RunState.Defeated
                : RunState.RunComplete);
        }

        public void StartRun()
        {
            if (state != RunState.Placement || !stage) return;

            elapsedSeconds = 0f;
            ApplyStageProgress();
            state = RunState.Running;

            if (survivorRoster.Count > 0 && LivingAlienCount == 0)
                Finish(RunState.Defeated);
        }

        public void Pause()
        {
            if (state != RunState.Running) return;
            state = RunState.Paused;
            FreezeGameTime();
        }

        public void Resume()
        {
            if (state != RunState.Paused) return;
            RestoreGameTime();
            state = RunState.Running;
        }

        public void Restart()
        {
            RestoreGameTime();
            elapsedSeconds = 0f;
            state = RunState.Placement;
            outcomeResolutionQueued = false;
            ApplyStageProgress();
            ResetStageObjects();
            RunRestarted?.Invoke();
        }

        /// <summary>
        /// Registers an alien for team-wipe detection and restart. The empty Main scene roster
        /// is intentional until the alien placement/roster system is connected.
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
            if (!authoredPositions.ContainsKey(actor)) authoredPositions.Add(actor, actor.transform.position);
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
                if (authoredPositions.TryGetValue(actor, out Vector3 start)) actor.transform.position = start;
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
            const float width = 320f;
            const float height = 132f;
            GUI.Box(new Rect(16f, 16f, width, height), string.Empty);
            GUI.Label(new Rect(28f, 24f, width - 24f, 22f), $"RUN: {state}");
            GUI.Label(new Rect(28f, 48f, width - 24f, 22f), $"TIME: {FormatTime(TimeRemainingSeconds)}");

            string roster = survivorRoster.Count == 0
                ? "ALIENS: roster not connected"
                : $"ALIENS: {LivingAlienCount}/{RegisteredAlienCount} alive";
            GUI.Label(new Rect(28f, 72f, width - 24f, 22f), roster);

            string instruction = state == RunState.Placement ? "Space: Start   |   R: Restart" :
                state == RunState.Running ? "Space: Pause   |   R: Restart" :
                state == RunState.Paused ? "Space: Resume   |   R: Restart" : "R: Restart";
            GUI.Label(new Rect(28f, 96f, 190f, 22f), instruction);

            if (GUI.Button(new Rect(230f, 98f, 92f, 24f), "Restart")) Restart();
        }

        static string FormatTime(float seconds)
        {
            int wholeSeconds = Mathf.CeilToInt(seconds);
            return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
        }
    }
}
