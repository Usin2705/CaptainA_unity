using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot layout pass over the background form's option groups.
///
/// The rows were built at different times and had drifted - widths of 329 against 600,
/// heights of 51 against 60, checkboxes half a pixel off, and two labels in
/// SelfAssessmentOptions inset by 61 and 94 instead of 60. The text read small and the
/// columns did not line up.
///
/// GenderOptions is the one that was already right, so it is the template rather than a
/// list of numbers copied into this file: the geometry is read off its first option row at
/// run time and stamped onto every other row. Fixing Gender in the Inspector and running
/// this again re-propagates it.
/// </summary>
public static class BackgroundPopupLayout
{
    // The group whose first option row defines the shape of every other row.
    private const string TEMPLATE_GROUP = "GenderOptions";

    // Groups of tick-box options to bring into line.
    private static readonly string[] OptionGroups = { "AgeOptions", "SelfAssessmentOptions" };

    // The free-text groups. No rows to align, but they share the popup's column width.
    private static readonly string[] InputGroups =
    {
        "MotherTongueOptions",
        "OtherLanguagesOptions",
    };

    private const float GROUP_WIDTH = 600f;
    private const float ROW_HEIGHT = 60f;

    [MenuItem("Tools/Background Form/Align option layout")]
    public static void Align()
    {
        // Play mode edits are thrown away when it exits, and a half-applied pass over 12
        // rows is worse than none. This project has been bitten by exactly that before.
        if (Application.isPlaying)
        {
            Debug.LogError("ABORTED: leave Play mode before running this.");
            return;
        }

        GameObject templateRow = FirstOptionRow(Find(TEMPLATE_GROUP));
        if (templateRow == null)
        {
            Debug.LogError($"ABORTED: no option row found under {TEMPLATE_GROUP}.");
            return;
        }

        RectTransform templateBox = Child(templateRow, "Unselected");
        RectTransform templateLabel = Child(templateRow, "Label");
        Text templateText = templateLabel == null ? null : templateLabel.GetComponent<Text>();

        if (templateBox == null || templateLabel == null || templateText == null)
        {
            Debug.LogError($"ABORTED: {TEMPLATE_GROUP} row is missing Unselected or Label.");
            return;
        }

        int rows = 0;

        foreach (string groupName in OptionGroups)
        {
            GameObject group = Find(groupName);
            if (group == null)
            {
                Debug.LogWarning($"Skipped {groupName}: not in the scene.");
                continue;
            }

            RectTransform groupRect = (RectTransform)group.transform;
            Undo.RecordObject(groupRect, "Align option layout");

            // Every child gets an equal slice of the height, the title included - that is
            // how GenderOptions arrives at 300 for a title plus four options.
            groupRect.sizeDelta = new Vector2(
                GROUP_WIDTH,
                ROW_HEIGHT * groupRect.childCount
            );
            EditorUtility.SetDirty(groupRect);

            foreach (RectTransform row in OptionRows(group))
            {
                ApplyRow(row, templateBox, templateLabel, templateText);
                rows++;
            }
        }

        foreach (string groupName in InputGroups)
        {
            GameObject group = Find(groupName);
            if (group == null)
            {
                continue;
            }

            // Width only. These hold an input field, not tick-boxes, so their height is
            // whatever the field needs.
            RectTransform groupRect = (RectTransform)group.transform;
            Undo.RecordObject(groupRect, "Align option layout");
            groupRect.sizeDelta = new Vector2(GROUP_WIDTH, groupRect.sizeDelta.y);
            EditorUtility.SetDirty(groupRect);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(
            $"Aligned {rows} option rows across {OptionGroups.Length} groups, using "
                + $"{TEMPLATE_GROUP} as the template. Save the scene to keep it."
        );
    }

    private static void ApplyRow(
        RectTransform row,
        RectTransform templateBox,
        RectTransform templateLabel,
        Text templateText
    )
    {
        Undo.RecordObject(row, "Align option layout");
        row.sizeDelta = new Vector2(GROUP_WIDTH, ROW_HEIGHT);
        EditorUtility.SetDirty(row);

        RectTransform box = Child(row.gameObject, "Unselected");
        if (box != null)
        {
            CopyRect(templateBox, box);

            // The tick that replaces the empty box when chosen. Sized from the template so
            // the two states cannot drift apart.
            RectTransform tick = Child(box.gameObject, "Selected");
            RectTransform templateTick = Child(templateBox.gameObject, "Selected");
            if (tick != null && templateTick != null)
            {
                CopyRect(templateTick, tick);
            }
        }

        RectTransform label = Child(row.gameObject, "Label");
        if (label == null)
        {
            return;
        }

        CopyRect(templateLabel, label);

        Text text = label.GetComponent<Text>();
        if (text != null)
        {
            Undo.RecordObject(text, "Align option layout");
            text.fontSize = templateText.fontSize;
            text.alignment = templateText.alignment;
            text.font = templateText.font;
            text.color = templateText.color;
            EditorUtility.SetDirty(text);
        }
    }

    private static void CopyRect(RectTransform from, RectTransform to)
    {
        Undo.RecordObject(to, "Align option layout");
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.anchoredPosition = from.anchoredPosition;
        to.sizeDelta = from.sizeDelta;
        EditorUtility.SetDirty(to);
    }

    // An option row is any child carrying a Toggle. That is what separates the four real
    // options from the Title sitting above them.
    private static IEnumerable<RectTransform> OptionRows(GameObject group)
    {
        foreach (Transform child in group.transform)
        {
            if (child.GetComponent<Toggle>() != null)
            {
                yield return (RectTransform)child;
            }
        }
    }

    private static GameObject FirstOptionRow(GameObject group)
    {
        if (group == null)
        {
            return null;
        }

        RectTransform first = OptionRows(group).FirstOrDefault();
        return first == null ? null : first.gameObject;
    }

    private static RectTransform Child(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        return t == null ? null : (RectTransform)t;
    }

    // Includes inactive objects: the popup is closed in the editor most of the time.
    private static GameObject Find(string name)
    {
        foreach (
            GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()
        )
        {
            if (go.name == name && go.scene.IsValid())
            {
                return go;
            }
        }

        return null;
    }
}
