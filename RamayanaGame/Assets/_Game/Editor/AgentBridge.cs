using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Ramayana.EditorTools
{
    // Lets tools outside Unity run whitelisted commands in the open editor by writing Temp/AgentBridge/command.txt:
    //   refresh | play | stop | menu:Ramayana/...
    [InitializeOnLoad]
    static class AgentBridge
    {
        static readonly string Dir = Path.GetFullPath("Temp/AgentBridge");
        static string CommandFile => Path.Combine(Dir, "command.txt");
        static string ResultFile => Path.Combine(Dir, "result.txt");
        static string ConsoleFile => Path.Combine(Dir, "console.log");
        static double nextPoll;

        static AgentBridge()
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(Path.Combine(Dir, "ready.txt"), DateTime.Now.ToString("o"));
            EditorApplication.update += Poll;
            Application.logMessageReceivedThreaded += OnLog;
            CompilationPipeline.assemblyCompilationFinished += OnCompiled;
        }

        static void OnLog(string message, string stackTrace, LogType type)
        {
            string line = $"{DateTime.Now:HH:mm:ss} [{type}] {message}";
            if (type == LogType.Exception || type == LogType.Error) line += "\n" + stackTrace;
            lock (Dir) File.AppendAllText(ConsoleFile, line + "\n");
        }

        static void OnCompiled(string assembly, CompilerMessage[] messages)
        {
            foreach (var m in messages)
                OnLog($"{m.file}({m.line}): {m.message}", "", m.type == CompilerMessageType.Error ? LogType.Error : LogType.Warning);
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(CommandFile)) return;

            string command = File.ReadAllText(CommandFile).Trim();
            File.Delete(CommandFile);

            string result;
            try { result = Run(command); }
            catch (Exception e) { result = "ERROR " + e; }
            File.WriteAllText(ResultFile, $"{DateTime.Now:o}\n{command}\n{result}\n");
        }

        static string Run(string command)
        {
            switch (command)
            {
                case "refresh": AssetDatabase.Refresh(); return "OK";
                case "play": EditorApplication.isPlaying = true; return "OK";
                case "stop": EditorApplication.isPlaying = false; return "OK";
            }
            if (command.StartsWith("menu:"))
            {
                string path = command.Substring(5);
                if (!path.StartsWith("Ramayana/")) return "REJECTED: only Ramayana/ menu items are allowed";
                return EditorApplication.ExecuteMenuItem(path) ? "OK" : "FAILED: menu item not found";
            }
            return "UNKNOWN COMMAND";
        }
    }
}
