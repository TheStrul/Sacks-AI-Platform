using System;
using System.Collections.Generic;
using BarcodeEnricher.Models;
using BarcodeEnricher.Services;
using BarcodeEnricher.Utils;

namespace BarcodeEnricher
{
    class Program
    {
        static void Main(string[] args)
        {
            var barcodesWithComments = new List<(string, string)>
            {
                ("088300606511", "CK OBSESSION 125 ML EDT SP (M) (FRANCE)"),
                ("085805574031", "WHITE TEA MANDARIN BLOSSOM(W)EDT SP 1.7oz")
            };

            var knowledgeBase = new KnowledgeBaseService();
            var results = new List<BarcodeRecord>();

            foreach (var (barcode, comment) in barcodesWithComments)
            {
                if (knowledgeBase.IsKnown(barcode)) continue;

                var record = new BarcodeRecord { Code = barcode, Comments = comment };
                CommentParser.EnrichFromComment(record, comment);
                record.Name = "ExampleName"; // Placeholder for web lookup
                record.Brand = "ExampleBrand";
                record.Size = 100;
                record.Units = "ml";

                knowledgeBase.Add(barcode);
                results.Add(record);
            }

            new CsvExportService().ExportToCsv("output.csv", results);
            Console.WriteLine("Export completed.");
        }
    }
}
