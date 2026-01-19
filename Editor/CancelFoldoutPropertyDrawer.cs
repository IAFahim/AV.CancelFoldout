using System.Collections.Generic;
using System.Reflection;
using AV.CancelFoldout.Runtime;
using UnityEditor;
using UnityEngine;

[HelpURL("https://github.com/IAFahim/AV.CancelFoldout")]

namespace AV.CancelFoldout.Editor
{
    [CustomPropertyDrawer(typeof(CancelFoldoutAttribute))]
    [CustomPropertyDrawer(typeof(InlineAttribute))]
    public class CancelFoldoutPropertyDrawer : PropertyDrawer
    {
        // -----------------------------------------------------------------------
        // 1. Height Calculation
        // -----------------------------------------------------------------------
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (CancelFoldoutAttribute)attribute;

            // Flat inline mode: Always show children, no header
            if (!attr.CanToggle) return GetInlineHeight(property);

            // Toggle mode: Show collapsed/expanded based on state
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var currentProperty = property.Copy();
            var endProperty = property.GetEndProperty();

            if (currentProperty.NextVisible(true))
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, endProperty)) break;
                    height += EditorGUI.GetPropertyHeight(currentProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                } while (currentProperty.NextVisible(false));

            return height;
        }

        private float GetInlineHeight(SerializedProperty property)
        {
            var height = 0f;
            var currentProperty = property.Copy();
            var endProperty = property.GetEndProperty();

            if (currentProperty.NextVisible(true))
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, endProperty)) break;
                    height += EditorGUI.GetPropertyHeight(currentProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                } while (currentProperty.NextVisible(false));

            // Add minimal spacing at top
            if (height > 0) height += EditorGUIUtility.standardVerticalSpacing;

            return height;
        }

        // -----------------------------------------------------------------------
        // 2. Main Draw Loop
        // -----------------------------------------------------------------------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var attr = (CancelFoldoutAttribute)attribute;

            // Flat inline mode: No header, just draw children inline
            if (!attr.CanToggle)
                DrawInline(position, property, label);
            // Toggle mode: Show foldout with dashboard
            else
                DrawFoldout(position, property, label, attr);

            EditorGUI.EndProperty();
        }

        // -----------------------------------------------------------------------
        // 2a. Flat Inline Mode (No Foldout)
        // -----------------------------------------------------------------------
        private void DrawInline(Rect position, SerializedProperty property, GUIContent label)
        {
            // Track if we're the root inline drawer to manage indent
            var propertyPath = property.propertyPath;
            var isRootInline = !IsParentInline(property);

            var originalIndent = EditorGUI.indentLevel;

            // Only add indent if we're the root inline property
            // Nested inline properties stay at same level to prevent runaway indent
            if (isRootInline)
            {
                // Draw a subtle separator line above
                var separatorRect = new Rect(position.x, position.y, position.width, 1);
                EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));

                // Add small label showing struct name (miniature, non-interactive)
                var title = string.IsNullOrEmpty(label.text) ? property.displayName : label.text;
                var titleRect = new Rect(position.x, position.y + 2, position.width, EditorGUIUtility.singleLineHeight);
                GUI.Label(titleRect, new GUIContent(title), EditorStyles.miniLabel);

                EditorGUI.indentLevel++;
            }

            // Draw children inline
            var yOffset = position.y + (isRootInline
                ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
                : 0);
            var currentProperty = property.Copy();
            var endProperty = property.GetEndProperty();

            if (currentProperty.NextVisible(true))
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, endProperty)) break;

                    var propertyHeight = EditorGUI.GetPropertyHeight(currentProperty, true);
                    var childRect = new Rect(position.x, yOffset, position.width, propertyHeight);

                    // For nested inline structs, don't add extra label
                    if (IsNestedInline(currentProperty))
                        EditorGUI.PropertyField(childRect, currentProperty, GUIContent.none);
                    else
                        EditorGUI.PropertyField(childRect, currentProperty, true);

                    yOffset += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
                } while (currentProperty.NextVisible(false));

            EditorGUI.indentLevel = originalIndent;
        }

        // -----------------------------------------------------------------------
        // 2b. Toggle Mode (Foldout with Dashboard)
        // -----------------------------------------------------------------------
        private void DrawFoldout(Rect position, SerializedProperty property, GUIContent label,
            CancelFoldoutAttribute attr)
        {
            var title = string.IsNullOrEmpty(attr.CustomTitle) ? label.text : attr.CustomTitle;
            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // -- DASHBOARD PRE-CALCULATION --
            var dashboardWidth = 0f;
            if (!property.isExpanded)
            {
                var labelWidth = EditorStyles.label.CalcSize(new GUIContent(title)).x + 20f;
                dashboardWidth = headerRect.width - labelWidth - 10f;
                if (dashboardWidth < 0) dashboardWidth = 0;
            }

            // -- DRAW FOLDOUT --
            var foldoutRect = headerRect;
            if (!property.isExpanded && dashboardWidth > 0) foldoutRect.width -= dashboardWidth;

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, new GUIContent(title), true);

            // -- DRAW DASHBOARD (If Collapsed) --
            if (!property.isExpanded && dashboardWidth > 50f)
            {
                var dashboardRect = new Rect(
                    headerRect.x + (headerRect.width - dashboardWidth),
                    headerRect.y,
                    dashboardWidth,
                    headerRect.height
                );
                DrawFlexDashboard(dashboardRect, property);
            }

            // -- DRAW CHILDREN (If Expanded) --
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var yOffset = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var currentProperty = property.Copy();
                var endProperty = property.GetEndProperty();

                if (currentProperty.NextVisible(true))
                    do
                    {
                        if (SerializedProperty.EqualContents(currentProperty, endProperty)) break;

                        var propertyHeight = EditorGUI.GetPropertyHeight(currentProperty, true);
                        var childRect = new Rect(position.x, yOffset, position.width, propertyHeight);

                        EditorGUI.PropertyField(childRect, currentProperty, true);

                        yOffset += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
                    } while (currentProperty.NextVisible(false));

                EditorGUI.indentLevel--;
            }
        }

        private void DrawFlexDashboard(Rect rect, SerializedProperty rootProperty)
        {
            // A. Harvest Fields
            var flexFields = new List<FlexField>();
            var endProperty = rootProperty.GetEndProperty();
            var currentProperty = rootProperty.Copy();

            // Force enter children
            if (currentProperty.NextVisible(true))
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, endProperty)) break;
                    var flexField = AnalyzeField(currentProperty);
                    if (flexField.MinWidth > 0) flexFields.Add(flexField);
                } while (currentProperty.NextVisible(false));

            if (flexFields.Count == 0) return;

            // B. Calculate Layout
            var totalMinWidth = 0f;
            var totalWeight = 0f;

            foreach (var flexField in flexFields)
            {
                // Native miniLabel calculation
                var labelWidth = string.IsNullOrEmpty(flexField.Label)
                    ? 0
                    : EditorStyles.miniLabel.CalcSize(new GUIContent(flexField.Label)).x + 2;
                totalMinWidth += flexField.MinWidth + labelWidth + 5f; // +5 padding
                totalWeight += flexField.FlexWeight;
            }

            // C. Cull Fields if not enough space
            var visibleCount = flexFields.Count;
            while (totalMinWidth > rect.width && visibleCount > 0)
            {
                var removed = flexFields[visibleCount - 1];
                var labelWidth = string.IsNullOrEmpty(removed.Label)
                    ? 0
                    : EditorStyles.miniLabel.CalcSize(new GUIContent(removed.Label)).x + 2;
                totalMinWidth -= removed.MinWidth + labelWidth + 5f;
                totalWeight -= removed.FlexWeight;
                visibleCount--;
            }

            // D. Distribute Excess Space
            var availableSpace = rect.width - totalMinWidth;
            if (availableSpace < 0) availableSpace = 0;

            // E. Draw
            var xPos = rect.x;

            // We use a safe indentation level for the dashboard to prevent shifting
            var originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            for (var i = 0; i < visibleCount; i++)
            {
                var flexField = flexFields[i];
                var labelWidth = string.IsNullOrEmpty(flexField.Label)
                    ? 0
                    : EditorStyles.miniLabel.CalcSize(new GUIContent(flexField.Label)).x + 2;

                float extraWidth = 0;
                if (totalWeight > 0 && flexField.FlexWeight > 0) extraWidth = availableSpace * (flexField.FlexWeight / totalWeight);

                var totalFieldWidth = flexField.MinWidth + labelWidth + extraWidth;

                // Draw Label
                if (!string.IsNullOrEmpty(flexField.Label))
                {
                    var labelRect = new Rect(xPos, rect.y, labelWidth, rect.height);
                    GUI.Label(labelRect, new GUIContent(flexField.Label, flexField.Tooltip), EditorStyles.miniLabel);
                }

                // Draw Field
                var fieldRect = new Rect(xPos + labelWidth, rect.y + 1, totalFieldWidth - labelWidth - 2, rect.height - 2);

                // Color Hints (Optional visual flair, keeps native look mostly)
                var originalColor = GUI.color;
                if (flexField.UseColorHint && flexField.Property.propertyType == SerializedPropertyType.Float)
                {
                    var value = flexField.Property.floatValue;
                    if (value <= 0) GUI.color = new Color(1f, 0.6f, 0.6f); // Red tint
                    else if (value < 100) GUI.color = new Color(1f, 0.9f, 0.6f); // Yellow tint
                }

                EditorGUI.PropertyField(fieldRect, flexField.Property, GUIContent.none);

                GUI.color = originalColor;
                xPos += totalFieldWidth + 5f;
            }

            EditorGUI.indentLevel = originalIndent;
        }

        private FlexField AnalyzeField(SerializedProperty property)
        {
            var flexField = new FlexField { Property = property.Copy() };
            var displayName = property.displayName;
            var lowerName = displayName.ToLower();

            flexField.Tooltip = property.tooltip;
            if (string.IsNullOrEmpty(flexField.Tooltip)) flexField.Tooltip = displayName;

            flexField.FlexWeight = 1f;
            flexField.UseColorHint = false;
            flexField.Label = GetShortLabel(displayName);

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    flexField.MinWidth = 16f;
                    flexField.FlexWeight = 0f;
                    flexField.Label = ""; // No label for checkboxes usually
                    break;

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Float:
                    flexField.MinWidth = 35f;
                    flexField.FlexWeight = 1f;
                    if (lowerName.Contains("health") || lowerName.Contains("hp")) flexField.UseColorHint = true;
                    break;

                case SerializedPropertyType.String:
                    flexField.MinWidth = 50f;
                    flexField.FlexWeight = 2f;
                    break;

                case SerializedPropertyType.Enum:
                    flexField.MinWidth = 60f;
                    flexField.FlexWeight = 1.5f;
                    flexField.Label = "";
                    break;

                case SerializedPropertyType.Color:
                    flexField.MinWidth = 30f;
                    flexField.FlexWeight = 0.5f;
                    flexField.Label = "";
                    break;

                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                    flexField.MinWidth = 90f;
                    flexField.FlexWeight = 2f;
                    flexField.Label = ""; // Vectors are too big for labels usually
                    break;

                default:
                    flexField.MinWidth = 0f; // Skip unsupported types in dashboard
                    break;
            }

            return flexField;
        }

        private string GetShortLabel(string name)
        {
            var lower = name.ToLower();
            if (lower.Contains("health") || lower == "hp") return "HP";
            if (lower.Contains("damage") || lower == "dmg") return "Dmg";
            if (lower.Contains("speed") || lower == "spd") return "Spd";
            if (lower.Contains("count")) return "#";
            if (lower.Contains("amount")) return "Amt";
            if (lower.Contains("id")) return "ID";
            if (lower.Contains("modifier")) return "Mod";

            // If name is short, use it
            if (name.Length <= 3) return name;

            // Otherwise truncate
            return name.Substring(0, 3);
        }

        // -----------------------------------------------------------------------
        // 3. HELPER METHODS - Inline Nesting Detection
        // -----------------------------------------------------------------------

        /// <summary>
        ///     Checks if the parent of this property has a CancelFoldoutAttribute with CanToggle=false
        /// </summary>
        private bool IsParentInline(SerializedProperty property)
        {
            // Extract parent path by removing last component
            var lastDot = property.propertyPath.LastIndexOf('.');
            if (lastDot <= 0) return false;

            var parentPath = property.propertyPath.Substring(0, lastDot);

            // Get the serialized object and navigate to parent
            var parentProp = property.serializedObject.FindProperty(parentPath);
            if (parentProp == null) return false;

            // Check if parent has our attribute with CanToggle=false
            return HasInlineCancelFoldout(parentProp);
        }

        /// <summary>
        ///     Checks if this specific property has CancelFoldoutAttribute with CanToggle=false
        /// </summary>
        private bool IsNestedInline(SerializedProperty property)
        {
            return HasInlineCancelFoldout(property);
        }

        /// <summary>
        ///     Core check: Does this property have an inline CancelFoldout attribute?
        ///     Uses the PropertyDrawer's built-in fieldInfo when available, otherwise falls back to reflection.
        /// </summary>
        private bool HasInlineCancelFoldout(SerializedProperty property)
        {
            // First try to use the drawer's fieldInfo (works for direct properties)
            if (fieldInfo != null && property.propertyPath == fieldInfo.Name)
            {
                var attr = fieldInfo.GetCustomAttributes(typeof(CancelFoldoutAttribute), false);
                if (attr.Length > 0) return !((CancelFoldoutAttribute)attr[0]).CanToggle;
            }

            // Fallback: Reflection for nested properties (arrays, nested structs, etc.)
            // Extract the field name from the property path
            var pathParts = property.propertyPath.Split('.');
            if (pathParts.Length == 0) return false;

            var fieldName = pathParts[0];
            var targetType = property.serializedObject.targetObject.GetType();

            // Try to get the field
            var field = targetType.GetField(fieldName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            if (field == null) return false;

            // Check for CancelFoldout attribute
            var attributes = field.GetCustomAttributes(typeof(CancelFoldoutAttribute), false);
            if (attributes.Length == 0) return false;

            var cancelFoldoutAttr = (CancelFoldoutAttribute)attributes[0];
            return !cancelFoldoutAttr.CanToggle;
        }

        // -----------------------------------------------------------------------
        // 4. FLEX DASHBOARD SYSTEM (Cleaned up for Native Look)
        // -----------------------------------------------------------------------
        private struct FlexField
        {
            public SerializedProperty Property;
            public float MinWidth;
            public float FlexWeight;
            public string Label;
            public string Tooltip;
            public bool UseColorHint;
        }
    }
}