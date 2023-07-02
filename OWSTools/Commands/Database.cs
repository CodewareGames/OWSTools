using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DbUp;
using DbUp.Builder;
using DbUp.ScriptProviders;
using DbUp.Helpers;

namespace OWSTools.Commands
{

    [Command("database")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand(typeof(DatabaseMigrate), typeof(DatabaseRun))]
    class Database : OWSCommandBase
    {
        public Database(ILogger<OWSCommand> logger, IConsole console)
        {
            _logger = logger;
            _console = console;
        }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }

        private static string GetVersion()
            => typeof(Database).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }

    public enum Databases { MSSQL, MySQL, Postgres }


    [Command(Name = "migrate", Description = "Run Database Migration")]
    class DatabaseMigrate : OWSCommandBase
    {

        [Option(CommandOptionType.SingleValue, ShortName ="t", LongName = "type", Description = "Database Type", ValueName = "type", ShowInHelpText = true)]
        public Databases Type { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "c", LongName = "connection", Description = "Connection String", ValueName = "string", ShowInHelpText = true)]
        public string Connection { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "s", LongName = "scripts", Description = "Path To Migration Scripts", ValueName = "string", ShowInHelpText = true)]
        public string Scripts { get; set; }


        public DatabaseMigrate(ILogger<Database> logger, IConsole console)
        {
            _logger = logger;
            _console = console;
        }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            if (string.IsNullOrEmpty(Connection) || string.IsNullOrEmpty(Scripts))
            {
                Connection = Prompt.GetString("Connection String:", Connection);
                Scripts = Prompt.GetString("Script Path:", Scripts);
            }

            try
            {
                OutputLine("Running Database Migration For " + Type);
                UpgradeEngineBuilder PreDeploymentScriptsExecutor = new UpgradeEngineBuilder();
                string PreDeploymentScriptsPath = Path.Combine(Scripts, Type.ToString(), "PreDeployment");

                UpgradeEngineBuilder MigrationScriptsExecutor = new UpgradeEngineBuilder();
                string MigrationScriptsPath = Path.Combine(Scripts, Type.ToString(), "Migrations");

                UpgradeEngineBuilder PostDeploymentScriptsExecutor = new UpgradeEngineBuilder();
                string PostDeploymentScriptsPath = Path.Combine(Scripts, Type.ToString(), "PostDeployment");

                if (Type == Databases.MSSQL)
                {
                    PreDeploymentScriptsExecutor = DeployChanges.To.SqlDatabase(Connection).WithScriptsFromFileSystem(PreDeploymentScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());

                    MigrationScriptsExecutor = DeployChanges.To.SqlDatabase(Connection).WithScriptsFromFileSystem(MigrationScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalToSqlTable("dbo", "Schema");

                    PostDeploymentScriptsExecutor = DeployChanges.To.SqlDatabase(Connection).WithScriptsFromFileSystem(PostDeploymentScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());
                }

                if (Type == Databases.MySQL)
                {
                    PreDeploymentScriptsExecutor = DeployChanges.To.MySqlDatabase(Connection).WithScriptsFromFileSystem(PreDeploymentScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());

                    MigrationScriptsExecutor = DeployChanges.To.MySqlDatabase(Connection).WithScriptsFromFileSystem(MigrationScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalToSqlTable("dbo", "Schema");

                    PostDeploymentScriptsExecutor = DeployChanges.To.MySqlDatabase(Connection).WithScriptsFromFileSystem(PostDeploymentScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());
                }

                if (Type == Databases.Postgres)
                {
                    PreDeploymentScriptsExecutor = DeployChanges.To.PostgresqlDatabase(Connection).WithScriptsFromFileSystem(PreDeploymentScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());

                    MigrationScriptsExecutor = DeployChanges.To.PostgresqlDatabase(Connection).WithScriptsFromFileSystem(MigrationScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalToSqlTable("dbo", "Schema");

                    PostDeploymentScriptsExecutor = DeployChanges.To.PostgresqlDatabase(Connection).WithScriptsFromFileSystem(PostDeploymentScriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());
                }

                OutputLine("Executing PreDeployment Scripts...");
                var PreDeploymentUpgradeResult = PreDeploymentScriptsExecutor.Build().PerformUpgrade();
                if (!PreDeploymentUpgradeResult.Successful)
                {
                    Output(PreDeploymentUpgradeResult.Error.ToString(), ConsoleColor.Red);
                    return Task.FromResult(1);
                }
                OutputLine("Success!", ConsoleColor.Green);

                OutputLine("Executing Migration Scripts...");
                var MigrationUpgradeResult = MigrationScriptsExecutor.Build().PerformUpgrade();
                if (!MigrationUpgradeResult.Successful)
                {
                    Output(MigrationUpgradeResult.Error.ToString(), ConsoleColor.Red);
                    return Task.FromResult(1);
                }
                OutputLine("Success!", ConsoleColor.Green);

                OutputLine("Executing PostDeployment Scripts...");
                var PostDeploymentUpgradeResult = PostDeploymentScriptsExecutor.Build().PerformUpgrade();
                if (!PostDeploymentUpgradeResult.Successful)
                {
                    Output(PostDeploymentUpgradeResult.Error.ToString(), ConsoleColor.Red);
                    return Task.FromResult(1);
                }
                OutputLine("Success!", ConsoleColor.Green);

                return Task.FromResult(0);

            }
            catch (Exception ex)
            {
                OnException(ex);
                return Task.FromResult(1);
            }
        }
    }

    [Command(Name = "run", Description = "Run Database Script")]
    class DatabaseRun : OWSCommandBase
    {

        [Option(CommandOptionType.SingleValue, ShortName = "t", LongName = "type", Description = "Database Type", ValueName = "type", ShowInHelpText = true)]
        public Databases Type { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "c", LongName = "connection", Description = "Connection String", ValueName = "string", ShowInHelpText = true)]
        public string Connection { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "s", LongName = "script", Description = "Path To Database Script", ValueName = "string", ShowInHelpText = true)]
        public string Script { get; set; }


        public DatabaseRun(ILogger<Database> logger, IConsole console)
        {
            _logger = logger;
            _console = console;
        }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
   
            if (string.IsNullOrEmpty(Connection) || string.IsNullOrEmpty(Script))
            {
                Connection = Prompt.GetString("Connection String:", Connection);
                Script = Prompt.GetString("Script Path:", Script);
            }

            try
            {
                OutputLine("Running Database Script For " + Type);
                UpgradeEngineBuilder DatabaseFileExecutor = new UpgradeEngineBuilder();
                string DatabaseFilePath = Path.Combine(Script);


                if (Type == Databases.MSSQL)
                {
                    DatabaseFileExecutor = DeployChanges.To.SqlDatabase(Connection).WithScriptsFromFileSystem(DatabaseFilePath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());
                }

                if (Type == Databases.MySQL)
                {
                    DatabaseFileExecutor = DeployChanges.To.MySqlDatabase(Connection).WithScriptsFromFileSystem(DatabaseFilePath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());
                }

                if (Type == Databases.Postgres)
                {
                    DatabaseFileExecutor = DeployChanges.To.PostgresqlDatabase(Connection).WithScriptsFromFileSystem(DatabaseFilePath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true,
                    }).LogToConsole().JournalTo(new NullJournal());
                }

                OutputLine("Executing Database Script...");
                var DatabaseFileResult = DatabaseFileExecutor.Build().PerformUpgrade();
                if (!DatabaseFileResult.Successful)
                {
                    OutputLine(DatabaseFileResult.Error.ToString(), ConsoleColor.Red);
                    return Task.FromResult(1);
                }
                OutputLine("Success!", ConsoleColor.Green);

                return Task.FromResult(0);

            }
            catch (Exception ex)
            {
                OnException(ex);
                return Task.FromResult(1);
            }
        }
    }
}