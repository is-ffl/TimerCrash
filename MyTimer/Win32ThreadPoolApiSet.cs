using System;
using System.Runtime.InteropServices;
using System.Threading;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

// adapted from Jiri Cincura`s https://github.com/cincuranet/AbsoluteTimer

namespace Win32ThreadPoolTimerApiSet
{
    public sealed class ThreadPoolTimer : IDisposable
    {
        /// <summary>
        /// An application-defined function that serves as the starting address for a timer callback or a registered wait callback.
        /// Specify this address when calling the CreateTimerQueueTimer, RegisterWaitForSingleObject function.
        /// </summary>
        /// <param name="lpParameter">
        /// The thread data passed to the function using a parameter
        /// of the CreateTimerQueueTimer or RegisterWaitForSingleObject function.
        /// </param>
        /// <param name="timerOrWaitFired">
        /// If this parameter is TRUE, the wait timed out.
        /// If this parameter is FALSE, the wait event has been signaled.
        /// (This parameter is always TRUE for timer callbacks.)
        /// </param>
        private delegate void TimerCallback([In, Out] IntPtr Instance, [In, Out, Optional] IntPtr pCBContext, [In, Out] IntPtr Timer);

        /// <summary>
        /// Creates a threadpool timer. This timer expires at the specified due time, then after every specified period. When the timer expires, the callback function is called.
        /// </summary>
        /// <param name="pfnti">A pointer to the application-defined callback function to be executed when the timer .</param>
        /// <param name="pv">optional application-defined user data (pv=parameter value) that will be passed to the callback function.</param>
        /// <param name="pcbe">A TP_CALLBACK_ENVIRON structure that defines the environment in which to execute the callback. The InitializeThreadpoolEnvironment function returns this structure. If this parameter is NULL, the callback executes in the default callback environment. For more information, see InitializeThreadpoolEnvironment.</param>
        /// <returns>
        /// If the function succeeds, it returns a pointer to a TP_TIMER structure that defines the timer object. Applications do not modify the members of this structure.
        /// If the function fails, it returns NULL. To retrieve extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateThreadpoolTimer([In] TimerCallback pfnti, [In, Out] IntPtr pCBContext, [Optional] IntPtr pcbe);

        /// <summary>
        /// Sets the timer object, replacing the previous timer, if any. A worker thread calls the timer object's callback after the specified timeout expires.
        /// Setting the timer cancels the previous timer, if any.
        /// </summary>
        /// <param name="pti">A pointer to a TP_TIMER structure that defines the timer object to set. The CreateThreadpoolTimer function returns this pointer.</param>
        /// <param name="pftDueTime">A pointer to a FILETIME structure that specifies the absolute or relative time at which the timer should expire. If positive or zero, it indicates the absolute time since January 1, 1601 (UTC), measured in 100 nanosecond units. If negative, it indicates the amount of time to wait relative to the current time. For more information about time values, see File Times. If this parameter is NULL, the timer object will cease to queue new callbacks (but callbacks already queued will still occur). The timer is set if the pftDueTime parameter is non-NULL. </param>
        /// <param name="msPeriod">The timer period, in milliseconds. If this parameter is zero, the timer is signaled once. If this parameter is greater than zero, the timer is periodic. A periodic timer automatically reactivates each time the period elapses, until the timer is canceled.</param>
        /// <param name="msWindowLength">The maximum amount of time the system can delay before calling the timer callback. If this parameter is set, the system can batch calls to conserve power.</param>
        /// <returns>
        /// none
        /// </returns>
        [DllImport("kernel32.dll")]
        private static extern void SetThreadpoolTimer([In, Out] IntPtr pti, [In, Optional] IntPtr pftDueTime, [In] uint msPeriod, [In, Optional] uint msWindowLength);

        /// <summary>
        /// Releases the specified timer object.
        /// The timer object is freed immediately if there are no outstanding callbacks; otherwise, the timer object is freed asynchronously after the outstanding callback functions complete.
        /// In some cases, callback functions might run after CloseThreadpoolTimer has been called.To prevent this behavior: see CloseThreadpoolTimer function
        /// </summary>
        /// <param name="pti">A pointer to a TP_TIMER structure that defines the timer object to set. The CreateThreadpoolTimer function returns this pointer.</param>
        /// <returns>
        /// none
        /// </returns>
        [DllImport("kernel32.dll")]
        private static extern void CloseThreadpoolTimer([In, Out] IntPtr pti);

        const uint _ticks_per_ms = 10_000;

        #region Fields

        static readonly int FileTimeSize = Marshal.SizeOf(typeof(FILETIME));
        private object _cbUserdata;
        IntPtr _timer;
        IntPtr _dueTimeUtc;
        TimerCallback _callback;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Win32ThreadpoolApiSet"/> class.
        /// </summary>
        public ThreadPoolTimer()
        {
        }
        #endregion Constructors

        #region Methods

        /// <summary>
        /// starts an absolute timer.
        /// </summary>
        public static ThreadPoolTimer StartAbsoluteTimer(Action<object> callback, object cbUserdata, DateTime dueTimeUtc, uint msperiod)
        {
            var ret = new ThreadPoolTimer();
            ret._cbUserdata = cbUserdata;
            ret._callback = (instance, context, timer) => callback(ret._cbUserdata);
            ret._timer = CreateThreadpoolTimer(ret._callback, IntPtr.Zero, IntPtr.Zero);
            ret._dueTimeUtc = Marshal.AllocHGlobal(FileTimeSize);
            Marshal.StructureToPtr(Int64ToFiletime(dueTimeUtc.ToFileTimeUtc()), ret._dueTimeUtc, false);     // marshal C# Filetime structure to unmanaged _dueTimeUtc; 'false': do not call DestroyStructure on _dueTimeUtc
            SetThreadpoolTimer(ret._timer, ret._dueTimeUtc, msperiod, 0);
            return ret;
        }

        /// <summary>
        /// starts a relative timer.
        /// </summary>
        /// <param name="callback">Callback function delegate</param>, 
        /// <param name="object userData">callback's userdata</param>, 
        /// <param name="initialwaitTimeMs">The amount of time in milliseconds relative to the current time that must elapse before the timer is signaled for the first time.</param>
        /// <param name="msperiod">The period, in milliseconds.</param>/// 
        public static ThreadPoolTimer StartRelativeTimer(Action<object> callback, object cbUserdata, uint initialwaitTimeMs, uint msperiod)
        {
            var ret = new ThreadPoolTimer();
            ret._cbUserdata = cbUserdata;
            ret._callback = (instance, context, timer) => callback(ret._cbUserdata);
            ret._timer = CreateThreadpoolTimer(ret._callback, IntPtr.Zero, IntPtr.Zero);
            ret._dueTimeUtc = Marshal.AllocHGlobal(FileTimeSize);
            Marshal.StructureToPtr(Int64ToFiletime((-1) - (initialwaitTimeMs * _ticks_per_ms)), ret._dueTimeUtc, false);     // (-1): relative wait time is negative in order to distiguish from absolute timer (is positive) 
            SetThreadpoolTimer(ret._timer, ret._dueTimeUtc, msperiod, 0);
            return ret;
        }

        #endregion Methods

        public void Dispose()
        {
            ReleaseTimer();
            GC.SuppressFinalize(this);
        }

        ~ThreadPoolTimer()
        {
            ReleaseTimer();
        }

        void ReleaseTimer()
        {
            var timer = Interlocked.Exchange(ref _timer, IntPtr.Zero);
            if (timer != IntPtr.Zero)
            {
                CloseThreadpoolTimer(timer);
                Marshal.FreeHGlobal(_dueTimeUtc);
                _dueTimeUtc = IntPtr.Zero;
            }
        }

        static FILETIME Int64ToFiletime(Int64 value)
        {
            FILETIME ft;

            ft.dwLowDateTime = (int)(value & 0xFFFFFFFF);
            ft.dwHighDateTime = (int)(value >> 32);
            return ft;
        }
    }
}
