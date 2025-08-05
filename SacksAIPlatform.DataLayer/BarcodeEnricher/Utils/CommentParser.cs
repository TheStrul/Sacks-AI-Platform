using BarcodeEnricher.Models;

namespace BarcodeEnricher.Utils
{
    public static class CommentParser
    {
        public static void EnrichFromComment(BarcodeRecord record, string comment)
        {
            // Placeholder for parsing logic
            if (comment.Contains("EDT")) record.Concentration = "EDT";
            if (comment.Contains("EDP")) record.Concentration = "EDP";
            if (comment.Contains("SP")) record.Type = "Spray";
            if (comment.Contains("(M)")) record.GenderCode = "M";
            if (comment.Contains("(W)")) record.GenderCode = "W";
            if (comment.Contains("(U)")) record.GenderCode = "U";
        }
    }
}
