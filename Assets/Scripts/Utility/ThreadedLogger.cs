using Assets.Scripts.Terrain;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Assets.Scripts.Utility {

    /*
     * Enum for log level. Level is printed in log before log message.
     */
    public enum LogLevel {
        INFO,
        ERROR,
        DEBUG
    }

    /*
     * Creates a logging thread which runs in the background for the entire duration of the program.
     */
    public class ThreadedLogger: ThreadedWorker {

        //Retrieve and print log messages 1 at a time whilst the thread is alive.
        protected override void ThreadFunction() {
            while (!terminate.WaitOne(0)) {
                signal.WaitOne(100);

                //Copy and clear the main queue so we don't block other threads from adding to the queue.
                Queue<List<object>> logQueue;
                lock (handle) {
                    logQueue = new Queue<List<object>>(queue);
                    queue.Clear();
                }

                foreach (List<object> log in logQueue) {
                    Print(log[0].ToString(), (LogLevel) log[1]);
                }
            }
        }

        //Reset the log file when the program starts.
        public ThreadedLogger() {
            File.WriteAllText("log.txt", "");
        }

        //Check again if the thread is still alive, then write the log text to the log file. (log.txt is in the root of the program exe)
        private void WriteToFile(string text) {
            if ((!terminate.WaitOne(0))) {
                File.AppendAllText("log.txt", text);
            }
        }

        //Format the string with the current time, log level, and log message.
        public void Print(string text, LogLevel level) {
            WriteToFile(string.Format("{0} - ({1}): {2}\n",System.DateTime.Now.ToLongTimeString(), level.ToString(), text));
        }
    }
}
