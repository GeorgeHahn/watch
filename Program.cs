using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace watch
{
    class Program
    {
        // Expected use: 'watch make'
        static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new ApplicationException("Expected at least one argument");

            List<string> argsList = new List<string>(args);
            argsList.RemoveAt(0);

            var watcher = new Watcher(args[0], Directory.GetCurrentDirectory(), string.Join(" ", argsList));
            watcher.Enable();

            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Enter && key.Modifiers == ConsoleModifiers.Control)
                    watcher.ExecuteImmediately();
                else if (key.Key == ConsoleKey.D && key.Modifiers == ConsoleModifiers.Control)
                    watcher.TerminateProcess();
                else
                    watcher.KeyPressed(key);
            }
        }
    }

    class Watcher
    {
        private readonly string _cmdline;
        private readonly string _cmdargs;
        private readonly FileSystemWatcher _watcher;
        private readonly string[] _extensions;
        private Process _process;
        private DateTime last = DateTime.Now - TimeSpan.FromSeconds(1);

        public Watcher(string commandLine, string path, string arguments = "", string[] extensions = null)
        {
            _cmdline = commandLine;
            _cmdargs = arguments;
            if (extensions == null)
                extensions = new[] {"cpp", "c", "h", ""};
            _extensions = extensions;
            _watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*.*"
            };
            _watcher.Changed += RunCommand;
        }

        public void Enable()
        {
            _watcher.EnableRaisingEvents = true;
        }

        private void RunCommand(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (DateTime.Now - TimeSpan.FromSeconds(1) < last)
                return;
            last = DateTime.Now;
            Console.Clear();
            Console.WriteLine($"\nChanged: {fileSystemEventArgs.FullPath}");
            var extension = Path.GetExtension(fileSystemEventArgs.FullPath);
            if (extension.Length > 0 && extension[0] == '.')
                extension = extension.Remove(0, 1);
            if (!_extensions.Contains(extension))
                return;
            ExecuteImmediately();
        }

        public void ExecuteImmediately()
        {
            TerminateProcess();

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _cmdline,
                    Arguments = _cmdargs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                }
            };
            
            _process.Start();
            new Task(() =>
            {
                while (!_process.StandardOutput.EndOfStream)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(_process.StandardOutput.ReadLine());
                }
            }).Start();

            new Task(() =>
            {
                while (!_process.StandardError.EndOfStream)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(_process.StandardError.ReadLine());
                }
            }).Start();
        }

        public void TerminateProcess()
        {
            if (_process != null && !_process.HasExited)
                _process.Kill();
        }

        public void KeyPressed(ConsoleKeyInfo key)
        {
            if (_process == null || _process.HasExited)
                return;
            _process.StandardInput.Write(key);
        }
    }
}
