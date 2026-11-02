using UnityEngine;

namespace AlienAllIn
{
    public sealed partial class MainRunGameplaySystems
    {
        [Header("Pufferfish bounce presentation")]
        [SerializeField, Min(0)] float pufferSwayDegrees = 22f;
        [SerializeField, Min(.01f)] float pufferSwayCyclesPerSecond = .25f;
        [SerializeField] AudioClip pufferCollisionClip;
        [SerializeField, Range(0, 1)] float pufferCollisionVolume = .4f;
        [SerializeField, Min(0)] float pufferCollisionClipOffset;
        [SerializeField, Min(.05f)] float pufferCollisionSoundSeconds = .4f;
        Transform pufferVisual;
        Quaternion pufferVisualBaseRotation;
        float pufferSwayClock, pufferSoundRemaining, pufferSoundCooldown;
        AudioSource pufferCollisionAudio;
        bool pufferAudioPaused;

        void ResetPufferPresentation()
        {
            if (pufferVisual) pufferVisual.localRotation = pufferVisualBaseRotation;
            pufferSwayClock = pufferSoundRemaining = pufferSoundCooldown = 0;
            if (pufferCollisionAudio) pufferCollisionAudio.Stop();
            pufferAudioPaused = false;
        }

        void PlayPufferCollisionSound()
        {
            if (!pufferCollisionClip || !runLoop || runLoop.State != RunState.Running || pufferSoundCooldown > 0) return;
            if (!pufferCollisionAudio)
            {
                var go = new GameObject("Puffer collision audio");
                go.transform.SetParent(transform, false);
                pufferCollisionAudio = go.AddComponent<AudioSource>();
                pufferCollisionAudio.playOnAwake = false;
                pufferCollisionAudio.loop = false;
                pufferCollisionAudio.spatialBlend = 1;
                pufferCollisionAudio.rolloffMode = AudioRolloffMode.Linear;
                pufferCollisionAudio.minDistance = 8f;
                pufferCollisionAudio.maxDistance = 30f;
                pufferCollisionAudio.dopplerLevel = 0;
                pufferCollisionAudio.spread = 25f;
            }
            pufferCollisionAudio.transform.position = pufferfish.transform.position;
            pufferCollisionAudio.Stop();
            pufferCollisionAudio.clip = pufferCollisionClip;
            pufferCollisionAudio.volume = pufferCollisionVolume;
            pufferCollisionAudio.time = Mathf.Clamp(pufferCollisionClipOffset, 0, Mathf.Max(0, pufferCollisionClip.length - .05f));
            pufferCollisionAudio.Play();
            pufferSoundRemaining = Mathf.Max(.05f, pufferCollisionSoundSeconds);
            pufferSoundCooldown = .08f;
            pufferAudioPaused = false;
        }

        void LateUpdate()
        {
            if (!runLoop) return;
            if (!pufferfish || !pufferfish.Alive || runLoop.State == RunState.Placement ||
                runLoop.State == RunState.RunComplete || runLoop.State == RunState.Defeated)
            {
                ResetPufferPresentation();
                return;
            }
            if (runLoop.State == RunState.Paused)
            {
                if (pufferCollisionAudio && pufferCollisionAudio.isPlaying) { pufferCollisionAudio.Pause(); pufferAudioPaused = true; }
                return;
            }
            float dt = runLoop.GameplayDeltaTime;
            if (pufferCollisionAudio) pufferCollisionAudio.transform.position = pufferfish.transform.position;
            if (pufferAudioPaused && pufferCollisionAudio) { pufferCollisionAudio.UnPause(); pufferAudioPaused = false; }
            pufferSoundCooldown = Mathf.Max(0, pufferSoundCooldown - dt);
            pufferSoundRemaining = Mathf.Max(0, pufferSoundRemaining - dt);
            if (pufferCollisionAudio && pufferSoundRemaining <= 0) pufferCollisionAudio.Stop();
            if (!pufferVisual)
            {
                // Main's sprite is on the actor root; legacy prefabs may use an art child.
                pufferVisual = pufferfish.art ? pufferfish.art : pufferfish.transform;
                pufferVisualBaseRotation = pufferVisual.localRotation;
            }
            bool bouncing = !manualDestinations.ContainsKey(pufferfish) &&
                (PufferPhase == PufferAttackPhase.Launched || PufferPhase == PufferAttackPhase.Bouncing);
            if (!pufferVisual) return;
            if (bouncing)
            {
                pufferSwayClock += dt;
                float angle = Mathf.Sin(pufferSwayClock * pufferSwayCyclesPerSecond * Mathf.PI * 2) * pufferSwayDegrees;
                pufferVisual.localRotation = pufferVisualBaseRotation * Quaternion.Euler(0, 0, angle);
            }
            else
            {
                pufferSwayClock = 0;
                pufferVisual.localRotation = pufferVisualBaseRotation;
            }
        }
    }
}
