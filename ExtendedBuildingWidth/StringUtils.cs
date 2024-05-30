using System;

namespace ExtendedBuildingWidth
{
    public static class StringUtils
    {
        public static string CombineTooltip(string tooltipLine1, string tooltipLine2)
        {
            if (string.IsNullOrEmpty(tooltipLine1) && string.IsNullOrEmpty(tooltipLine2))
            {
                return string.Empty;
            }
            return tooltipLine1 + Environment.NewLine + tooltipLine2;
        }
    }
}