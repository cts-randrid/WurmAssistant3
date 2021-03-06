﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AldursLab.Essentials;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace AldursLab.WurmAssistant3.Areas.Logging
{
    public class LoggingConfig : ILoggingConfig
    {
        readonly LoggingConfiguration _config;
        string _currentReadableLogFileFullPath;
        string _currentVerboseLogFileFullPath;

        public LoggingConfig(string logOutputDirFullPath)
        {
            LogsDirectoryFullPath = logOutputDirFullPath ?? throw new ArgumentNullException(nameof(logOutputDirFullPath));

            _config = new LoggingConfiguration();

            SetupReadableLogging(LogsDirectoryFullPath);
            SetupVerboseLogging(LogsDirectoryFullPath);

            ApplyLoggingConfig();
        }

        public string LogsDirectoryFullPath { get; }

        private void SetupReadableLogging(string logOutputDirFullPath)
        {
            logOutputDirFullPath = Path.Combine(logOutputDirFullPath, "Readable");

            var currentLogDir = Path.Combine(logOutputDirFullPath);
            if (!Directory.Exists(currentLogDir))
                Directory.CreateDirectory(currentLogDir);

            var archiveLogDir = Path.Combine(currentLogDir, "Archive");
            if (!Directory.Exists(archiveLogDir))
                Directory.CreateDirectory(archiveLogDir);

            MoveOldLogToArchive(currentLogDir, archiveLogDir);
            TrimOlgLogFiles(archiveLogDir, maxMegabytes: 50);

            var currentFileName = Time.Get.LocalNow.ToString("yyy-MM-dd_HH-mm-ss-ffffff") + ".txt";
            _currentReadableLogFileFullPath = Path.Combine(currentLogDir, currentFileName);
            var fileTarget = new FileTarget
            {
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                KeepFileOpen = true,
                FileName = GetCurrentReadableLogFileFullPath(),
                Layout = "${date:universalTime=true} > ${level} > ${logger:shortName=true} > ${message}${onexception:inner= > ${exception:format=Message:maxInnerExceptionLevel=1:innerFormat=Message}}"
            };

            var globalrule = new LoggingRule("*", LogLevel.Debug, fileTarget);
            _config.LoggingRules.Add(globalrule);
        }

        private void SetupVerboseLogging(string logOutputDirFullPath)
        {
            logOutputDirFullPath = Path.Combine(logOutputDirFullPath, "Verbose");

            var currentLogDir = Path.Combine(logOutputDirFullPath);
            if (!Directory.Exists(currentLogDir)) Directory.CreateDirectory(currentLogDir);

            var archiveLogDir = Path.Combine(currentLogDir, "Archive");
            if (!Directory.Exists(archiveLogDir)) Directory.CreateDirectory(archiveLogDir);

            MoveOldLogToArchive(currentLogDir, archiveLogDir);
            TrimOlgLogFiles(archiveLogDir, maxMegabytes: 50);

            var currentFileName = Time.Get.LocalNow.ToString("yyy-MM-dd_HH-mm-ss-ffffff") + ".txt";
            _currentVerboseLogFileFullPath = Path.Combine(currentLogDir, currentFileName);
            var fileTarget = new FileTarget
            {
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                KeepFileOpen = true,
                FileName = GetCurrentVerboseLogFileFullPath(),
                Layout = "${date:universalTime=true}|${level}|${logger}|${message}${onexception:inner=|${exception:format=ToString}}"
            };

            var globalrule = new LoggingRule("*", LogLevel.Debug, fileTarget);
            _config.LoggingRules.Add(globalrule);
        }

        private void ApplyLoggingConfig()
        {
            LogManager.Configuration = _config;
        }

        public string GetCurrentReadableLogFileFullPath()
        {
            if (string.IsNullOrWhiteSpace(_currentReadableLogFileFullPath))
            {
                throw new InvalidOperationException("Logging is not configured");
            }
            return _currentReadableLogFileFullPath;
        }

        public string GetCurrentVerboseLogFileFullPath()
        {
            if (string.IsNullOrWhiteSpace(_currentVerboseLogFileFullPath))
            {
                throw new InvalidOperationException("Logging is not configured");
            }
            return _currentVerboseLogFileFullPath;
        }

        private void TrimOlgLogFiles(string archiveLogDir, int maxMegabytes)
        {
            var files = new DirectoryInfo(archiveLogDir).GetFiles();
            if (files.Any())
            {
                var totalSize = files.Sum(info => info.Length);
                if (totalSize > ((long)maxMegabytes) * 1024L * 1024L)
                {
                    var fileToDelete = files.First(file => file.CreationTime == files.Min(info => info.CreationTime));
                    TryDeleteFile(fileToDelete);
                    TrimOlgLogFiles(archiveLogDir, maxMegabytes);
                }
            }
        }

        private void MoveOldLogToArchive(string currentLogDir, string archiveLogDir)
        {
            var currentFiles = new DirectoryInfo(currentLogDir).GetFiles();
            foreach (var file in currentFiles)
            {
                var newPath = Path.Combine(archiveLogDir, file.Name);
                var uniqueNewPath = GetNewUniqueName(newPath);
                TryMoveFile(file, uniqueNewPath);
            }
        }

        private string GetNewUniqueName(string newPath, int counter = 1)
        {
            var file = new FileInfo(newPath);
            if (file.Exists)
            {
                var dirPath = file.DirectoryName;
                var rawFileName = Path.GetFileNameWithoutExtension(file.Name);
                if (rawFileName.EndsWith("(" + (counter - 1) + ")"))
                {
                    rawFileName = rawFileName.Substring(0, rawFileName.Length - 3);
                }
                var newFileName = rawFileName + "(" + counter + ")" + file.Extension;
                Debug.Assert(dirPath != null, "dirPath != null");
                newPath = Path.Combine(dirPath, newFileName);
                counter++;
                return GetNewUniqueName(newPath, counter);
            }
            return newPath;
        }

        private void TryDeleteFile(FileInfo file)
        {
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void TryMoveFile(FileInfo file, string targetPath)
        {
            try
            {
                file.MoveTo(targetPath);
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
