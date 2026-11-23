using SaySuomi.Modals;
using SaySuomi.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class PhonemeScreen : AppScreen
    {
        [Header("Explanation")]
        [SerializeField]
        private TextMeshProUGUI explainText;

        [SerializeField]
        private TextMeshProUGUI creditText;

        [Header("Mouth Images")]
        [SerializeField]
        private Image frontMouth1;

        [SerializeField]
        private Image frontMouth2;

        [SerializeField]
        private Image sideMouth1;

        [SerializeField]
        private Image sideMouth2;

        [Header("Buttons")]
        [SerializeField]
        private Button backButton;

        [Tooltip("Opens the phoneme's video link.")]
        [SerializeField]
        private Button videoButton;

        private Animation mouthAnimation;

        private SOPhonemeHelper phonemeSO;

        public override ScreenLayer Layer => ScreenLayer.Overlay;

        protected override void Bind()
        {
            mouthAnimation = GetComponent<Animation>();

            ButtonWiring.Replace(backButton, () => Navigator.GetManager().Back());
            ButtonWiring.Replace(videoButton, OnVideoButtonClick);
        }

        private void OnEnable()
        {
            // Empty the first time, because the caller only gives the phoneme after Push.
            // Set when something on the Screen layer covered this and has now closed.
            if (phonemeSO != null)
            {
                Display();
            }

            if (!PlayerPrefs.HasKey(Const.PREF_INS_PHONE))
            {
                InstructionModal.Show(Const.PREF_INS_PHONE, Const.INSTRUCTION_PHONE);
            }
        }

        private void Update()
        {
            // Set focus to the panel
            // so that the back button won't go back to the URL
            EventSystem.current.SetSelectedGameObject(gameObject, null);
        }

        private void OnDisable()
        {
            mouthAnimation.clip = null;
        }

        public void Show(string phoneme)
        {
            phonemeSO = Resources.Load<SOPhonemeHelper>(Const.PHONE_HELP_PATH + phoneme.ToLower());

            // In case there is no entry for this phoneme
            if (phonemeSO == null)
            {
                phonemeSO = Resources.Load<SOPhonemeHelper>(Const.PHONE_HELP_PATH + "spacing");
            }

            Display();
        }

        private void OnVideoButtonClick()
        {
            Application.OpenURL(phonemeSO.videoURL);
        }

        private void Display()
        {
            string phonemeString = TextUtils.WrapPhonemeSO(
                phonemeSO,
                Const.GOOD_COLOR,
                isBold: false
            );

            explainText.text = phonemeString + " " + phonemeSO.instruction;
            frontMouth1.sprite = phonemeSO.front_1;
            frontMouth2.sprite = phonemeSO.front_2;
            sideMouth1.sprite = phonemeSO.side_1;
            sideMouth2.sprite = phonemeSO.side_2;
            mouthAnimation.clip = phonemeSO.animaClip;
            mouthAnimation.Play();

            creditText.text =
                "Top images: "
                + phonemeSO.front_credit
                + ".\nBottom images: "
                + phonemeSO.side_credit
                + ".";
        }
    }
}
