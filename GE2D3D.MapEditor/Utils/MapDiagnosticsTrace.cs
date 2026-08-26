using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GE2D3D.MapEditor.Utils
{
    public enum MapDiagnosticLevel
    {
        Info,
        Success,
        Warning,
        Error,
        Parser,
        Asset,
        Renderer,
        Performance
    }

    public sealed class MapDiagnosticEntry
    {
        public DateTime Timestamp { get; private set; }
        public TimeSpan Elapsed { get; private set; }
        public MapDiagnosticLevel Level { get; private set; }
        public string Message { get; private set; }
        public string Detail { get; private set; }

        public MapDiagnosticEntry(DateTime timestamp, TimeSpan elapsed, MapDiagnosticLevel level, string message, string detail)
        {
            Timestamp = timestamp;
            Elapsed = elapsed;
            Level = level;
            Message = message ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public override string ToString()
        {
            var elapsed = Elapsed.TotalMilliseconds.ToString("000000.000");
            var line = "[" + elapsed + " ms] " + Level.ToString().ToUpperInvariant().PadRight(11) + Message;
            if (!string.IsNullOrWhiteSpace(Detail))
                line += " | " + Detail;
            return line;
        }
    }

    /// <summary>
    /// Central in-memory trace for map loading and parsing. It is intentionally
    /// independent from the normal editor Logger so the diagnostics window can
    /// show an ordered, copyable trace of exactly what the map pipeline did.
    /// </summary>
    public static class MapDiagnosticsTrace
    {
        private static readonly object Sync = new object();
        private static readonly List<MapDiagnosticEntry> EntriesInternal = new List<MapDiagnosticEntry>();
        private static readonly Stopwatch Stopwatch = new Stopwatch();
        private static DateTime _startedAt;
        private static string _currentMapPath = string.Empty;

        public static event EventHandler TraceChanged;

        public static void Begin(string mapPath)
        {
            lock (Sync)
            {
                EntriesInternal.Clear();
                Stopwatch.Reset();
                Stopwatch.Start();
                _startedAt = DateTime.Now;
                _currentMapPath = mapPath ?? string.Empty;
                AddLocked(MapDiagnosticLevel.Info, "Map diagnostic session started", _currentMapPath);
            }
            RaiseTraceChanged();
        }

        public static void Log(MapDiagnosticLevel level, string message, string detail)
        {
            lock (Sync)
            {
                if (!Stopwatch.IsRunning)
                {
                    Stopwatch.Start();
                    _startedAt = DateTime.Now;
                }
                AddLocked(level, message, detail);
            }
            RaiseTraceChanged();
        }

        public static void Info(string message, string detail = "") { Log(MapDiagnosticLevel.Info, message, detail); }
        public static void Success(string message, string detail = "") { Log(MapDiagnosticLevel.Success, message, detail); }
        public static void Warning(string message, string detail = "") { Log(MapDiagnosticLevel.Warning, message, detail); }
        public static void Error(string message, string detail = "") { Log(MapDiagnosticLevel.Error, message, detail); }
        public static void Parser(string message, string detail = "") { Log(MapDiagnosticLevel.Parser, message, detail); }
        public static void Asset(string message, string detail = "") { Log(MapDiagnosticLevel.Asset, message, detail); }
        public static void Renderer(string message, string detail = "") { Log(MapDiagnosticLevel.Renderer, message, detail); }
        public static void Performance(string message, string detail = "") { Log(MapDiagnosticLevel.Performance, message, detail); }

        public static List<MapDiagnosticEntry> GetSnapshot()
        {
            lock (Sync)
            {
                return new List<MapDiagnosticEntry>(EntriesInternal);
            }
        }

        public static string CurrentMapPath
        {
            get { lock (Sync) { return _currentMapPath; } }
        }

        public static DateTime StartedAt
        {
            get { lock (Sync) { return _startedAt; } }
        }

        public static TimeSpan Elapsed
        {
            get { lock (Sync) { return Stopwatch.Elapsed; } }
        }

        public static void Clear()
        {
            lock (Sync)
            {
                EntriesInternal.Clear();
                Stopwatch.Reset();
                _startedAt = DateTime.MinValue;
                _currentMapPath = string.Empty;
            }
            RaiseTraceChanged();
        }

        public static string BuildTextReport()
        {
            var snapshot = GetSnapshot();
            var builder = new StringBuilder();
            builder.AppendLine("LIVE MAP LOAD TRACE");
            builder.AppendLine(new string('=', 88));
            builder.AppendLine("Map: " + (string.IsNullOrWhiteSpace(CurrentMapPath) ? "<none>" : CurrentMapPath));
            builder.AppendLine("Started: " + (StartedAt == DateTime.MinValue ? "<not started>" : StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            builder.AppendLine("Elapsed: " + Elapsed.TotalMilliseconds.ToString("F1") + " ms");
            builder.AppendLine(new string('-', 88));

            foreach (var entry in snapshot)
                builder.AppendLine(entry.ToString());

            return builder.ToString();
        }

        private static void AddLocked(MapDiagnosticLevel level, string message, string detail)
        {
            EntriesInternal.Add(new MapDiagnosticEntry(DateTime.Now, Stopwatch.Elapsed, level, message, detail));
        }

        private static void RaiseTraceChanged()
        {
            var handler = TraceChanged;
            if (handler != null)
                handler(null, EventArgs.Empty);
        }
    }
}
