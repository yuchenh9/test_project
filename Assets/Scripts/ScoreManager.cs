using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Namespace for TextMeshPro

namespace DynamicMeshCutter
{
    public class ScoreManager : MonoBehaviour
    {
        public TextMeshProUGUI scoreText; // Assign this from the inspector
        private int score = 0; // Initial score

        void Start()
        {
            UpdateScoreText();
        }

        public void AddScore(int points)
        {
            score += points;
            UpdateScoreText();
        }

        void UpdateScoreText()
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }
}
