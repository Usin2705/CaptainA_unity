using UnityEditor;
using UnityEngine;

namespace SaySuomi.EditorTools
{
    public static class DeletePlayerPrefsMenu
    {
        [MenuItem("Tools/Delete All PlayerPrefs")]
        private static void DeleteAll()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete All PlayerPrefs",
                "Delete every PlayerPrefs value for this project on this computer? This can't be undone.",
                "Delete",
                "Cancel"
            );

            if (confirmed)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("Deleted all PlayerPrefs.");
            }
        }
    }
}
