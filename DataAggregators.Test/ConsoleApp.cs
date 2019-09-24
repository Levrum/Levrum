using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Levrum.DataAggregators.Test
{
    class ConsoleApp
    {
        #region Members
        List<string> m_cmdList = new List<string>();
        bool m_keepRunning = true;
        List<string> m_cmdNamespaces = new List<string>();
        string m_readPrompt = "\n> ";
        bool m_echoCmds = false;
        #endregion

        #region Properties
        public List<string> CommandNamespaces { get { return m_cmdNamespaces; } set { m_cmdNamespaces = value; } }

        public string CommandNamespace
        {
            get
            {
                if (CommandNamespaces.Count == 0)
                    return string.Empty;

                return CommandNamespaces[0];
            }
            set
            {
                CommandNamespaces[0] = value;
            }
        }

        public List<string> Commands
        {
            get
            {
                string[] commands = new string[m_cmdList.Count];
                m_cmdList.CopyTo(commands);

                return commands.ToList();
            }
        }

        public string Title { get { return Console.Title; } set { Console.Title = value; } }

        public string ReadPrompt { get { return m_readPrompt; } set { m_readPrompt = value; } }

        public bool EchoCommands { get { return m_echoCmds; } set { m_echoCmds = value; } }
        #endregion

        #region Methods
        public void LoadCommands()
        {
            m_cmdList.Clear();

            if (CommandNamespaces.Count == 0)
                return;

            var commandClasses = from Type t in Assembly.GetExecutingAssembly().GetTypes()
                                 where t.IsClass && CommandNamespaces.Contains(t.Namespace)
                                 select t;

            foreach (var commandClass in commandClasses)
            {
                MethodInfo[] methods = commandClass.GetMethods(BindingFlags.Static | BindingFlags.Public);
                foreach (var method in methods)
                {
                    m_cmdList.Add(method.Name);
                }
            }
        }

        public void Run(string _inputStr = "")
        {
            if (!string.IsNullOrEmpty(_inputStr))
            {
                try
                {
                    ConsoleCommand command = new ConsoleCommand(_inputStr);
                    Execute(command);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("**Exception: {0}", exc.Message);
                }
                return;
            }

            while (m_keepRunning)
            {
                string inputStr = ReadFromConsole();
                if (string.IsNullOrWhiteSpace(inputStr))
                    continue;

                try
                {
                    ConsoleCommand command = new ConsoleCommand(inputStr);

                    Execute(command);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("**Exception: {0}", exc.Message);
                }
            }
        }

        public void End()
        {
            m_keepRunning = false;
        }

        public string ReadFromConsole(string promptMessage = "")
        {
            Console.Write(promptMessage + ReadPrompt);
            return Console.ReadLine();
        }

        public void Execute(ConsoleCommand command)
        {
            int argCount = command.Arguments.Count;
            if (!m_cmdList.Contains(command.Name))
            {
                throw new NotImplementedException(string.Format("Invalid command \"{0}\".\nType \"help\" for a list of commands.", command.Name));
            }

            #region Echo Command and Arguments
            if (EchoCommands)
            {
                Console.WriteLine("Received cmd {0}", command.Name);
                if (argCount > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(command.Arguments[0]);
                    for (int i = 1; i < argCount; i++)
                    {
                        sb.Append(string.Format("    {0}", command.Arguments[i]));
                    }
                    Console.WriteLine("  {0} args:  {1}", argCount, sb.ToString());
                }
            }
            #endregion

            Assembly current = GetType().Assembly;

            foreach (string ns in CommandNamespaces)
            {
                Type commandLibraryClass = current.GetType(ns + ".ConsoleCommands");
                object[] args = { command.Arguments };

                if (commandLibraryClass.GetMember(command.Name, BindingFlags.Static | BindingFlags.Public).Length == 0)
                    continue;

                try
                {
                    var result = commandLibraryClass.InvokeMember(command.Name, BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, args);

                    Console.WriteLine(result.ToString());
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
        }
        #endregion
    }

    class ConsoleCommand
    {
        public ConsoleCommand(string input)
        {
            string[] inputArgs = Regex.Split(input, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            Arguments = new List<string>();

            Name = inputArgs[0].ToLower();
            if (Name.Contains('.'))
            {
                string[] pieces = Name.Split('.');
                if (pieces.Length > 2)
                {
                    throw new ArgumentException("Invalid input: {0}. Valid format is \"[namespace.]command [arguments]\"", input);
                }

                Namespace = pieces[0];
                Name = pieces[1];
            }

            for (int i = 1; i < inputArgs.Length; i++)
            {
                string innerString = inputArgs[i];
                if (innerString.StartsWith("\"") && innerString.EndsWith("\""))
                    innerString = innerString.TrimStart('"').TrimEnd('"');

                Arguments.Add(innerString);
            }
        }

        public string Namespace { get; protected set; }

        public string Name { get; protected set; }

        public List<string> Arguments { get; protected set; }
    }
}