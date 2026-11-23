using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    /// <summary>Marks an object on FlashcardsScreen as a deck, and says which one.</summary>
    public sealed class FlashcardDeck : MonoBehaviour
    {
        [Header("Deck")]
        [Tooltip("The name SaveData loads and saves this deck under.")]
        [SerializeField]
        private string deckName;

        [Tooltip("The deck unlocked by the secret code. Set this on one deck only.")]
        [SerializeField]
        private bool isSecret;

        [Header("Deck Display")]
        [SerializeField]
        private Image progressBar;

        [SerializeField]
        private TextMeshProUGUI progressText;

        [SerializeField]
        private TextMeshProUGUI cardInfoText;

        /// <summary>The name SaveData loads and saves this deck under.</summary>
        public string DeckName => deckName;

        /// <summary>True for a deck only shown once the secret text has been entered.</summary>
        public bool IsSecret => isSecret;

        public Image ProgressBar => progressBar;

        public TextMeshProUGUI ProgressText => progressText;

        public TextMeshProUGUI CardInfoText => cardInfoText;
    }
}
