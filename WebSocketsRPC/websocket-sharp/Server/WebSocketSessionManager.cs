#region License
/*
 * WebSocketSessionManager.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2015 sta.blockhead
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;

namespace WebSocketsRPC.Server
{
    #region Description
    /// <summary>
    /// Provides the management function for the sessions in a WebSocket service.
    /// </summary>
    /// <remarks>
    /// This class manages the sessions in a WebSocket service provided by
    /// the <see cref="WebSocketServer"/> or <see cref="HttpServer"/>.
    /// </remarks>
    #endregion Description
    public class WebSocketSessionManager
    {
        #region Private Fields

        private volatile bool _clean;
        private Logger _log;
        private ConcurrentDictionary<string, IWebSocketSession> _sessions;
        private volatile ServerState _state;
        private volatile bool _sweeping;
        private System.Timers.Timer _sweepTimer;
        private TimeSpan _waitTime;

        #endregion

        #region Internal Constructors

        internal WebSocketSessionManager(Logger log)
        {
            _log = log;

            _clean = true;
            _sessions = new ConcurrentDictionary<string, IWebSocketSession>();
            _state = ServerState.Ready;
            _waitTime = TimeSpan.FromSeconds(1);

            setSweepTimer(60000);
        }

        #endregion

        #region Internal Properties

        internal ServerState State => _state;

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets the IDs for the active sessions in the WebSocket service.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>IEnumerable&lt;string&gt;</c> instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the IDs for the active sessions.
        ///   </para>
        /// </value>
        #endregion Description
        public IEnumerable<string> ActiveIDs
        {
            get
            {
                foreach (var res in broadping(WebSocketFrame.EmptyPingBytes))
                {
                    if (res.Value)
                        yield return res.Key;
                }
            }
        }

        #region Description
        /// <summary>
        /// Gets the number of the sessions in the WebSocket service.
        /// </summary>
        /// <value>
        /// An <see cref="int"/> that represents the number of the sessions.
        /// </value>
        #endregion Description
        public int Count => _sessions.Count;

        #region Description
        /// <summary>
        /// Gets the IDs for the sessions in the WebSocket service.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>IEnumerable&lt;string&gt;</c> instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the IDs for the sessions.
        ///   </para>
        /// </value>
        #endregion Description
        public IEnumerable<string> IDs => _state != ServerState.Start ? Enumerable.Empty<string>() : _sessions.Keys.ToList();

        #region Description
        /// <summary>
        /// Gets the IDs for the inactive sessions in the WebSocket service.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>IEnumerable&lt;string&gt;</c> instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the IDs for the inactive sessions.
        ///   </para>
        /// </value>
        #endregion Description
        public IEnumerable<string> InactiveIDs
        {
            get
            {
                foreach (var res in broadping(WebSocketFrame.EmptyPingBytes))
                {
                    if (!res.Value)
                        yield return res.Key;
                }
            }
        }

        #region Description
        /// <summary>
        /// Gets the session instance with <paramref name="id"/>.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="IWebSocketSession"/> instance or <see langword="null"/>
        ///   if not found.
        ///   </para>
        ///   <para>
        ///   The session instance provides the function to access the information
        ///   in the session.
        ///   </para>
        /// </value>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to find.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        #endregion Description
        public IWebSocketSession this[string id]
        {
            get
            {
                if (id == null)
                    throw new ArgumentNullException("id");

                if (id.Length == 0)
                    throw new ArgumentException("An empty string.", "id");

                tryGetSession(id, out IWebSocketSession session);
                return session;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets a value indicating whether the inactive sessions in
        /// the WebSocket service are cleaned up periodically.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// <c>true</c> if the inactive sessions are cleaned up every 60 seconds;
        /// otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public bool KeepClean
        {
            get => _clean;
            set
            {
                if (!canSet(out string msg))
                {
                    _log.Warn(msg);
                    return;
                }
                _clean = value;
            }
        }

        #region Description
        /// <summary>
        /// Gets the session instances in the WebSocket service.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>IEnumerable&lt;IWebSocketSession&gt;</c> instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the session instances.
        ///   </para>
        /// </value>
        #endregion Description
        public IEnumerable<IWebSocketSession> Sessions => _state != ServerState.Start ? Enumerable.Empty<IWebSocketSession>() : _sessions.Values.ToList();

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
        public TimeSpan WaitTime
        {
            get => _waitTime;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException("value", "Zero or less.");

                if (!canSet(out string msg))
                {
                    _log.Warn(msg);
                    return;
                }
                _waitTime = value;
            }
        }

        #endregion

        #region Private Methods

        private void broadcast(Opcode opcode, byte[] data, Action completed)
        {
            var cache = new Dictionary<CompressionMethod, byte[]>();
            try
            {
                foreach (var session in Sessions)
                {
                    if (_state != ServerState.Start)
                    {
                        _log.Error("The service is shutting down.");
                        break;
                    }
                    session.Context.WebSocket.Send(opcode, data, cache);
                }
                completed?.Invoke();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                _log.Debug(ex.ToString());
            }
            finally
            {
                cache.Clear();
            }
        }

        private void broadcast(Opcode opcode, Stream stream, Action completed)
        {
            var cache = new Dictionary<CompressionMethod, Stream>();
            try
            {
                foreach (var session in Sessions)
                {
                    if (_state != ServerState.Start)
                    {
                        _log.Error("The service is shutting down.");
                        break;
                    }

                    session.Context.WebSocket.Send(opcode, stream, cache);
                }
                completed?.Invoke();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                _log.Debug(ex.ToString());
            }
            finally
            {
                foreach (var cached in cache.Values)
                    cached.Dispose();

                cache.Clear();
            }
        }

        private void broadcastAsync(Opcode opcode, byte[] data, Action completed) => ThreadPool.QueueUserWorkItem(state => broadcast(opcode, data, completed));

        private void broadcastAsync(Opcode opcode, Stream stream, Action completed) => ThreadPool.QueueUserWorkItem(state => broadcast(opcode, stream, completed));

        private Dictionary<string, bool> broadping(byte[] frameAsBytes)
        {
            var ret = new Dictionary<string, bool>();
            foreach (var session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }
                ret.Add(session.ID, session.Context.WebSocket.Ping(frameAsBytes, _waitTime));
            }
            return ret;
        }

        private bool canSet(out string message)
        {
            message = null;
            if (_state == ServerState.Start)
            {
                message = "The service has already started.";
                return false;
            }
            if (_state == ServerState.ShuttingDown)
            {
                message = "The service is shutting down.";
                return false;
            }
            return true;
        }

        private static string createID(List<string> existsKeys) 
        {
            if (existsKeys == null || existsKeys.Count == 0)
                return Guid.NewGuid().ToString("N");
            while (true)
            {
                var newId = Guid.NewGuid().ToString("N");
                if (!existsKeys.Contains(newId))
                    return newId;
            }
        }

        private void setSweepTimer(double interval)
        {
            _sweepTimer = new System.Timers.Timer(interval);
            _sweepTimer.Elapsed += (sender, e) => Sweep();
        }

        private void stop(PayloadData payloadData, bool send)
        {
            var bytes = send ? WebSocketFrame.CreateCloseFrame(payloadData, false).ToArray() : null;
            var sessionValues = _sessions.Values.ToList();
            _state = ServerState.ShuttingDown;
            _sweepTimer.Enabled = false;
            for (int i = 0; i < sessionValues.Count; i++)
                sessionValues[i].Context.WebSocket.Close(payloadData, bytes);
            _state = ServerState.Stop;
        }

        private bool tryGetSession(string id, out IWebSocketSession session)
        {
            session = null;
            return _state != ServerState.Start ? false : _sessions.TryGetValue(id, out session);
        }

        #endregion

        #region Internal Methods

        internal string Add(IWebSocketSession session)
        {
            if (_state != ServerState.Start)
                return null;
            var id = createID(_sessions.Keys.ToList());
            _sessions.TryAdd(id, session);
            return id;
        }

        internal void Broadcast(Opcode opcode, byte[] data, Dictionary<CompressionMethod, byte[]> cache)
        {
            foreach (var session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }
                session.Context.WebSocket.Send(opcode, data, cache);
            }
        }

        internal void Broadcast(Opcode opcode, Stream stream, Dictionary<CompressionMethod, Stream> cache)
        {
            foreach (var session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }
                session.Context.WebSocket.Send(opcode, stream, cache);
            }
        }

        internal Dictionary<string, bool> Broadping(byte[] frameAsBytes, TimeSpan timeout)
        {
            var ret = new Dictionary<string, bool>();
            foreach (var session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }
                ret.Add(session.ID, session.Context.WebSocket.Ping(frameAsBytes, timeout));
            }
            return ret;
        }

        internal bool Remove(string id) => _sessions.TryRemove(id, out var removedItem);

        internal void Start()
        {
            _sweepTimer.Enabled = _clean;
            _state = ServerState.Start;
        }

        internal void Stop(ushort code, string reason)
        {
            if (code == 1005)
            { // == no status
                stop(PayloadData.Empty, true);
                return;
            }
            stop(new PayloadData(code, reason), !code.IsReserved());
        }

        #endregion

        #region Public Methods

        #region Description
        /// <summary>
        /// Sends <paramref name="data"/> to every client in the WebSocket service.
        /// </summary>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public void Broadcast(byte[] data)
        {
            if (_state != ServerState.Start)
            {
                var msg = "The current state of the manager is not Start.";
                throw new InvalidOperationException(msg);
            }
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.LongLength <= WebSocket.FragmentLength)
                broadcast(Opcode.Binary, data, null);
            else
                broadcast(Opcode.Binary, new MemoryStream(data), null);
        }

        #region Description
        /// <summary>
        /// Sends <paramref name="data"/> to every client in the WebSocket service.
        /// </summary>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        #endregion Description
        public void Broadcast(string data)
        {
            if (_state != ServerState.Start)
                throw new InvalidOperationException("The current state of the manager is not Start.");
            if (data == null)
                throw new ArgumentNullException("data");
            if (!data.TryGetUTF8EncodedBytes(out byte[] bytes))
            {
                var msg = "It could not be UTF-8-encoded.";
                throw new ArgumentException(msg, "data");
            }
            if (bytes.LongLength <= WebSocket.FragmentLength)
                broadcast(Opcode.Text, bytes, null);
            else
                broadcast(Opcode.Text, new MemoryStream(bytes), null);
        }

        #region Description
        /// <summary>
        /// Sends the data from <paramref name="stream"/> to every client in
        /// the WebSocket service.
        /// </summary>
        /// <remarks>
        /// The data is sent as the binary data.
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> instance from which to read the data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        #endregion Description
        public void Broadcast(Stream stream, int length)
        {
            if (_state != ServerState.Start)
                throw new InvalidOperationException("The current state of the manager is not Start.");

            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanRead)
                throw new ArgumentException("It cannot be read.", "stream");

            if (length < 1)
                throw new ArgumentException("Less than 1.", "length");

            var bytes = stream.ReadBytes(length);

            var len = bytes.Length;
            if (len == 0)
                throw new ArgumentException("No data could be read from it.", "stream");

            if (len < length)
                _log.Warn($"Only {len} byte(s) of data could be read from the stream.");

            if (len <= WebSocket.FragmentLength)
                broadcast(Opcode.Binary, bytes, null);
            else
                broadcast(Opcode.Binary, new MemoryStream(bytes), null);
        }

        #region Description
        /// <summary>
        /// Sends <paramref name="data"/> asynchronously to every client in
        /// the WebSocket service.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="Action"/> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public void BroadcastAsync(byte[] data, Action completed)
        {
            if (_state != ServerState.Start)
                throw new InvalidOperationException("The current state of the manager is not Start.");

            if (data == null)
                throw new ArgumentNullException("data");

            if (data.LongLength <= WebSocket.FragmentLength)
                broadcastAsync(Opcode.Binary, data, completed);
            else
                broadcastAsync(Opcode.Binary, new MemoryStream(data), completed);
        }

        #region Description
        /// <summary>
        /// Sends <paramref name="data"/> asynchronously to every client in
        /// the WebSocket service.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="Action"/> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        #endregion Description
        public void BroadcastAsync(string data, Action completed)
        {
            if (_state != ServerState.Start)
                throw new InvalidOperationException("The current state of the manager is not Start.");

            if (data == null)
                throw new ArgumentNullException("data");

            if (!data.TryGetUTF8EncodedBytes(out byte[] bytes))
                throw new ArgumentException("It could not be UTF-8-encoded.", "data");

            if (bytes.LongLength <= WebSocket.FragmentLength)
                broadcastAsync(Opcode.Text, bytes, completed);
            else
                broadcastAsync(Opcode.Text, new MemoryStream(bytes), completed);
        }

        #region Description
        /// <summary>
        /// Sends the data from <paramref name="stream"/> asynchronously to
        /// every client in the WebSocket service.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The data is sent as the binary data.
        ///   </para>
        ///   <para>
        ///   This method does not wait for the send to be complete.
        ///   </para>
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> instance from which to read the data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="Action"/> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        #endregion Description
        public void BroadcastAsync(Stream stream, int length, Action completed)
        {
            if (_state != ServerState.Start)
                throw new InvalidOperationException("The current state of the manager is not Start.");

            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanRead)
                throw new ArgumentException("It cannot be read.", "stream");

            if (length < 1)
                throw new ArgumentException("Less than 1.", "length");

            var bytes = stream.ReadBytes(length);

            var len = bytes.Length;
            if (len == 0)
                throw new ArgumentException("No data could be read from it.", "stream");

            if (len < length)
                _log.Warn($"Only {len} byte(s) of data could be read from the stream.");

            if (len <= WebSocket.FragmentLength)
                broadcastAsync(Opcode.Binary, bytes, completed);
            else
                broadcastAsync(Opcode.Binary, new MemoryStream(bytes), completed);
        }

        #region Description
        /// <summary>
        /// Sends a ping to every client in the WebSocket service.
        /// </summary>
        /// <returns>
        ///   <para>
        ///   A <c>Dictionary&lt;string, bool&gt;</c>.
        ///   </para>
        ///   <para>
        ///   It represents a collection of pairs of a session ID and
        ///   a value indicating whether a pong has been received from
        ///   the client within a time.
        ///   </para>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        #endregion Description
        [Obsolete("This method will be removed.")]
        public Dictionary<string, bool> Broadping()
        {
            if (_state != ServerState.Start)
                throw new InvalidOperationException("The current state of the manager is not Start.");
            return Broadping(WebSocketFrame.EmptyPingBytes, _waitTime);
        }

        #region Description
        /// <summary>
        /// Sends a ping with <paramref name="message"/> to every client in
        /// the WebSocket service.
        /// </summary>
        /// <returns>
        ///   <para>
        ///   A <c>Dictionary&lt;string, bool&gt;</c>.
        ///   </para>
        ///   <para>
        ///   It represents a collection of pairs of a session ID and
        ///   a value indicating whether a pong has been received from
        ///   the client within a time.
        ///   </para>
        /// </returns>
        /// <param name="message">
        ///   <para>
        ///   A <see cref="string"/> that represents the message to send.
        ///   </para>
        ///   <para>
        ///   The size must be 125 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The current state of the manager is not Start.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="message"/> could not be UTF-8-encoded.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="message"/> is greater than 125 bytes.
        /// </exception>
        #endregion Description
        [Obsolete("This method will be removed.")]
        public Dictionary<string, bool> Broadping(string message)
        {
            if (_state != ServerState.Start)
                throw new InvalidOperationException("The current state of the manager is not Start.");

            if (message.IsNullOrEmpty())
                return Broadping(WebSocketFrame.EmptyPingBytes, _waitTime);

            if (!message.TryGetUTF8EncodedBytes(out byte[] bytes))
                throw new ArgumentException("It could not be UTF-8-encoded.", "message");

            if (bytes.Length > 125)
                throw new ArgumentOutOfRangeException("message", "Its size is greater than 125 bytes.");

            return Broadping(WebSocketFrame.CreatePingFrame(bytes, false).ToArray(), _waitTime);
        }

        #region Description
        /// <summary>
        /// Closes the specified session.
        /// </summary>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to close.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        #endregion Description
        public void CloseSession(string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.Close();
        }

        #region Description
        /// <summary>
        /// Closes the specified session with <paramref name="code"/> and
        /// <paramref name="reason"/>.
        /// </summary>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to close.
        /// </param>
        /// <param name="code">
        ///   <para>
        ///   A <see cref="ushort"/> that represents the status code indicating
        ///   the reason for the close.
        ///   </para>
        ///   <para>
        ///   The status codes are defined in
        ///   <see href="http://tools.ietf.org/html/rfc6455#section-7.4">
        ///   Section 7.4</see> of RFC 6455.
        ///   </para>
        /// </param>
        /// <param name="reason">
        ///   <para>
        ///   A <see cref="string"/> that represents the reason for the close.
        ///   </para>
        ///   <para>
        ///   The size must be 123 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is 1010 (mandatory extension).
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is 1005 (no status) and there is
        ///   <paramref name="reason"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///   <paramref name="code"/> is less than 1000 or greater than 4999.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The size of <paramref name="reason"/> is greater than 123 bytes.
        ///   </para>
        /// </exception>
        #endregion Description
        public void CloseSession(string id, ushort code, string reason)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.Close(code, reason);
        }

        #region Description
        /// <summary>
        /// Closes the specified session with <paramref name="code"/> and
        /// <paramref name="reason"/>.
        /// </summary>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to close.
        /// </param>
        /// <param name="code">
        ///   <para>
        ///   One of the <see cref="CloseStatusCode"/> enum values.
        ///   </para>
        ///   <para>
        ///   It represents the status code indicating the reason for the close.
        ///   </para>
        /// </param>
        /// <param name="reason">
        ///   <para>
        ///   A <see cref="string"/> that represents the reason for the close.
        ///   </para>
        ///   <para>
        ///   The size must be 123 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.MandatoryExtension"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is
        ///   <see cref="CloseStatusCode.NoStatus"/> and there is
        ///   <paramref name="reason"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="reason"/> is greater than 123 bytes.
        /// </exception>
        #endregion Description
        public void CloseSession(string id, CloseStatusCode code, string reason)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.Close(code, reason);
        }

        #region Description
        /// <summary>
        /// Sends a ping to the client using the specified session.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the send has done with no error and a pong has been
        /// received from the client within a time; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        #endregion Description
        public bool PingTo(string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            return session.Context.WebSocket.Ping();
        }

        #region Description
        /// <summary>
        /// Sends a ping with <paramref name="message"/> to the client using
        /// the specified session.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the send has done with no error and a pong has been
        /// received from the client within a time; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="message">
        ///   <para>
        ///   A <see cref="string"/> that represents the message to send.
        ///   </para>
        ///   <para>
        ///   The size must be 125 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="message"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The session could not be found.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="message"/> is greater than 125 bytes.
        /// </exception>
        #endregion Description
        public bool PingTo(string message, string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            return session.Context.WebSocket.Ping(message);
        }

        #region Description
        /// <summary>
        /// Sends <paramref name="data"/> to the client using the specified session.
        /// </summary>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        #endregion Description
        public void SendTo(byte[] data, string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.Send(data);
        }

        #region Description
        /// <summary>
        /// Sends <paramref name="data"/> to the client using the specified session.
        /// </summary>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        #endregion Description
        public void SendTo(string data, string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.Send(data);
        }

        #region Description
        /// <summary>
        /// Sends the data from <paramref name="stream"/> to the client using
        /// the specified session.
        /// </summary>
        /// <remarks>
        /// The data is sent as the binary data.
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> instance from which to read the data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="stream"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        #endregion Description
        public void SendTo(Stream stream, int length, string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.Send(stream, length);
        }

        #region Description
        /// <summary>
        /// Sends <paramref name="data"/> asynchronously to the client using
        /// the specified session.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <c>Action&lt;bool&gt;</c> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   <c>true</c> is passed to the method if the send has done with
        ///   no error; otherwise, <c>false</c>.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        #endregion Description
        public void SendToAsync(byte[] data, string id, Action<bool> completed)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.SendAsync(data, completed);
        }

        #region Description
        /// <summary>
        /// Sends <paramref name="data"/> asynchronously to the client using
        /// the specified session.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <c>Action&lt;bool&gt;</c> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   <c>true</c> is passed to the method if the send has done with
        ///   no error; otherwise, <c>false</c>.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="data"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        #endregion Description
        public void SendToAsync(string data, string id, Action<bool> completed)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.SendAsync(data, completed);
        }

        #region Description
        /// <summary>
        /// Sends the data from <paramref name="stream"/> asynchronously to
        /// the client using the specified session.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The data is sent as the binary data.
        ///   </para>
        ///   <para>
        ///   This method does not wait for the send to be complete.
        ///   </para>
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> instance from which to read the data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <c>Action&lt;bool&gt;</c> delegate or <see langword="null"/>
        ///   if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   <c>true</c> is passed to the method if the send has done with
        ///   no error; otherwise, <c>false</c>.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <para>
        ///   <paramref name="id"/> is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="stream"/> is <see langword="null"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="id"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session could not be found.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket connection is not Open.
        ///   </para>
        /// </exception>
        #endregion Description
        public void SendToAsync(Stream stream, int length, string id, Action<bool> completed)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
                throw new InvalidOperationException("The session could not be found.");
            session.Context.WebSocket.SendAsync(stream, length, completed);
        }

        #region Description
        /// <summary>
        /// Cleans up the inactive sessions in the WebSocket service.
        /// </summary>
        #endregion Description
        public void Sweep()
        {
            if (_sweeping)
            {
                _log.Info("The sweeping is already in progress.");
                return;
            }
            _sweeping = true;

            foreach (var id in InactiveIDs)
            {
                if (_state != ServerState.Start)
                    break;
                if (_sessions.TryGetValue(id, out IWebSocketSession session))
                {
                    var state = session.ConnectionState;
                    if (state == WebSocketState.Open)
                        session.Context.WebSocket.Close(CloseStatusCode.Abnormal);
                    else if (state == WebSocketState.Closing)
                        continue;
                    else
                        _sessions.TryRemove(id, out var removedItem);
                }
            }

            _sweeping = false;
        }

        #region Description
        /// <summary>
        /// Tries to get the session instance with <paramref name="id"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the session is successfully found; otherwise,
        /// <c>false</c>.
        /// </returns>
        /// <param name="id">
        /// A <see cref="string"/> that represents the ID of the session to find.
        /// </param>
        /// <param name="session">
        ///   <para>
        ///   When this method returns, a <see cref="IWebSocketSession"/>
        ///   instance or <see langword="null"/> if not found.
        ///   </para>
        ///   <para>
        ///   The session instance provides the function to access
        ///   the information in the session.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is an empty string.
        /// </exception>
        #endregion Description
        public bool TryGetSession(string id, out IWebSocketSession session)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (id.Length == 0)
                throw new ArgumentException("An empty string.", "id");
            return tryGetSession(id, out session);
        }

        #endregion
    }
}