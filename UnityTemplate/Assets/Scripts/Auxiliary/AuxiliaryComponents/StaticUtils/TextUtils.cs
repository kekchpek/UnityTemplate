using TMPro;

namespace AuxiliaryComponents.StaticUtils
{
    public static class TextUtils
    {
        public static float TextWidthApproximation (
            string text, 
            TMP_FontAsset fontAsset, 
            float fontSize, 
            FontStyles style)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }
            // Compute scale of the target point size relative to the sampling point size of the font asset.
            float pointSizeScale = fontSize / (fontAsset.faceInfo.pointSize * fontAsset.faceInfo.scale);
 
            float styleSpacingAdjustment = (style & FontStyles.Bold) == FontStyles.Bold ? fontAsset.boldSpacing : 0;
            float normalSpacingAdjustment = fontAsset.normalSpacingOffset;
 
            float width = 0;
 
            foreach (var unicode in text)
            {
                // Make sure the given unicode exists in the font asset.
                if (fontAsset.characterLookupTable.TryGetValue(unicode, out var character))
                    width += character.glyph.metrics.horizontalAdvance * pointSizeScale + (styleSpacingAdjustment + normalSpacingAdjustment);
            }
 
            return width;
        }
    }
}