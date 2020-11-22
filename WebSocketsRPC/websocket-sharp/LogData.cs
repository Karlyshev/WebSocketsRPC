#region License
/*
 * LogData.cs
 *
 * The MIT License
 *
 * Copyright (c) 2013-2015 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

using System;
using System.Diagnostics;
using System.Text;

namespace WebSocketsRPC
{
    #region Description
    /// <summary>
    /// Represents a log data used by the <see cref="Logger"/> class.
    /// </summary>
    #endregion Description
    public class LogData
    {
        #region Private Fields

        private StackFrame _caller;
        private DateTime _date;
        private LogLevel _level;
        private string _message;

        #endregion

        #region Internal Constructors

        internal LogData(LogLevel level, StackFrame caller, string message)
        {
            _level = level;
            _caller = caller;
            _message = message ?? string.Empty;
            _date = DateTime.Now;
        }

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets the information of the logging method caller.
        /// </summary>
        /// <value>
        /// A <see cref="StackFrame"/> that provides the information of the logging method caller.
        /// </value>
        #endregion Description
        public StackFrame Caller => _caller;

        #region Description
        /// <summary>
        /// Gets the date and time when the log data was created.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> that represents the date and time when the log data was created.
        /// </value>
        #endregion Description
        public DateTime Date => _date;

        #region Description
        /// <summary>
        /// Gets the logging level of the log data.
        /// </summary>
        /// <value>
        /// One of the <see cref="LogLevel"/> enum values, indicates the logging level of the log data.
        /// </value>
        #endregion Description
        public LogLevel Level => _level;

        #region Description
        /// <summary>
        /// Gets the message of the log data.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the message of the log data.
        /// </value>
        #endregion Description
        public string Message => _message;

        #endregion

        #region Public Methods

        #region Description
        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="LogData"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current <see cref="LogData"/>.
        /// </returns>
        #endregion Description
        public override string ToString()
        {
            var header = $"{_date}|{_level,-5}|";
            var method = _caller.GetMethod();
            var type = method.DeclaringType;
#if DEBUG
            var lineNum = _caller.GetFileLineNumber();
            var headerAndCaller = $"{header}{type.Name}.{method.Name}:{lineNum}|";
#else
            var headerAndCaller = $"{header}{type.Name}.{method.Name}|";
#endif
            var msgs = _message.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            if (msgs.Length <= 1)
                return $"{headerAndCaller}{_message}";
            var buff = new StringBuilder($"{headerAndCaller}{msgs[0]}\n", 64);
            for (var i = 1; i < msgs.Length; i++)
                buff.Append($",{header.Length}{msgs[i]}\n");
            buff.Length--;
            return buff.ToString();
        }

        #endregion
    }
}