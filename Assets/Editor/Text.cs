using TMPro;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    /// <summary>
    /// Editor-builder compatibility surface for the project-wide TextMeshPro migration.
    /// Existing generated-layout code can keep its legacy formatting assignments while
    /// the actual component authored into scenes is a TextMeshProUGUI.
    /// </summary>
    internal sealed class Text : TextMeshProUGUI
    {
        public new Font font
        {
            get => null;
            set
            {
                // LegacyRuntime.ttf assignments are intentionally ignored. TMP uses the
                // project/default TMP font asset; no UnityEngine.UI.Text is generated.
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
                if ((base.fontStyle & FontStyles.Bold) != 0)
                {
                    return FontStyle.Bold;
                }
                if ((base.fontStyle & FontStyles.Italic) != 0)
                {
                    return FontStyle.Italic;
                }
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
            if ((value & TextAlignmentOptions.Top) != 0)
            {
                if ((value & TextAlignmentOptions.Left) != 0) return TextAnchor.UpperLeft;
                if ((value & TextAlignmentOptions.Right) != 0) return TextAnchor.UpperRight;
                return TextAnchor.UpperCenter;
            }
            if ((value & TextAlignmentOptions.Bottom) != 0)
            {
                if ((value & TextAlignmentOptions.Left) != 0) return TextAnchor.LowerLeft;
                if ((value & TextAlignmentOptions.Right) != 0) return TextAnchor.LowerRight;
                return TextAnchor.LowerCenter;
            }
            if ((value & TextAlignmentOptions.Left) != 0) return TextAnchor.MiddleLeft;
            if ((value & TextAlignmentOptions.Right) != 0) return TextAnchor.MiddleRight;
            return TextAnchor.MiddleCenter;
        }
    }
}
