using Assets.Scripts.Terrain;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Assets.Scripts.Utility {

    /*
     * Contains baseline functions for creating a new thread to execute large/background tasks.
     * Includes an output queue for collecting data whilst running.
     */
    public class ThreadedWorker {
        public AutoResetEvent signal = new AutoResetEvent(false);
        public ManualResetEvent terminate = new ManualResetEvent(false);
        protected object handle = new object();
        protected Thread thread = null;
        private bool isDone = false;
        protected Queue<List<object>> queue = new Queue<List<object>>();

        //Add data to the queue and set the signal indicating something is in the queue.
        private void Send(List<object> value) {
            lock (handle) {
                queue.Enqueue(value);
            }
            signal.Set();
        }

        public void Send() {
            Send("", LogLevel.INFO, 0);
        }

        public void Send(object message) {
            Send(message, LogLevel.INFO, 0);
        }

        public void Send(object message, LogLevel level) {
            Send(message, level, 0);
        }

        public void Send(object message, int verbosity) {
            Send(message, LogLevel.DEBUG, verbosity);
        }

        //Only output messages if the given log level and verbosity are less than or equal to the log level and verbosity settings. This allows fine control over what messages are output.
        public void Send(object message, LogLevel level, int verbosity) {
            if (level <= Info.LOG_LEVEL && verbosity <= Info.LOG_VERBOSITY) {
                Send(new List<object>() { message, level });
            }
        }

        //Return item from queue (if one exists).
        public List<object> Recieve() {
            List<object> item = null;
            lock (handle) {
                if (queue.Count > 0) {
                    item = queue.Dequeue();
                }
            }
            return item;
        }

        //Set/Get whether the thread work is done.
        public bool IsDone {
            get {
                bool done;
                lock (handle) {
                    done = isDone;
                }
                return done;
            }
            set {
                lock (handle) {
                    isDone = value;
                }
            }
        }

        //Create and start the thread. All threads are marked as background threads so as not to prevent the main process from terminating.
        public virtual void Start() {
            signal = new AutoResetEvent(false);
            terminate = new ManualResetEvent(false);
            handle = new object();
            queue = new Queue<List<object>>();
            thread = new Thread(new ThreadStart(Run)) {
                IsBackground = true
            };
            thread.Start();
        }

        //Tell the thread to stop at its next check (checks are implemented per thread).
        public virtual void Abort() {
            signal.Set();
            terminate.Set();
        }

        protected virtual void ThreadFunction() { }

        //Return update state (If thread work complete, return true).
        public virtual bool Update() {
            if (IsDone) {
                return true;
            }
            return false;
        }

        //Run the thread function. Catches and outputs any errors.
        private void Run() {
            try {
                ThreadFunction();
                IsDone = true;
            } catch (System.Exception e) {
                Info.log.Send(string.Format("Caught exception in thread: {0}", e.ToString()), LogLevel.ERROR, 1);
            }
        }
    }
}
