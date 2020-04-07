namespace PdbSourceIndexer
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    internal class Program
    {
        private class SourceServerProviderInfo
        {
            public readonly Type ProviderType;
            public readonly Dictionary<string, PropertyInfo> Options;
            public SourceServerProviderInfo(Type providerType)
            {
                this.ProviderType = providerType;
                this.Options = new Dictionary<string, PropertyInfo>();
            }
        }

        private Dictionary<string, SourceServerProviderInfo> _providers;

        static int Main(string[] args)
        {
            return new Program().Run(args);
        }

        private static bool IsBrowsable(Type t)
        {
            bool isNotBrowsable = t.GetCustomAttributes(true)
                .OfType<BrowsableAttribute>()
                .Select(attr => !attr.Browsable)
                .FirstOrDefault();
            return !isNotBrowsable;
        }

        private static Dictionary<string, SourceServerProviderInfo> FindProviders()
        {
            return typeof(Program).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(SourceServerProvider).IsAssignableFrom(t) && IsBrowsable(t))
                .Select(t => new SourceServerProviderInfo(t))
                .ToDictionary(i =>
                {
                    string key = i.ProviderType.Name;
                    const string suffix = "SourceServerProvider";
                    if (key.EndsWith(suffix))
                    {
                        key = key.Substring(0, key.Length - suffix.Length);
                    }
                    return key.ToLowerInvariant();
                });
        }

        private int Run(string[] args)
        {
            var rootCommand = new RootCommand()
            {
                new Option("--tools-path", "Debugging Tools for Windows installation path.")
                {
                    Argument = new Argument<DirectoryInfo>().ExistingOnly()
                },

                new Option("--source-root", "Source files root path.")
                {
                    Argument = new Argument<DirectoryInfo>().ExistingOnly()
                },

                new Option("--symbol-root", "Symbol files root path.")
                {
                    Argument = new Argument<DirectoryInfo>().ExistingOnly()
                },

                new Option("--recursive", "Search symbol files recursively.")
                {
                    Argument = new Argument<bool>()
                }
            };

            _providers = FindProviders();
            foreach (var pair in _providers)
            {
                var command = new Command(pair.Key);

                var providerInfo = pair.Value;
                var providerType = providerInfo.ProviderType;
                var properties = providerType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Select(p => (Property: p, Option: p.GetCustomAttribute<OptionAttribute>()))
                    .Where(x => x.Option != null);

                foreach (var property in properties)
                {
                    var option = new Option(property.Option.Alias, property.Option.Description);
                    option.Argument = new Argument()
                    {
                        ArgumentType = property.Property.PropertyType
                    };
                    providerInfo.Options.Add(property.Option.Alias.TrimStart('-'), property.Property);
                    command.AddOption(option);
                }

                rootCommand.AddCommand(command);
                command.Handler = CommandHandler.Create<ParseResult>(Run);
            }

            rootCommand.TreatUnmatchedTokensAsErrors = true;
            rootCommand.Handler = CommandHandler.Create<ParseResult>(Run);

            return rootCommand.Invoke(args);
        }

        private int Run(ParseResult result)
        {
            var indexer = new SourceIndexer();

            indexer.DebuggingToolsPath = result.RootCommandResult.ValueForOption<DirectoryInfo>("--tools-path");
            indexer.SourceRootPath = result.RootCommandResult.ValueForOption<DirectoryInfo>("--source-root");

            bool recursive = result.RootCommandResult.ValueForOption<bool>("--recursive");

            var symbolRoot = result.RootCommandResult.ValueForOption<DirectoryInfo>("--symbol-root") ?? new DirectoryInfo(".");
            indexer.SymbolFiles = symbolRoot.EnumerateFiles("*.pdb", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            var providerInfo = _providers[result.CommandResult.Command.Name];
            var provider = (SourceServerProvider)Activator.CreateInstance(providerInfo.ProviderType);
            foreach (var option in result.CommandResult.Children)
            {
                string alias = option.Symbol.Aliases[0];
                var value = result.ValueForOption(alias);
                providerInfo.Options[alias].SetValue(provider, value);
            }

            indexer.Log = new ConsoleLogger();
            indexer.SourceServerProvider = provider;

            try
            {
                indexer.Run();
                return 0;
            }
            catch (Exception ex)
            {
                indexer.Log.Error(ex.Message);
                return 1;
            }
        }
    }
}
