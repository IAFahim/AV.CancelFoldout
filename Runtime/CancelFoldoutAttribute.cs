using System.Diagnostics;
using UnityEngine;

namespace AV.CancelFoldout.Runtime
{
    /// <summary>
    ///     Controls how serialized classes/structs are rendered in the Inspector.
    ///     <para />
    ///     Usage:
    ///     <list type="bullet">
    ///         <item>[CancelFoldout] - Shows a foldout with dashboard (when collapsed)</item>
    ///         <item>[CancelFoldout(canToggle: false)] - Completely inline, no foldout (FLAT mode)</item>
    ///         <item>[Inline] - Alias for flat inline mode (cleaner syntax)</item>
    ///     </list>
    /// </summary>
    [HelpURL("https://github.com/IAFahim/AV.CancelFoldout")]
    public class CancelFoldoutAttribute : PropertyAttribute
    {
        public readonly bool CanToggle;
        public readonly string CustomTitle;

        /// <param name="customTitle">Override variable name.</param>
        /// <param name="canToggle">
        ///     If true (default): Shows a toggleable foldout with dashboard when collapsed.
        ///     If false: Completely inline rendering - no foldout, no header, just flat children.
        /// </param>
        public CancelFoldoutAttribute(string customTitle = null, bool canToggle = true)
        {
            CustomTitle = customTitle;
            CanToggle = canToggle;
        }
    }

    /// <summary>
    ///     Convenience alias for [CancelFoldout(canToggle: false)].
    ///     Renders a serialized class or struct completely inline with no foldout arrow or header.
    ///     Perfect for nested data structures where you want everything visible at once.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class InlineAttribute : CancelFoldoutAttribute
    {
        public InlineAttribute(string customTitle = null) : base(customTitle, false)
        {
        }
    }
}