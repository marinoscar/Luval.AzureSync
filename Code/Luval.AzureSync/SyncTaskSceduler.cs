using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.AzureSync
{
    public class SyncTaskSceduler : TaskScheduler
    {

        #region Variable Declaration
        
        private readonly int _maxTasks;
        private List<Task> _taskList;
        private List<Task> _queueList; 

        #endregion

        #region Constructors
        
        public SyncTaskSceduler()
            : this(10)
        {
        }

        public SyncTaskSceduler(int concurrentTasks)
        {
            _maxTasks = concurrentTasks;
            _taskList = new List<Task>(_maxTasks);
            _queueList = new List<Task>(_maxTasks * 10);
        } 

        #endregion

        public bool RunAsync { get; set; }

        #region Method Implementations

        public void Execute(Task task)
        {
            QueueTask(task);
        }

        public void WaitAll()
        {
            Task.WaitAll(_taskList.ToArray());
        }

        protected override void QueueTask(Task task)
        {
            _queueList.Add(task);
            ExecuteTask(task);
        }

        protected virtual void TryToExecute(Task task)
        {
            if (_taskList.Count >= _maxTasks) return;
            _taskList.Add(task);
        }

        private void ExecuteTask(Task task)
        {
            if (!RunAsync)
            {
                task.RunSynchronously();
                OnTaskCompleted(task);
                return;
            }
            task.Start();
            task.ContinueWith(OnTaskCompleted);
        }

        private void OnTaskCompleted(Task task)
        {
            _taskList.Remove(task);
            _queueList.Remove(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _queueList;
        } 

        #endregion
    }
}
