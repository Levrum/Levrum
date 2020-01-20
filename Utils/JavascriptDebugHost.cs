using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace Levrum.Utils
{
    public class JavascriptDebugHost
    {
        public delegate void DebugMessageDelegate(object sender, string message);

        public void WriteLine()
        {
            writeMessage("\r\n");
        }

        public void Write(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            WriteLine(json);
        }

        public void Write(string message)
        {
            writeMessage(message);
        }

        public void WriteLine(string message)
        {
            string output = string.Format("{0}\n", message);
            writeMessage(output);
        }

        private void writeMessage(string message)
        {
            if (OnDebugMessage != null)
            {
                OnDebugMessage.Invoke(this, message);
            }
        }

        public event DebugMessageDelegate OnDebugMessage = null;
    }
}
