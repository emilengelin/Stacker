﻿using Stacker.Rounds;
using Stacker.UIControllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649

namespace Stacker.UI.Challenges
{

    class UIChallenge : MonoBehaviour
    {

        #region Editor

        [SerializeField] private TMP_Text challengeName;
        [SerializeField] private TMP_Text challengeDescription;

        [Space]
        [SerializeField] private RectTransform starsSpawnReference;
        [SerializeField] private Image[]       starRewardImages = new Image[3];

        #endregion

        #region Private variables

        private RoundChallenge roundChallenge;

        private bool hasBeenCompleted;

        #endregion

        public void Initialize(RoundChallenge roundChallenge)
        {
            this.roundChallenge   = roundChallenge;
            this.hasBeenCompleted = false;

            challengeName.text        = roundChallenge.Name;
            challengeDescription.text = roundChallenge.Description;

            challengeName.fontStyle        &= ~FontStyles.Strikethrough; // Remove bit flag
            challengeDescription.fontStyle &= ~FontStyles.Strikethrough; // -||-

            // Deactivate all star reward images beforehand:
            for (int i = 0; i < starRewardImages.Length; i++)
            {
                starRewardImages[i].gameObject.SetActive(false);
            }

            // Now, activate all star reward images that should be active:
            for (int i = 0; i < roundChallenge.StarsReward; i++)
            {
                starRewardImages[i].gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Update the UI.
        /// </summary>
        public void UpdateUIChallenge()
        {
            if (roundChallenge != null && roundChallenge.IsCompleted && !hasBeenCompleted)
            {
                challengeName.fontStyle        |= FontStyles.Strikethrough;
                challengeDescription.fontStyle |= FontStyles.Strikethrough;

                UIChallengesController.Singleton.SpawnStars(starsSpawnReference.position, roundChallenge.StarsReward);

                hasBeenCompleted = true;
            }
        }

    }

}
