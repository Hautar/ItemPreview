#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AssetGuidMap.Editor
{
    internal class UpdateReport
    {
        private List<string> messages = new List<string>();
        private List<UpdateReportMessageType>   types = new List<UpdateReportMessageType>();

        public int Count => messages.Count;
        public string Message(int index) => messages[index];
        public UpdateReportMessageType Type(int index) => types[index];
        
        public void Add(UpdateReportMessageType updateReportMessageType, string message)
        {
            types.Add(updateReportMessageType);
            messages.Add(message);
        }
        
        public void Log()
        {
            for (int i = 0; i < messages.Count; i++)
            {
                switch (types[i])
                {
                    case UpdateReportMessageType.Log:
                        Debug.Log(messages[i]);
                        break;
                    case UpdateReportMessageType.Warning:
                        Debug.LogWarning(messages[i]);
                        break;
                    case UpdateReportMessageType.Error:
                        Debug.LogError(messages[i]);
                        break;
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var message in messages)
            {
                sb.AppendLine(message);
            }

            return sb.ToString();
        }
    }
}
#endif
