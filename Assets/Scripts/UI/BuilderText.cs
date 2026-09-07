using TMPro;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    /// <summary>
    /// Runtime-safe TextMeshProUGUI component used by the editor builders while their
    /// legacy formatting syntax is migrated. The component is part of the player assembly,
    /// so generated scenes never serialize an editor-only text script.
    /// </summary>
    public sealed class Text : TextMeshProUGUI
    {
        public new Font font
        {
            get => null;
            set
            {
                // Legacy Runtime.ttf assignments from historical builders are ignored.
                // TextMeshPro uses its configured/default TMP font asset instead.
            }
        }

        public new int fontSize
        {
            get => Mathf.RoundToInt(base.fontSize);
            set => base.fontSize = value;
        }

        public new FontStyle fontStyle
        {
            get
            {
                if ((base.fontStyle & FontStyles.Bold) != 0) return FontStyle.Bold;
                if ((base.fontStyle & FontStyles.Italic) != 0) return FontStyle.Italic;
                return FontStyle.Normal;
            }
            set
            {
                switch (value)
                {
                    case FontStyle.Bold:
                        base.fontStyle = FontStyles.Bold;
                        break;
                    case FontStyle.Italic:
                        base.fontStyle = FontStyles.Italic;
                        break;
                    case FontStyle.BoldAndItalic:
                        base.fontStyle = FontStyles.Bold | FontStyles.Italic;
                        break;
                    default:
                        base.fontStyle = FontStyles.Normal;
                        break;
                }
            }
        }

        public new TextAnchor alignment
        {
            get => ToLegacyAlignment(base.alignment);
            set => base.alignment = ToTmpAlignment(value);
        }

        public bool resizeTextForBestFit
        {
            get => enableAutoSizing;
            set => enableAutoSizing = value;
        }

        public int resizeTextMinSize
        {
            get => Mathf.RoundToInt(fontSizeMin);
            set => fontSizeMin = value;
        }

        public int resizeTextMaxSize
        {
            get => Mathf.RoundToInt(fontSizeMax);
            set => fontSizeMax = value;
        }

        public HorizontalWrapMode horizontalOverflow
        {
            get => textWrappingMode == TextWrappingModes.NoWrap
                ? HorizontalWrapMode.Overflow
                : HorizontalWrapMode.Wrap;
            set => textWrappingMode = value == HorizontalWrapMode.Overflow
                ? TextWrappingModes.NoWrap
                : TextWrappingModes.Normal;
        }

        public VerticalWrapMode verticalOverflow
        {
            get => overflowMode == TextOverflowModes.Overflow
                ? VerticalWrapMode.Overflow
                : VerticalWrapMode.Truncate;
            set => overflowMode = value == VerticalWrapMode.Overflow
                ? TextOverflowModes.Overflow
                : TextOverflowModes.Truncate;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor value)
        {
            switch (value)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static TextAnchor ToLegacyAlignment(TextAlignmentOptions value)
        {
            switch (value)
            {
                case TextAlignmentOptions.TopLeft: return TextAnchor.UpperLeft;
                case TextAlignmentOptions.Top: return TextAnchor.UpperCenter;
                case TextAlignmentOptions.TopRight: return TextAnchor.UpperRight;
                case TextAlignmentOptions.Left: return TextAnchor.MiddleLeft;
                case TextAlignmentOptions.Center: return TextAnchor.MiddleCenter;
                case TextAlignmentOptions.Right: return TextAnchor.MiddleRight;
                case TextAlignmentOptions.BottomLeft: return TextAnchor.LowerLeft;
                case TextAlignmentOptions.Bottom: return TextAnchor.LowerCenter;
                case TextAlignmentOptions.BottomRight: return TextAnchor.LowerRight;
                default: return TextAnchor.MiddleCenter;
            }
        }
    }
}
