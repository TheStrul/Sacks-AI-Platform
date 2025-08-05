using System.Collections.Generic;

namespace BarcodeEnricher.Services
{
    public class KnowledgeBaseService
    {
        private readonly HashSet<string> knownBarcodes = new HashSet<string>();

        public bool IsKnown(string barcode) => knownBarcodes.Contains(barcode);

        public void Add(string barcode) => knownBarcodes.Add(barcode);
    }
}
