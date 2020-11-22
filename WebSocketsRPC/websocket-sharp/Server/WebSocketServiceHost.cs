#region License
/*
 * WebSocketServiceHost.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2017 sta.blockhead
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

#region Contributors
/*
 * Contributors:
 * - Juan Manuel Lallana <juan.manuel.lallana@gmail.com>
 */
#endregion

using System;
using System.Linq;
using WebSocketsRPC.Net.WebSockets;

namespace WebSocketsRPC.Server
{
    #region Description
    /// <summary>
    /// Exposes the methods and properties used to access the information in
    /// a WebSocket service provided by the <see cref="WebSocketServer"/> or
    /// <see cref="HttpServer"/>.
    /// </summary>
    /// <remarks>
    /// This class is an abstract class.
    /// </remarks>
    #endregion Description
    public abstract class WebSocketServiceHost
    {
        #region Private Fields

        private Logger _log;
        private string _path;
        private WebSocketSessionManager _sessions;

        #endregion

        #region Protected Constructors

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServiceHost"/> class
        /// with the specified <paramref name="path"/> and <paramref name="log"/>.
        /// </summary>
        /// <param name="path">
        /// A <see cref="string"/> that represents the absolute path to the service.
        /// </param>
        /// <param name="log">
        /// A <see cref="Logger"/> that represents the logging function for the service.
        /// </param>
        #endregion Description
        protected WebSocketServiceHost(string path, Logger log)
        {
            _path = path;
            _log = log;
            _sessions = new WebSocketSessionManager(log);
        }

        #endregion

        #region Internal Properties

        internal ServerState State => _sessions.State;

        #endregion

        #region Protected Properties

        #region Description
        /// <summary>
        /// Gets the logging function for the service.
        /// </summary>
        /// <value>
        /// A <see cref="Logger"/> that provides the logging function.
        /// </value>
        #endregion Description
        protected Logger Log => _log;

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets or sets a value indicating whether the service cleans up
        /// the inactive sessions periodically.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// <c>true</c> if the service cleans up the inactive sessions every
        /// 60 seconds; otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public bool KeepClean { get => _sessions.KeepClean; set => _sessions.KeepClean = value; }

        #region Description
        /// <summary>
        /// Gets the path to the service.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the absolute path to
        /// the service.
        /// </value>
        #endregion Description
        public string Path => _path;

        #region Description
        /// <summary>
        /// Gets the management function for the sessions in the service.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketSessionManager"/> that manages the sessions in
        /// the service.
        /// </value>
        #endregion Description
        public WebSocketSessionManager Sessions => _sessions;

        #region Description
        /// <summary>
        /// Gets the <see cref="Type"/> of the behavior of the service.
        /// </summary>
        /// <value>
        /// A <see cref="Type"/> that represents the type of the behavior of
        /// the service.
        /// </value>
        #endregion Description
        public abstract Type BehaviorType { get; }

        #region Description
        /// <summary>
        /// Gets or sets the time to wait for the response to the WebSocket Ping or
        /// Close.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// A <see cref="TimeSpan"/> to wait for the response.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value specified for a set operation is zero or less.
        /// </exception>
        #endregion Description
        public TimeSpan WaitTime { get => _sessions.WaitTime; set => _sessions.WaitTime = value; }

        #endregion

        #region Internal Methods

        internal void Start() => _sessions.Start();

        internal void StartSession(WebSocketContext context) => CreateSession().Start(context, _sessions);

        internal void Stop(ushort code, string reason) => _sessions.Stop(code, reason);

        #endregion

        #region Protected Methods

        #region Description
        /// <summary>
        /// Creates a new session for the service.
        /// </summary>
        /// <returns>
        /// A <see cref="WebSocketBehavior"/> instance that represents
        /// the new session.
        /// </returns>
        #endregion Description
        protected abstract WebSocketBehavior CreateSession();

        #endregion
    }
}