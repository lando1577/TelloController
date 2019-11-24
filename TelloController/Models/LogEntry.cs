using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelloController.Enums;

namespace TelloController.Models
{
    public class LogEntry
    {
        public LogEntry(LogEntryType type, string message, DateTime? time = null)
        {
            EntryType = type;
            Message = message;
            Time = time ?? DateTime.Now;
        }

        public LogEntryType EntryType { get; }

        public string Message { get; }

        public DateTime Time { get; }

        public override string ToString()
        {
            return $"{Time}: [{EntryType}] {Message}";
        }
    }
}
