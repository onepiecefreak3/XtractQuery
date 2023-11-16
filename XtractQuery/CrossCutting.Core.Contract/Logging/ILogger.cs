using System;

namespace CrossCutting.Core.Contract.Logging
{
    public interface ILogger
    {
        bool IsTraceEnabled { get; }
        bool IsDebugEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }
        bool IsErrorEnabled { get; }

        /// <summary>
        /// Message only for debug trace
        /// </summary>
        /// <param name="msg">Trace message</param>
        void Trace(string msg);

        /// <summary>
        /// Message only for debug trace
        /// </summary>
        /// <param name="msg">Trace message</param>
        /// <param name="arg0">First argument</param>
        void Trace(string msg, object arg0);

        /// <summary>
        /// Message only for debug trace
        /// </summary>
        /// <param name="msg">Trace message</param>
        /// <param name="arg0">First argument</param>
        /// <param name="arg1">Second argument</param>
        void Trace(string msg, object arg0, object arg1);

        /// <summary>
        /// Message only for debug trace
        /// </summary>
        /// <param name="msg">Trace message</param>
        /// <param name="args">Arguments</param>
        void Trace(string msg, params object[] args);

        /// <summary>
        /// Message only for debugging purpose
        /// </summary>
        /// <param name="msg">Debug message</param>
        void Debug(string msg);

        /// <summary>
        /// Message only for debug purpose
        /// </summary>
        /// <param name="msg">Debug message</param>
        /// <param name="arg0">First argument</param>
        void Debug(string msg, object arg0);

        /// <summary>
        /// Message only for debug purpose
        /// </summary>
        /// <param name="msg">Debug message</param>
        /// <param name="arg0">First argument</param>
        /// <param name="arg1">Second argument</param>
        void Debug(string msg, object arg0, object arg1);

        /// <summary>
        /// Message only for debug purpose
        /// </summary>
        /// <param name="msg">Debug message</param>
        /// <param name="args">Arguments</param>
        void Debug(string msg, params object[] args);

        /// <summary>
        /// Non erroneous behavior
        /// </summary>
        /// <param name="msg">Information message</param>
        void Info(string msg);

        /// <summary>
        /// Non erroneous behavior
        /// </summary>
        /// <param name="msg">Information message</param>
        /// <param name="arg0">First argument</param>
        void Info(string msg, object arg0);

        /// <summary>
        /// Non erroneous behavior
        /// </summary>
        /// <param name="msg">Information message</param>
        /// <param name="arg0">First argument</param>
        /// <param name="arg1">Second argument</param>
        void Info(string msg, object arg0, object arg1);

        /// <summary>
        /// Non erroneous behavior
        /// </summary>
        /// <param name="msg">Information message</param>
        /// <param name="args">Arguments</param>
        void Info(string msg, params object[] args);

        /// <summary>
        /// A warning occured, application will continue
        /// </summary>
        /// <param name="msg">Warning message</param>
        void Warn(string msg);

        /// <summary>
        /// A warning occured, application will continue
        /// </summary>
        /// <param name="msg">Warning message</param>
        /// <param name="arg0">First argument</param>
        void Warn(string msg, object arg0);

        /// <summary>
        /// A warning occured, application will continue
        /// </summary>
        /// <param name="msg">Warning message</param>
        /// <param name="arg0">First argument</param>
        /// <param name="arg1">Second argument</param>
        void Warn(string msg, object arg0, object arg1);

        /// <summary>
        /// A warning occured, application will continue
        /// </summary>
        /// <param name="msg">Warning message</param>
        /// <param name="args">Arguments</param>
        void Warn(string msg, params object[] args);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="msg">Error message</param>
        void Error(string msg);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <param name="arg0">First argument</param>
        void Error(string msg, object arg0);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <param name="arg0">First argument</param>
        /// <param name="arg1">Second argument</param>
        void Error(string msg, object arg0, object arg1);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <param name="args">Arguments</param>
        void Error(string msg, params object[] args);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <param name="ex">Exception that was thrown</param>
        void Error(string msg, Exception ex);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <param name="ex">Exception that was thrown</param>
        /// <param name="arg0">First argument</param>
        void Error(string msg, Exception ex, object arg0);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <param name="ex">Exception that was thrown</param>
        /// <param name="arg0">First argument</param>
        /// <param name="arg1">Second argument</param>
        void Error(string msg, Exception ex, object arg0, object arg1);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <param name="ex">Exception that was thrown</param>
        /// <param name="args">Arguments</param>
        void Error(string msg, Exception ex, params object[] args);

        /// <summary>
        /// An error occured, application may or may not continue
        /// </summary>
        /// <param name="ex">Exception that was thrown</param>
        void Error(Exception ex);
    }
}
