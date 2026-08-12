using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatLineUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;

    public void SetData(
        string label,
        string value)
    {
        bool spacer =
            string.IsNullOrEmpty(label) &&
            string.IsNullOrEmpty(value);

        labelText.gameObject.SetActive(!spacer);
        valueText.gameObject.SetActive(!spacer);

        if (!spacer)
        {
            labelText.text = label;
            valueText.text = value;
        }
        else
        {
            labelText.text = string.Empty;
            valueText.text = string.Empty;
        }

        LayoutElement layout =
            GetComponent<LayoutElement>();

        if (layout != null)
            layout.preferredHeight = spacer ? 15f : 30f;
    }
}