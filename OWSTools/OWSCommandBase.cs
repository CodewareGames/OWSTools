using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace OWSTools
{
    [HelpOption("--help")]
    abstract class OWSCommandBase
    {

        protected ILogger _logger;
        protected IConsole _console;

        protected string FileNameSuffix { get; set; }

        protected virtual Task<int> OnExecute(CommandLineApplication app)
        {
            return Task.FromResult(0);
        }

        protected void OnException(Exception ex)
        {
            OutputError(ex.Message);
            _logger.LogError(ex.Message);
            _logger.LogDebug(ex, ex.Message);
        }

        protected void Output(string data, ConsoleColor color = ConsoleColor.White)
        {
            _console.BackgroundColor = ConsoleColor.Black;
            _console.ForegroundColor = color;
            _console.Out.Write(data);
            _console.ResetColor();
        }

        protected void OutputLine(string data, ConsoleColor color = ConsoleColor.White)
        {
            _console.BackgroundColor = ConsoleColor.Black;
            _console.ForegroundColor = color;
            _console.Out.WriteLine(data);
            _console.ResetColor();
        }

        protected void OutputError(string message, ConsoleColor color = ConsoleColor.White)
        {
            _console.BackgroundColor = ConsoleColor.Red;
            _console.ForegroundColor = color;
            _console.Error.WriteLine(message);
            _console.ResetColor();
        }
    }
}