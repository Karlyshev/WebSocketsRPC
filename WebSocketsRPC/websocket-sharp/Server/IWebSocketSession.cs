#region License
/*
 * IWebSocketSession.cs
 *
 * The MIT License
 *
 * Copyright (c) 2013-2018 sta.blockhead
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
using WebSocketsRPC.Net.WebSockets;

namespace WebSocketsRPC.Server
{
    #region Description
    /// <summary>
    /// Exposes the access to the information in a WebSocket session.
    /// </summary>
    #endregion Description
    public interface IWebSocketSession
    {
        #region Properties

        #region Description
        /// <summary>
        /// Gets the current state of the WebSocket connection for the session.
        /// </summary>
        /// <value>
        ///   <para>
        ///   One of the <see cref="WebSocketState"/> enum values.
        ///   </para>
        ///   <para>
        ///   It indicates the current state of the connection.
        ///   </para>
        /// </value>
        #endregion Description
        WebSocketState ConnectionState { get; }

        #region Description
        /// <summary>
        /// Gets the information in the WebSocket handshake request.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketContext"/> instance that provides the access to
        /// the information in the handshake request.
        /// </value>
        #endregion Description
        WebSocketContext Context { get; }

        #region Description
        /// <summary>
        /// Gets the unique ID of the session.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the unique ID of the session.
        /// </value>
        #endregion Description
        string ID { get; }

        #region Description
        /// <summary>
        /// Gets the name of the WebSocket subprotocol for the session.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the name of the subprotocol
        /// if present.
        /// </value>
        #endregion Description
        string Protocol { get; }

        #region Description
        /// <summary>
        /// Gets the time that the session has started.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> that represents the time that the session
        /// has started.
        /// </value>
        #endregion Description
        DateTime StartTime { get; }

        #endregion
    }
}