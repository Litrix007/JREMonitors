using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace JREMonitors.BveEx.Utils
{
    public sealed class ConfigFileWatcher : IDisposable
    {
        private const int DebounceMs = 500;

        private readonly Timer _debounceTimer;

        private readonly Dictionary<string, FileSystemWatcher> _watchers =
            new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

        private volatile bool _disposed;
        private Action _onReloadRequested;

        public ConfigFileWatcher(IEnumerable<string> paths, Action onReloadRequested)
        {
            _onReloadRequested = onReloadRequested ??
                                 throw new ArgumentNullException(nameof(onReloadRequested));
            EnsureWatchers(NormalizePaths(paths));
            _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var watcher in _watchers.Values) DisposeWatcher(watcher);
            _watchers.Clear();
            _debounceTimer.Dispose();
            _onReloadRequested = null;
        }

        public void UpdatePaths(IEnumerable<string> paths)
        {
            if (_disposed) return;
            var desired = NormalizePaths(paths);
            var toRemove = _watchers.Keys.Where(k => !desired.Contains(k)).ToList();
            foreach (var key in toRemove)
            {
                DisposeWatcher(_watchers[key]);
                _watchers.Remove(key);
            }

            EnsureWatchers(desired);
        }

        private static HashSet<string> NormalizePaths(IEnumerable<string> paths)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                try
                {
                    set.Add(Path.GetFullPath(path));
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return set;
        }

        private void EnsureWatchers(HashSet<string> fullPaths)
        {
            foreach (var fullPath in fullPaths)
            {
                if (_watchers.ContainsKey(fullPath)) continue;
                var directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) continue;
                var fileName = Path.GetFileName(fullPath);
                var watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                   NotifyFilters.LastWrite | NotifyFilters.Size |
                                   NotifyFilters.CreationTime,
                    IncludeSubdirectories = false
                };
                watcher.Changed += OnFileChanged;
                watcher.Created += OnFileChanged;
                watcher.Renamed += OnFileChanged;
                watcher.EnableRaisingEvents = true;
                _watchers[fullPath] = watcher;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_disposed) return;
            _debounceTimer.Change(DebounceMs, Timeout.Infinite);
        }

        private void OnDebounceElapsed(object state)
        {
            if (_disposed) return;
            try
            {
                _onReloadRequested?.Invoke();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void DisposeWatcher(FileSystemWatcher watcher)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnFileChanged;
                watcher.Created -= OnFileChanged;
                watcher.Renamed -= OnFileChanged;
                watcher.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}