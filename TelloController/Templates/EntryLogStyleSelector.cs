using System;
using System.Windows;
using System.Windows.Controls;
using TelloController.Models;

namespace TelloController.Templates
{
    public class EntryLogStyleSelector : StyleSelector
    {
        private readonly ResourceDictionary _dictionary;

        public EntryLogStyleSelector()
        {
            _dictionary = new ResourceDictionary
            {
                Source = new Uri(@"pack://application:,,,/TelloController;component/Templates/EntryLogStyleDictionary.xaml")
            };
        }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is LogEntry entry)
            {
                switch (entry.EntryType)
                {
                    case Enums.LogEntryType.Send:
                        return (Style)_dictionary["SendTypeRowStyle"];
                    case Enums.LogEntryType.Receive:
                        return (Style)_dictionary["ReceiveTypeRowStyle"];
                    case Enums.LogEntryType.Local:
                        return (Style)_dictionary["LocalTypeRowStyle"];
                    case Enums.LogEntryType.Error:
                        return (Style)_dictionary["ErrorTypeRowStyle"];
                    case Enums.LogEntryType.Space:
                        return (Style)_dictionary["SpaceTypeRowStyle"];
                }
            }
            return null;
        }
    }
}
