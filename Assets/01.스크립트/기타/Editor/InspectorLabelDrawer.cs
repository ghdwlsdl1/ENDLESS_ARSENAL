using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InspectorLabelAttribute))]
public class InspectorLabelDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        InspectorLabelAttribute inspectorLabel = (InspectorLabelAttribute)attribute;

        if (!ShouldShow(property, inspectorLabel))
            return;

        label.text = inspectorLabel.Label;

        TextAreaAttribute textArea = GetTextAreaAttribute();

        if (textArea != null && property.propertyType == SerializedPropertyType.String)
        {
            DrawTextArea(position, property, label);
            return;
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        InspectorLabelAttribute inspectorLabel = (InspectorLabelAttribute)attribute;

        if (!ShouldShow(property, inspectorLabel))
            return 0f;

        TextAreaAttribute textArea = GetTextAreaAttribute();

        if (textArea != null && property.propertyType == SerializedPropertyType.String)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float textHeight = EditorGUIUtility.singleLineHeight * textArea.minLines;
            return lineHeight + textHeight;
        }

        return EditorGUI.GetPropertyHeight(property, label, true);
    }
    
    private TextAreaAttribute GetTextAreaAttribute()
    {
        object[] attributes = fieldInfo.GetCustomAttributes(typeof(TextAreaAttribute), true);
        return attributes.Length > 0 ? (TextAreaAttribute)attributes[0] : null;
    }
    
    private void DrawTextArea(Rect position, SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;

        Rect labelRect = new Rect(position.x, position.y, position.width, lineHeight);
        EditorGUI.LabelField(labelRect, label);

        Rect textRect = new Rect(
            position.x,
            position.y + lineHeight,
            position.width,
            position.height - lineHeight);

        property.stringValue = EditorGUI.TextArea(textRect, property.stringValue);
    }

    private bool ShouldShow(SerializedProperty property, InspectorLabelAttribute inspectorLabel)
    {
        if (!inspectorLabel.HasCondition)
            return true;

        SerializedProperty conditionProperty =
            property.serializedObject.FindProperty(inspectorLabel.ConditionFieldName);

        if (conditionProperty == null)
            return true;

        switch (conditionProperty.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return conditionProperty.boolValue.Equals(inspectorLabel.CompareValue);

            case SerializedPropertyType.Enum:
                return conditionProperty.enumNames[conditionProperty.enumValueIndex]
                       == inspectorLabel.CompareValue.ToString();

            case SerializedPropertyType.Integer:
                return conditionProperty.intValue.Equals(inspectorLabel.CompareValue);

            case SerializedPropertyType.Float:
                return Mathf.Approximately(
                    conditionProperty.floatValue,
                    System.Convert.ToSingle(inspectorLabel.CompareValue));

            case SerializedPropertyType.String:
                return conditionProperty.stringValue.Equals(inspectorLabel.CompareValue);

            default:
                return true;
        }
    }
}