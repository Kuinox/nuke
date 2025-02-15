﻿// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Common.OutputSinks
{
    [PublicAPI]
    public abstract class OutputSink
    {
        public static OutputSink Default
        {
            get
            {
                var term = Environment.GetEnvironmentVariable("TERM");
                return term == null || !term.StartsWithOrdinalIgnoreCase("xterm")
                    ? (OutputSink) new SystemColorOutputSink()
                    : new AnsiColorOutputSink();
            }
        }

        internal readonly List<Tuple<LogLevel, string>> SevereMessages = new List<Tuple<LogLevel, string>>();

        internal virtual IDisposable WriteBlock(string text)
        {
            return DelegateDisposable.CreateBracket(
                () =>
                {
                    var formattedBlockText = FormatBlockText(text)
                        .Split(new[] { EnvironmentInfo.NewLine }, StringSplitOptions.None);

                    Console.WriteLine();
                    Console.WriteLine("╬" + new string(c: '═', text.Length + 5));
                    formattedBlockText.ForEach(x => Console.WriteLine($"║ {x}"));
                    Console.WriteLine("╬" + new string(c: '═', Math.Max(text.Length - 4, 2)));
                    Console.WriteLine();
                });
        }

        protected internal void WriteLogo()
        {
            Logger.Normal("███╗   ██╗██╗   ██╗██╗  ██╗███████╗");
            Logger.Normal("████╗  ██║██║   ██║██║ ██╔╝██╔════╝");
            Logger.Normal("██╔██╗ ██║██║   ██║█████╔╝ █████╗  ");
            Logger.Normal("██║╚██╗██║██║   ██║██╔═██╗ ██╔══╝  ");
            Logger.Normal("██║ ╚████║╚██████╔╝██║  ██╗███████╗");
            Logger.Normal("╚═╝  ╚═══╝ ╚═════╝ ╚═╝  ╚═╝╚══════╝");
        }

        internal virtual void WriteSummary(NukeBuild build)
        {
            if (SevereMessages.Count > 0)
            {
                WriteNormal();
                WriteSevereMessages();
            }

            WriteNormal();
            WriteSummaryTable(build);
            WriteNormal();

            if (build.IsSuccessful)
                WriteSuccessfulBuild();
            else
                WriteFailedBuild();
        }

        protected virtual void WriteSuccessfulBuild()
        {
            WriteSuccess($"Build succeeded on {DateTime.Now.ToString(CultureInfo.CurrentCulture)}. ＼（＾ᴗ＾）／");
        }

        protected virtual void WriteFailedBuild()
        {
            WriteError($"Build failed on {DateTime.Now.ToString(CultureInfo.CurrentCulture)}. (╯°□°）╯︵ ┻━┻");
        }

        protected virtual void WriteSummaryTable(NukeBuild build)
        {
            var firstColumn = Math.Max(build.ExecutionPlan.Max(x => x.Name.Length) + 4, val2: 19);
            var secondColumn = 10;
            var thirdColumn = 10;
            var allColumns = firstColumn + secondColumn + thirdColumn;
            var totalDuration = build.ExecutionPlan.Aggregate(TimeSpan.Zero, (t, x) => t.Add(x.Duration));

            string CreateLine(string target, string executionStatus, string duration, string information = null)
                => target.PadRight(firstColumn, paddingChar: ' ')
                   + executionStatus.PadRight(secondColumn, paddingChar: ' ')
                   + duration.PadLeft(thirdColumn, paddingChar: ' ')
                   + (information != null ? $"   // {information}" : string.Empty);

            static string GetDurationOrBlank(ExecutableTarget target)
                => target.Status == ExecutionStatus.Succeeded ||
                   target.Status == ExecutionStatus.Failed ||
                   target.Status == ExecutionStatus.Aborted
                    ? GetDuration(target.Duration)
                    : string.Empty;

            static string GetDuration(TimeSpan duration)
                => $"{(int) duration.TotalMinutes}:{duration:ss}".Replace("0:00", "< 1sec");

            static string GetInformation(ExecutableTarget target)
                => target.SummaryInformation.Any()
                    ? target.SummaryInformation.Select(x => $"{x.Key}: {x.Value}").JoinComma()
                    : null;

            WriteNormal(new string(c: '═', count: allColumns));
            WriteInformation(CreateLine("Target", "Status", "Duration"));
            //WriteInformationInternal($"{{0,-{firstColumn}}}{{1,-{secondColumn}}}{{2,{thirdColumn}}}{{3,1}}", "Target", "Status", "Duration", "Test");
            WriteNormal(new string(c: '─', count: allColumns));
            foreach (var target in build.ExecutionPlan)
            {
                var line = CreateLine(target.Name, target.Status.ToString(), GetDurationOrBlank(target), GetInformation(target));
                switch (target.Status)
                {
                    case ExecutionStatus.Skipped:
                        WriteNormal(line);
                        break;
                    case ExecutionStatus.Succeeded:
                        WriteSuccess(line);
                        break;
                    case ExecutionStatus.Aborted:
                    case ExecutionStatus.NotRun:
                        WriteWarning(line);
                        break;
                    case ExecutionStatus.Failed:
                        WriteError(line);
                        break;
                    case ExecutionStatus.Collective:
                        break;
                    default:
                        throw new NotSupportedException(target.Status.ToString());
                }
            }

            WriteNormal(new string(c: '─', count: allColumns));
            WriteInformation(CreateLine("Total", string.Empty, GetDuration(totalDuration)));
            WriteNormal(new string(c: '═', count: allColumns));
        }

        protected virtual void WriteSevereMessages()
        {
            WriteInformation("Repeating warnings and errors:");

            foreach (var (level, message) in SevereMessages.ToList())
            {
                switch (level)
                {
                    case LogLevel.Warning:
                        WriteWarning(message);
                        break;
                    case LogLevel.Error:
                        WriteError(message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal void WriteAndReportWarning(string text, string details = null)
        {
            SevereMessages.Add(Tuple.Create(LogLevel.Warning, text));
            ReportWarning(text, details);
            if (EnableWriteWarnings)
                WriteWarning(text, details);
        }

        internal void WriteAndReportError(string text, string details = null)
        {
            SevereMessages.Add(Tuple.Create(LogLevel.Error, text));
            ReportError(text, details);
            if (EnableWriteErrors)
                WriteError(text, details);
        }

        protected virtual string FormatBlockText(string text)
        {
            return text;
        }

        protected virtual bool EnableWriteWarnings => true;
        protected virtual bool EnableWriteErrors => true;

        protected virtual void ReportWarning(string text, string details = null)
        {
        }

        protected virtual void ReportError(string text, string details = null)
        {
        }

        protected void WriteNormal()
        {
            WriteNormal(string.Empty);
        }

        protected internal abstract void WriteNormal(string text);
        protected internal abstract void WriteSuccess(string text);
        protected internal abstract void WriteTrace(string text);
        protected internal abstract void WriteInformation(string text);

        protected abstract void WriteWarning(string text, string details = null);
        protected abstract void WriteError(string text, string details = null);
    }
}
