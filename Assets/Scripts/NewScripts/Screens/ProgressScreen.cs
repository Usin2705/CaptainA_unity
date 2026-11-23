using System.Collections;
using System.Collections.Generic;
using SaySuomi.Modals;
using SaySuomi.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class ProgressScreen : AppScreen
    {
        [Tooltip("The ListItemPanel prefab.")]
        [SerializeField]
        private GameObject phonemeScorePrefab;

        [Tooltip("The scroll content the rows are added to.")]
        [SerializeField]
        private GameObject content;

        [SerializeField]
        private Button infoButton;

        private List<GameObject> listItems = new List<GameObject>(); // To store item from scroll list and delete after inactive

        public override bool IsTabRoot => true;

        protected override void Bind()
        {
            ButtonWiring.Replace(infoButton, () => Navigator.GetManager().Push<InfoScreen>());
        }

        private void OnEnable()
        // Is called when SetActive(true)
        // The opposite is OnDisable() when SetActive(false)
        {
            UserData userData = SaveData.LoadUserData();

            // Use Sort for sorting from small to large
            // Reverse() for large to small
            userData.phonemeScores.Sort();

            // Initiate phoneme score list
            // from prefab "ListItemPanel"
            foreach (PhonemeScore phonemeScore in userData.phonemeScores)
            {
                // TODO consider sth to do with spacing
                string phoneme = phonemeScore.phoneme != " " ? phonemeScore.phoneme : "Spacing";

                string noTries = "Time practice: " + phonemeScore.no_tries.ToString();
                string score = ((int)(phonemeScore.average_score * 100)).ToString() + "%";

                string phoneColor = Const.GOOD_COLOR;
                if (phonemeScore.average_score < Const.BAD_SCORE)
                    phoneColor = Const.BAD_COLOR;
                else if (phonemeScore.average_score < Const.AVG_SCORE)
                    phoneColor = Const.AVG_COLOR;
                score = "<color=" + phoneColor + ">" + score + "</color>";

                GameObject phonemeScoreGO = Instantiate(
                    phonemeScorePrefab,
                    new Vector3(0, 0, 0),
                    Quaternion.identity
                );
                phonemeScoreGO.transform.SetParent(content.transform, false); // Register the big panel (ProgressScreen --> Content) as parent

                // Find the Button to register OnClick Function
                Button phonemeButton = phonemeScoreGO
                    .transform.Find("ScorePanel")
                    .transform.Find("PhonemeBG")
                    .GetComponent<Button>();
                // Pass phoneme value to onClick function
                // To be safe, remove all old listeners were add to this component
                phonemeButton.onClick.RemoveAllListeners();
                phonemeButton.onClick.AddListener(() =>
                    Navigator.GetManager().Push<PhonemeScreen>().Show(phoneme.ToLower())
                );

                phonemeScoreGO
                    .transform.Find("ScorePanel")
                    .transform.Find("PhonemeBG")
                    .transform.Find("Phoneme")
                    .GetComponent<TextMeshProUGUI>()
                    .text = phoneme;

                phonemeScoreGO
                    .transform.Find("ScorePanel")
                    .transform.Find("Score")
                    .GetComponent<TextMeshProUGUI>()
                    .text = score;

                phonemeScoreGO.transform.Find("NoTries").GetComponent<TextMeshProUGUI>().text =
                    noTries;

                listItems.Add(phonemeScoreGO);

                //phonemeScore.average_score;
                //phonemeScore.phoneme;
                //phonemeScore.no_tries;
            }
            // We don't need to set this anymore if we set content as parent
            // float prefabHeight = phonemeScorePrefab.transform.Find("ScorePanel").GetComponent<RectTransform>().sizeDelta.y;
            // prefabHeight += phonemeScorePrefab.transform.Find("NoTries").GetComponent<RectTransform>().sizeDelta.y;

            // content.GetComponent<RectTransform>().
            //         SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            //                                   prefabHeight*(userData.phonemeScores.Count+1) + 50.f);

            if (!PlayerPrefs.HasKey(Const.PREF_INS_PROFILE))
            {
                InstructionModal.Show(Const.PREF_INS_PROFILE, Const.INSTRUCTION_PROFILE);
            }
        }

        private void OnDisable()
        /*
        *   Need to Destroy all GO in the list to avoid create duplicate scorelist
        */
        {
            foreach (GameObject go in listItems)
                Destroy(go);
        }
    }
}
