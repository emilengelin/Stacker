﻿using Stacker.Rounds;
using Stacker.Templates.Rounds;
using Stacker.UIControllers;
using System.Collections;
using System.Linq;
using UnityEngine;

#pragma warning disable 0649

namespace Stacker.Controllers
{

    class RoundController : Controller<RoundController>
    {

        #region Editor

        [SerializeField] private RoundTemplate[] roundTemplates;

        [Header("Audio")]
        [SerializeField] private AudioSource buildPhaseTickAudioSource;
        [SerializeField] private AudioClip   buildPhaseTickSoundEffect;

        #endregion

        #region Private variables

        private Round currentRound;

        private bool roundHasEnded;

        private int roundsPassedWithoutLoss;

        #endregion

        #region Public properties

        public Round CurrentRound
        {
            get
            {
                return currentRound;
            }
        }

        /// <summary>
        /// A percentage value (0...1) that indicates how far the build phase has come.
        /// </summary>
        public float BuildPhaseProgress { get; private set; }

        #endregion

        #region Round cycle

        /// <summary>
        /// Creates a new round from a random round template.
        /// </summary>
        public void CreateNewRound()
        {
            BorderController.HideBorder();
            RoundCleanController.Singleton.CleanRound();

            StopCoroutine("BuildCycle");
            StopCoroutine("ActionPhase");
            roundHasEnded = false;

            currentRound = new Round(ChooseRoundTemplate());
            
            ChallengesController.ResetChallengeValues();

            UIChallengesController.Singleton.InitializeUIChallenges(currentRound.RoundChallenges);
        }

        /// <summary>
        /// Starts the round, allowing the player to build etc.
        /// </summary>
        public void BeginRound()
        {
            ProjectileController.Singleton.ClearProjectiles();
            VehicleController.Singleton.ClearVehicles();
            RoundCleanController.Singleton.ResetRound();

            ChallengesController.ResetChallengeValues();

            ReadyActionControllers();

            RoundSurpriseController.Singleton.ResetRoundSurprise();

            StartCoroutine("BuildCycle");
        }

        private void BeginBuildPhase()
        {
            UIPhaseController.Singleton.BeginPhases(); // Set the current UI phase to build.

            CameraController.Singleton.CanReadInput = true;
            BorderController.SetupBorder();
            BuildController.Singleton.BeginBuildPhase(currentRound.Template.RoundBuildingBlockTemplates);
        }

        private void EndBuildPhase()
        {
            UIStackHeightController.Singleton.ActivateUIHeightMeter(false);
            ChallengesController.CheckSkyscraperChallenges();

            // Check if the player even has placed blocks:
            // Otherwise, end the round prematurely.
            if (BuildController.NumberOfPlacedBuildingBlockCopies > 0)
            {
                StartCoroutine("ActionPhase");
            }
            else
            {
                EndRound();
            }
        }

        private IEnumerator BuildCycle()
        {
            yield return StartCoroutine(RoundSurpriseController.Singleton.AwaitBeforeBuildPhase());

            BeginBuildPhase();

            float time = currentRound.TimeRestraint;
            float soundEffectCooldown = GetBuildPhaseTickSoundEffectCooldown(time, currentRound.TimeRestraint);

            // Keep the round going as long as we're using a time restraint and time > 0 OR we're not using a time restraint (aka forever).
            while (time > 0)
            {
                if (currentRound.UseTimeRestraint)
                {
                    time -= Time.deltaTime;
                    soundEffectCooldown -= Time.deltaTime;

                    if (soundEffectCooldown <= 0f)
                    {
                        buildPhaseTickAudioSource.PlayOneShot(buildPhaseTickSoundEffect, AudioController.MiscVolume * 0.1f);
                        soundEffectCooldown = GetBuildPhaseTickSoundEffectCooldown(time, currentRound.TimeRestraint);
                    }
                }

                BuildPhaseProgress = 1 - time / currentRound.TimeRestraint;

                yield return new WaitForEndOfFrame();
            }

            // In case anything updated roundHasEnded between frames because of framelag.
            if (!roundHasEnded)
            {
                BuildController.Singleton.EndBuildPhase(); // Disable the ability to place blocks.

                if (BuildController.NumberOfPlacedBuildingBlockCopies > 0)
                {
                    yield return StartCoroutine(RoundSurpriseController.Singleton.AwaitAfterBuildPhase());
                }

                EndBuildPhase();
            }
        }

        private IEnumerator ActionPhase()
        {
            UIPhaseController.Singleton.NextPhase(); // Set the current UI phase to fortress.
            yield return StartCoroutine(FortressPhase());
            ChallengesController.CheckFortressChallenges();

            UIPhaseController.Singleton.NextPhase(); // Set the current UI phase to tunnel.
            yield return StartCoroutine(TunnelPhase());
            ChallengesController.CheckTunnelChallenges();
            
            EndRound();
        }

        public void EndRound()
        {
            StopCoroutine("BuildCycle");

            UIPhaseController.Singleton.EndPhases();

            CameraController.Singleton.CanReadInput = false;
            
            roundHasEnded = true;

            CheckRoundWinLossState();
        }

        /// <summary>
        /// Check if the round was a win or a loss.
        /// </summary>
        private void CheckRoundWinLossState()
        {
            // Count stars:
            int starsReceived = currentRound.RoundStarsReward();

            // The round was lost because the player did not gain any stars:
            if (starsReceived > 0 && BuildController.NumberOfPlacedBuildingBlockCopies > 0)
            {
                roundsPassedWithoutLoss++;

                UIRoundController.Singleton.WonRoundWindow();
            }
            else
            {
                roundsPassedWithoutLoss = 0;

                UIRoundController.Singleton.LostRoundWindow();

                GameController.ResetStars();
            }

            UIRoundController.Singleton.UpdateStarCount(false);
            UIRoundController.Singleton.UpdateCurrentRound(roundsPassedWithoutLoss);
        }

        #endregion

        #region Challenge helpers

        private void ReadyActionControllers()
        {
            ProjectileController.Singleton.SetupProjectiles();
            VehicleController.Singleton.SetupVehicles();
        }

        private IEnumerator FortressPhase()
        {
            yield return StartCoroutine(ProjectileController.Singleton.FireProjectiles());
        }

        private IEnumerator TunnelPhase()
        {
            yield return StartCoroutine(VehicleController.Singleton.LaunchVehicles());
        }

        #endregion

        #region Round templates helpers

        private RoundTemplate ChooseRoundTemplate()
        {
            var viableRoundTemplates = roundTemplates.Where(rt => rt.RoundCanAppear(GameController.TotalStars)).ToList();

            if (viableRoundTemplates.Count > 0) // Make sure that there are round we can use.
            {
                return viableRoundTemplates[Random.Range(0, viableRoundTemplates.Count)];
            }
            else
            {
                return roundTemplates[Random.Range(0, roundTemplates.Length)];
            }
        }

        #endregion

        #region Helper methods
        
        private float GetBuildPhaseTickSoundEffectCooldown(float t, float phaseTime) => Mathf.Pow(t / phaseTime, 0.6f);

        #endregion

    }

}