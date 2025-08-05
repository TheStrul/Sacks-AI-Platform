using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarcodeEnricher.Models;

namespace BarcodeEnricher.Services
{
    public class CsvExportService
    {
        public void ExportToCsv(string filePath, List<BarcodeRecord> records)
        {
            var lines = new List<string>
            {
                "Code,Brand,Name,GenderCode,Concentration,Type,Size,Units,Comments"
            };
            lines.AddRange(records.Select(r =>
                $"{r.Code},{r.Brand},{r.Name},{r.GenderCode},{r.Concentration},{r.Type},{r.Size},{r.Units},{r.Comments}"
            ));
            File.WriteAllLines(filePath, lines);
        }
    }
}
