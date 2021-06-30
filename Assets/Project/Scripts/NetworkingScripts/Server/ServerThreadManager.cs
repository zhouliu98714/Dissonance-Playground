using System;
using System.Collections.Generic;
using UnityEngine;

namespace CEMSIM
{
    namespace Network
    {
        /// <summary>
        /// This class coordinates network related actions at the server side to prevent congesting the GUI/main thread.
        /// It's functionality is realized by maintaining two queues holding tasks to be executed.
        /// The two queues are:
        ///     1. executeOnMainThread:
        ///         Actions are directly pushed to the end of this queue.
        ///         Periodically (based on the frame rate), actions in queue are transferred to
        ///         executeCopiedOnMainThread to be executed.
        ///     2. executeCopiedOnMainThread:
        ///         Actions in queue are executed sequentially.
        /// 
        /// TODO:
        ///     According to the scheduling problem discussed in the paper, it would be more reasonable to
        ///     maintain a priority queue/heap at least at the server side. At present, it's the same as the
        ///     server side ThreadManager (Server/ServerThreadManager.cs), but in the future, they are going to be quite different.
        ///     However, since the load of the network traffic is almost negligible, there is
        ///     no need to change this class at present.
        /// </summary>
        public class ServerThreadManager : MonoBehaviour
        {
            private static readonly List<Action> executeOnMainThread = new List<Action>();       // a buffer queue that accept actions
            private static readonly List<Action> executeCopiedOnMainThread = new List<Action>(); // a queue of actions(no-input function handlers) to run on main thread.
            private static bool actionToExecuteOnMainThread = false;                             // whether executeOnMainThread is empty


            // Update is called once per frame
            private void Update()
            {
                UpdateMain();
            }

            public static void ExecuteOnMainThread(Action _action)
            {
                if (_action == null)
                {
                    Debug.Log("No action to execute on main thread");
                    return;
                }

                // add action to a list of actions to run
                // lock is released once the insertion is completed
                lock (executeOnMainThread)
                {
                    executeOnMainThread.Add(_action);
                    actionToExecuteOnMainThread = true;
                }
            }

            /// <summary>
            /// call all actions piled up in the main thread. This is a not a good idea for real application, but make sense here.
            /// This function should be called only from the main thread.
            /// </summary>
            public static void UpdateMain()
            {
                if (actionToExecuteOnMainThread)
                {
                    executeCopiedOnMainThread.Clear();
                    lock (executeOnMainThread)
                    {
                        executeCopiedOnMainThread.AddRange(executeOnMainThread);
                        executeOnMainThread.Clear();
                        actionToExecuteOnMainThread = false;
                    }

                    // run actions
                    for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
                    {
                        executeCopiedOnMainThread[i]();
                    }
                }
            }
        }
    }
}