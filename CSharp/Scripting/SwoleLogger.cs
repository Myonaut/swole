using System.Collections.Generic;

namespace Swolescript
{
    public class SwoleLogger
    {

        public int maxEntries = 1024;
        private List<string> log = new List<string>();

        public int Count => log.Count;
        public string this[int index] => index < 0 || index >= Count ? "" : log[index];

        public void AddEntry(string entry)
        {
            if (string.IsNullOrEmpty(entry)) return;
            log.Add(entry);
            while (maxEntries > 0 && log.Count > maxEntries) log.RemoveAt(0);
        }

        public void Log(string message)
        {
            AddEntry(message);
            LogInternal(message);
        }

        public void LogWarning(string warning)
        {
            AddEntry(warning);
            LogWarningInternal(warning);
        }

        public void LogError(string error)
        {
            AddEntry(error);
            LogErrorInternal(error);
        }

        protected virtual void LogInternal(string message) { }

        protected virtual void LogWarningInternal(string warning) { }

        protected virtual void LogErrorInternal(string error) { }

    }

}
