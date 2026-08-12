using UnityEngine;

public class InspectorLabelAttribute : PropertyAttribute
{
    public string Label { get; }

    public string ConditionFieldName { get; }

    public object CompareValue { get; }

    public bool HasCondition => !string.IsNullOrEmpty(ConditionFieldName);

    public InspectorLabelAttribute(string label)
    {
        Label = label;
    }

    public InspectorLabelAttribute(string label, string conditionFieldName, object compareValue)
    {
        Label = label;
        ConditionFieldName = conditionFieldName;
        CompareValue = compareValue;
    }
}
//[InspectorLabel("테스트")]

