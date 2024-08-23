//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Threading;
//using static Nrk.FluentCore.Utils.IProgressReporter;

//namespace Nrk.FluentCore.Utils;

//public interface IProgressReporter
//{
//    public event EventHandler<ProgressUpdater>? ProgressChanged;

//    public Dictionary<string, ProgressData> Progresses { get; }

//    internal IProgress<ProgressUpdater> Progress { get; }

//    public class ProgressData(string taskName) : INotifyPropertyChanged
//    {
//        public string TaskName { get; set; } = taskName;

//        public State TaskState { get; set; } = State.Prepared;

//        public int TotalTasks { get; set; } = 1;

//        internal int finishedTasks;
//        public int FinishedTasks
//        {
//            get => finishedTasks;
//            set => finishedTasks = value;
//        }

//        public event PropertyChangedEventHandler? PropertyChanged;

//        internal void ReportPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

//        public enum State
//        {
//            Prepared,
//            Running,
//            Finished,
//            Failed
//        }
//    }

//    public class ProgressUpdater
//    {
//        private Action<Dictionary<string, ProgressData>> UpdateAction { get; set; }

//        internal ProgressUpdater(Action<Dictionary<string, ProgressData>> updateAction)
//        {
//            UpdateAction = updateAction;
//        }

//        public void Update(Dictionary<string, ProgressData> progresses) => UpdateAction!.Invoke(progresses);

//        internal static ProgressUpdater FromFinished(string taskName)
//        {
//            return new ProgressUpdater(progresses =>
//            {
//                var data = progresses[taskName];

//                data.FinishedTasks = data.TotalTasks;
//                data.TaskState = ProgressData.State.Finished;

//                data.ReportPropertyChanged("FinishedTasks");
//                data.ReportPropertyChanged("TaskState");
//            });
//        }

//        internal static ProgressUpdater FromFailed(string taskName)
//        {
//            return new ProgressUpdater(progresses =>
//            {
//                var data = progresses[taskName];

//                data.TaskState = ProgressData.State.Failed;
//                data.ReportPropertyChanged("TaskState");
//            });
//        }

//        internal static ProgressUpdater FromRunning(string taskName)
//        {
//            return new ProgressUpdater(progresses =>
//            {
//                var data = progresses[taskName];

//                data.TaskState = ProgressData.State.Running;
//                data.ReportPropertyChanged("TaskState");
//            });
//        }

//        internal static ProgressUpdater FromUpdateTotalTasks(string taskName, int totalTasksNumber)
//        {
//            return new ProgressUpdater(progresses =>
//            {
//                var data = progresses[taskName];

//                data.TotalTasks = totalTasksNumber;
//                data.ReportPropertyChanged("TotalTasks");
//            });
//        }

//        internal static ProgressUpdater FromUpdateFinishedTasks(string taskName, int finishedTasksNumber)
//        {
//            return new ProgressUpdater(progresses =>
//            {
//                var data = progresses[taskName];

//                data.FinishedTasks = finishedTasksNumber;
//                data.ReportPropertyChanged("FinishedTasks");
//            });
//        }

//        internal static ProgressUpdater FromUpdateAllTasks(string taskName, int finishedTasksNumber, int totalTasksNumber)
//        {
//            return new ProgressUpdater(progresses =>
//            {
//                var data = progresses[taskName];

//                data.TotalTasks = totalTasksNumber;
//                data.FinishedTasks = finishedTasksNumber;

//                data.ReportPropertyChanged("TotalTasks");
//                data.ReportPropertyChanged("FinishedTasks");
//            });
//        }

//        internal static ProgressUpdater FromIncrementFinishedTasks(string taskName)
//        {
//            return new ProgressUpdater(progresses =>
//            {
//                var data = progresses[taskName];

//                Interlocked.Increment(ref data.finishedTasks);
//                data.ReportPropertyChanged("FinishedTasks");
//            });
//        }
//    }
//}

//internal static class ProgressReporterHelper
//{
//    internal static Dictionary<string, ProgressData> CreateProgressesFromStringArray(string[] taskNames)
//    {
//        var dic = new Dictionary<string, ProgressData>();

//        foreach (var names in taskNames)
//            dic.Add(names, new ProgressData(names));

//        return dic;
//    }

//    internal static void ReportWhenExceptionThrow(IProgressReporter progressReporter)
//    {
//        foreach (var progressData in progressReporter.Progresses.Values.Where(x => x.TaskState.Equals(ProgressData.State.Running)))
//        {
//            progressReporter.Progress.Report(ProgressUpdater.FromFailed(progressData.TaskName));
//        }
//    }
//}