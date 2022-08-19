using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImprovedHordes.Command
{
    internal abstract class ConsoleCommandBase : CommandBase, IConsoleCommand
    {
        private string prefix;
        private string[] aliases;

        public ConsoleCommandBase(string name, string prefix, params string[] aliases) : base(name)
        {
            this.prefix = prefix;
            this.aliases = aliases;
        }

        public virtual bool IsExecuteOnClient => false;

        public virtual int DefaultPermissionLevel => 0;

        public virtual bool AllowedInMainMenu => false;

        public void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            string message = "";
            if(!CallSubcommand(_params, _senderInfo, ref message))
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"{prefix} {message}");
            }
        }

        public string[] GetCommands()
        {
            return new string[] { this.GetName() }.Concat(aliases).ToArray();
        }

        public abstract string GetDescription();

        public string GetHelp()
        {
            string message = "";
            CallSubcommand("help", new CommandSenderInfo(), ref message, new List<string>());

            return message;
        }
    }

    internal class CommandBase
    {
        protected readonly Dictionary<string, SubcommandBase> subcommands;
        protected string name;

        public CommandBase(string name)
        {
            this.subcommands = new Dictionary<string, SubcommandBase>();
            this.name = name;

            if(!name.EqualsCaseInsensitive("help"))
                RegisterSubcommand(new HelpSubcommand());
        }

        class HelpSubcommand : ExecutableSubcommandBase
        {
            public HelpSubcommand() : base("help")
            {
            }

            public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
            {
                if(parent is ExecutableSubcommandBase)
                {

                }
                else
                {
                    StringBuilder builder = new StringBuilder();

                    builder.AppendLine("Commands:");

                    foreach (var subcommand in parent.subcommands)
                    {
                        builder.Append($"{GetPathToRoot()} {subcommand.Key} ");

                        if(subcommand.Value is ExecutableSubcommandBase esb && esb.GetArgs() != null)
                        {
                            for(int argIndex = 0; argIndex < esb.GetArgs().Length; argIndex++)
                            {
                                var arg = esb.GetArgs()[argIndex];
                                bool optional = arg.optional;

                                if (optional && argIndex != esb.GetArgs().Length - 1)
                                    optional = false;

                                if (optional)
                                    builder.Append("(");
                                else
                                    builder.Append("<");

                                builder.Append($"{arg.name}");

                                if (optional)
                                    builder.Append(")");
                                else
                                    builder.Append(">");

                                builder.Append(" ");
                            }
                        }

                        builder.AppendLine($"- {subcommand.Value.GetDescription()}");
                    }

                    message = builder.ToString();
                }

                return false;
            }

            public override (string name, bool optional)[] GetArgs()
            {
                return null;
            }

            public override string GetDescription()
            {
                return "Opens this menu.";
            }
        }

        public string GetName()
        {
            return this.name;
        }

        public void RegisterSubcommand(SubcommandBase subcommand)
        {
            if (!subcommands.ContainsKey(subcommand.name))
            {
                subcommands.Add(subcommand.name, subcommand);
                subcommand.parent = this;
            }
            else
            {
                throw new ArgumentException($"A subcommand with the name {subcommand.name} already exists.");
            }
        }

        public bool CallSubcommand(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            if(args.Count == 0 || subcommands.Count == 0)
            {
                if(!(this is IExecutableCommand iec))
                {
                    if (subcommands.Count > 0)
                    {
                        return CallSubcommand("help", _senderInfo, ref message, new List<string>());
                    }
                    else
                    {
                        message = "This command is not fully implemented.";
                    }

                    return false;
                }
                else
                {
                    return iec.Execute(ref message, _senderInfo, args);
                }
            }
            else
            {
                string subcommand = args[0];

                if(!subcommands.ContainsKey(subcommand))
                {
                    message = $"No such subcommand with name \'{subcommand}\' exists.";
                    return false;
                }
                else
                {
                    args.RemoveAt(0);
                    return CallSubcommand(subcommand, _senderInfo, ref message, args);
                }
            }
        }

        protected bool CallSubcommand(string subcommand, CommandSenderInfo _senderInfo, ref string message, List<string> args)
        {
            SubcommandBase command = subcommands[subcommand];

            if (command is ExecutableSubcommandBase)
                return ((ExecutableSubcommandBase)command).Execute(args, _senderInfo, ref message);
            else
                return command.CallSubcommand(args, _senderInfo, ref message);
        }
    }

    internal interface IExecutableCommand
    {
        bool Execute(ref string message, CommandSenderInfo _senderInfo, List<string> args);
    }

    internal abstract class SubcommandBase : CommandBase
    {
        internal CommandBase parent;

        protected SubcommandBase(string name) : base(name)
        {
        }

        public abstract string GetDescription();

        protected string GetPathToRoot()
        {
            string path = parent.GetName();

            CommandBase root = parent;
            while(root is SubcommandBase scb)
            {
                path = scb.parent.GetName() + " " + path;
                root = scb.parent;
            }

            return path;
        }
    }

    internal abstract class ExecutableSubcommandBase : SubcommandBase
    {
        public ExecutableSubcommandBase(string name) : base(name)
        {
        }

        public abstract bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message);

        public abstract (string name, bool optional)[] GetArgs();
    }
}