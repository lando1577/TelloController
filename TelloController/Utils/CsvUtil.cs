using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelloController.Models;

namespace TelloController.Utils
{
    public static class CsvUtil
    {
        public static void GenerateCsv(string file, List<TelloState> states)
        {
            if (!file.EndsWith(".csv"))
            {
                file += ".csv";
            }
            var lines = states.Select(state => state.GetCsvDataRow()).ToList();
            lines.Insert(0, TelloState.GetCsvHeaderRow());
            var csvFile = File.Create(file);
            csvFile.Close();
            File.WriteAllLines(file, lines);
        }
    }
}
