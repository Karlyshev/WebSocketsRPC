#region License
/*
 * WebSocketServer.cs
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

#region Contributors
/*
 * Contributors:
 * - Juan Manuel Lallana <juan.manuel.lallana@gmail.com>
 * - Jonas Hovgaard <j@jhovgaard.dk>
 * - Liryna <liryna.stark@gmail.com>
 * - Rohan Singh <rohan-singh@hotmail.com>
 */
#endregion

using System;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using WebSocketsRPC.Net;
using WebSocketsRPC.Net.WebSockets;

namespace WebSocketsRPC.Server
{
    #region Description
    /// <summary>
    /// Provides a WebSocket protocol server.
    /// </summary>
    /// <remarks>
    /// This class can provide multiple WebSocket services.
    /// </remarks>
    #endregion Description
    public class WebSocketServer
    {
        #region Private Fields

        private System.Net.IPAddress _address;
        private bool _allowForwardedRequest;
        private AuthenticationSchemes _authSchemes;
        private static readonly string _defaultRealm;
        private bool _dnsStyle;
        private string _hostname;
        private TcpListener _listener;
        private Logger _log;
        private int _port;
        private string _realm;
        private string _realmInUse;
        private Thread _receiveThread;
        private bool _reuseAddress;
        private bool _secure;
        private WebSocketServiceManager _services;
        private ServerSslConfiguration _sslConfig;
        private ServerSslConfiguration _sslConfigInUse;
        private volatile ServerState _state;
        private object _sync;
        private Func<IIdentity, NetworkCredential> _userCredFinder;

        #endregion

        #region Static Constructor

        static WebSocketServer() => _defaultRealm = "SECRET AREA";

        #endregion

        #region Public Constructors

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class.
        /// </summary>
        /// <remarks>
        /// The new instance listens for incoming handshake requests on
        /// <see cref="System.Net.IPAddress.Any"/> and port 80.
        /// </remarks>
        #endregion Description
        public WebSocketServer()
        {
            var addr = System.Net.IPAddress.Any;
            init(addr.ToString(), addr, 80, false);
        }

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified <paramref name="port"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The new instance listens for incoming handshake requests on
        ///   <see cref="System.Net.IPAddress.Any"/> and <paramref name="port"/>.
        ///   </para>
        ///   <para>
        ///   It provides secure connections if <paramref name="port"/> is 443.
        ///   </para>
        /// </remarks>
        /// <param name="port">
        /// An <see cref="int"/> that represents the number of the port
        /// on which to listen.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="port"/> is less than 1 or greater than 65535.
        /// </exception>
        #endregion Description
        public WebSocketServer(int port) : this(port, port == 443)
        {
        }

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified <paramref name="url"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The new instance listens for incoming handshake requests on
        ///   the IP address of the host of <paramref name="url"/> and
        ///   the port of <paramref name="url"/>.
        ///   </para>
        ///   <para>
        ///   Either port 80 or 443 is used if <paramref name="url"/> includes
        ///   no port. Port 443 is used if the scheme of <paramref name="url"/>
        ///   is wss; otherwise, port 80 is used.
        ///   </para>
        ///   <para>
        ///   The new instance provides secure connections if the scheme of
        ///   <paramref name="url"/> is wss.
        ///   </para>
        /// </remarks>
        /// <param name="url">
        /// A <see cref="string"/> that represents the WebSocket URL of the server.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="url"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="url"/> is invalid.
        ///   </para>
        /// </exception>
        #endregion Description
        public WebSocketServer(string url)
        {
            if (url == null)
                throw new ArgumentNullException("url");
            if (url.Length == 0)
                throw new ArgumentException("An empty string.", "url");
            if (!tryCreateUri(url, out Uri uri, out string msg))
                throw new ArgumentException(msg, "url");
            var host = uri.DnsSafeHost;
            var addr = host.ToIPAddress();
            if (addr == null)
                throw new ArgumentException("The host part could not be converted to an IP address.", "url");
            if (!addr.IsLocal())
                throw new ArgumentException("The IP address of the host is not a local IP address.", "url");
            init(host, addr, uri.Port, uri.Scheme == "wss");
        }

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified <paramref name="port"/> and <paramref name="secure"/>.
        /// </summary>
        /// <remarks>
        /// The new instance listens for incoming handshake requests on
        /// <see cref="System.Net.IPAddress.Any"/> and <paramref name="port"/>.
        /// </remarks>
        /// <param name="port">
        /// An <see cref="int"/> that represents the number of the port
        /// on which to listen.
        /// </param>
        /// <param name="secure">
        /// A <see cref="bool"/>: <c>true</c> if the new instance provides
        /// secure connections; otherwise, <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="port"/> is less than 1 or greater than 65535.
        /// </exception>
        #endregion Description
        public WebSocketServer(int port, bool secure)
        {
            if (!port.IsPortNumber())
                throw new ArgumentOutOfRangeException("port", "Less than 1 or greater than 65535.");
            var addr = System.Net.IPAddress.Any;
            init(addr.ToString(), addr, port, secure);
        }

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified <paramref name="address"/> and <paramref name="port"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The new instance listens for incoming handshake requests on
        ///   <paramref name="address"/> and <paramref name="port"/>.
        ///   </para>
        ///   <para>
        ///   It provides secure connections if <paramref name="port"/> is 443.
        ///   </para>
        /// </remarks>
        /// <param name="address">
        /// A <see cref="System.Net.IPAddress"/> that represents the local
        /// IP address on which to listen.
        /// </param>
        /// <param name="port">
        /// An <see cref="int"/> that represents the number of the port
        /// on which to listen.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="address"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="address"/> is not a local IP address.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="port"/> is less than 1 or greater than 65535.
        /// </exception>
        #endregion Description
        public WebSocketServer(System.Net.IPAddress address, int port) : this(address, port, port == 443)
        {
        }

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified <paramref name="address"/>, <paramref name="port"/>,
        /// and <paramref name="secure"/>.
        /// </summary>
        /// <remarks>
        /// The new instance listens for incoming handshake requests on
        /// <paramref name="address"/> and <paramref name="port"/>.
        /// </remarks>
        /// <param name="address">
        /// A <see cref="System.Net.IPAddress"/> that represents the local
        /// IP address on which to listen.
        /// </param>
        /// <param name="port">
        /// An <see cref="int"/> that represents the number of the port
        /// on which to listen.
        /// </param>
        /// <param name="secure">
        /// A <see cref="bool"/>: <c>true</c> if the new instance provides
        /// secure connections; otherwise, <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="address"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="address"/> is not a local IP address.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="port"/> is less than 1 or greater than 65535.
        /// </exception>
        #endregion Description
        public WebSocketServer(System.Net.IPAddress address, int port, bool secure)
        {
            if (address == null)
                throw new ArgumentNullException("address");
            if (!address.IsLocal())
                throw new ArgumentException("Not a local IP address.", "address");
            if (!port.IsPortNumber())
                throw new ArgumentOutOfRangeException("port", "Less than 1 or greater than 65535.");
            init(address.ToString(), address, port, secure);
        }

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets the IP address of the server.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPAddress"/> that represents the local
        /// IP address on which to listen for incoming handshake requests.
        /// </value>
        #endregion Description
        public System.Net.IPAddress Address => _address;

        #region Description
        /// <summary>
        /// Gets or sets a value indicating whether the server accepts every
        /// handshake request without checking the request URI.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the server has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the server accepts every handshake request without
        ///   checking the request URI; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        #endregion Description
        public bool AllowForwardedRequest
        {
            get => _allowForwardedRequest;
            set
            {
                string msg;
                if (!canSet(out msg))
                {
                    _log.Warn(msg);
                    return;
                }

                lock (_sync)
                {
                    _allowForwardedRequest = value;
                }
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the scheme used to authenticate the clients.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the server has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   One of the <see cref="WebSocketsRPC.Net.AuthenticationSchemes"/>
        ///   enum values.
        ///   </para>
        ///   <para>
        ///   It represents the scheme used to authenticate the clients.
        ///   </para>
        ///   <para>
        ///   The default value is
        ///   <see cref="WebSocketsRPC.Net.AuthenticationSchemes.Anonymous"/>.
        ///   </para>
        /// </value>
        #endregion Description
        public AuthenticationSchemes AuthenticationSchemes
        {
            get => _authSchemes;
            set
            {
                if (!canSet(out string msg))
                {
                    _log.Warn(msg);
                    return;
                }
                lock (_sync)
                {
                    _authSchemes = value;
                }
            }
        }

        #region Description
        /// <summary>
        /// Gets a value indicating whether the server has started.
        /// </summary>
        /// <value>
        /// <c>true</c> if the server has started; otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public bool IsListening => _state == ServerState.Start;

        #region Description
        /// <summary>
        /// Gets a value indicating whether secure connections are provided.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance provides secure connections; otherwise,
        /// <c>false</c>.
        /// </value>
        #endregion Description
        public bool IsSecure => _secure;

        #region Description
        /// <summary>
        /// Gets or sets a value indicating whether the server cleans up
        /// the inactive sessions periodically.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the server has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the server cleans up the inactive sessions every
        ///   60 seconds; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>true</c>.
        ///   </para>
        /// </value>
        #endregion Description
        public bool KeepClean { get => _services.KeepClean; set => _services.KeepClean = value; }

        #region Description
        /// <summary>
        /// Gets the logging function for the server.
        /// </summary>
        /// <remarks>
        /// The default logging level is <see cref="LogLevel.Error"/>.
        /// </remarks>
        /// <value>
        /// A <see cref="Logger"/> that provides the logging function.
        /// </value>
        #endregion Description
        public Logger Log => _log;

        #region Description
        /// <summary>
        /// Gets the port of the server.
        /// </summary>
        /// <value>
        /// An <see cref="int"/> that represents the number of the port
        /// on which to listen for incoming handshake requests.
        /// </value>
        #endregion Description
        public int Port => _port;

        #region Description
        /// <summary>
        /// Gets or sets the realm used for authentication.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   "SECRET AREA" is used as the realm if the value is
        ///   <see langword="null"/> or an empty string.
        ///   </para>
        ///   <para>
        ///   The set operation does nothing if the server has
        ///   already started or it is shutting down.
        ///   </para>
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> or <see langword="null"/> by default.
        ///   </para>
        ///   <para>
        ///   That string represents the name of the realm.
        ///   </para>
        /// </value>
        #endregion Description
        public string Realm
        {
            get => _realm;
            set
            {
                if (!canSet(out string msg))
                {
                    _log.Warn(msg);
                    return;
                }
                lock (_sync)
                {
                    _realm = value;
                }
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets a value indicating whether the server is allowed to
        /// be bound to an address that is already in use.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   You should set this property to <c>true</c> if you would
        ///   like to resolve to wait for socket in TIME_WAIT state.
        ///   </para>
        ///   <para>
        ///   The set operation does nothing if the server has already
        ///   started or it is shutting down.
        ///   </para>
        /// </remarks>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the server is allowed to be bound to an address
        ///   that is already in use; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        #endregion Description
        public bool ReuseAddress
        {
            get => _reuseAddress;

            set
            {
                if (!canSet(out string msg))
                {
                    _log.Warn(msg);
                    return;
                }
                lock (_sync)
                {
                    _reuseAddress = value;
                }
            }
        }

        #region Description
        /// <summary>
        /// Gets the configuration for secure connection.
        /// </summary>
        /// <remarks>
        /// This configuration will be referenced when attempts to start,
        /// so it must be configured before the start method is called.
        /// </remarks>
        /// <value>
        /// A <see cref="ServerSslConfiguration"/> that represents
        /// the configuration used to provide secure connections.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// This instance does not provide secure connections.
        /// </exception>
        #endregion Description
        public ServerSslConfiguration SslConfiguration
        {
            get
            {
                if (!_secure)
                    throw new InvalidOperationException("This instance does not provide secure connections.");
                return getSslConfiguration();
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the delegate used to find the credentials
        /// for an identity.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   No credentials are found if the method invoked by
        ///   the delegate returns <see langword="null"/> or
        ///   the value is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   The set operation does nothing if the server has
        ///   already started or it is shutting down.
        ///   </para>
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <c>Func&lt;<see cref="IIdentity"/>,
        ///   <see cref="NetworkCredential"/>&gt;</c> delegate or
        ///   <see langword="null"/> if not needed.
        ///   </para>
        ///   <para>
        ///   That delegate invokes the method called for finding
        ///   the credentials used to authenticate a client.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        #endregion Description
        public Func<IIdentity, NetworkCredential> UserCredentialsFinder
        {
            get => _userCredFinder;
            set
            {
                if (!canSet(out string msg))
                {
                    _log.Warn(msg);
                    return;
                }
                lock (_sync)
                {
                    _userCredFinder = value;
                }
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the time to wait for the response to the WebSocket Ping or
        /// Close.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the server has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="TimeSpan"/> to wait for the response.
        ///   </para>
        ///   <para>
        ///   The default value is the same as 1 second.
        ///   </para>
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value specified for a set operation is zero or less.
        /// </exception>
        #endregion Description
        public TimeSpan WaitTime { get => _services.WaitTime; set => _services.WaitTime = value; }

        #region Description
        /// <summary>
        /// Gets the management function for the WebSocket services
        /// provided by the server.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketServiceManager"/> that manages
        /// the WebSocket services provided by the server.
        /// </value>
        #endregion Description
        public WebSocketServiceManager WebSocketServices => _services;

        #endregion

        #region Private Methods

        private void abort()
        {
            if (_state != ServerState.Start)
                return;
            _state = ServerState.ShuttingDown;

            try
            {
                try
                {
                    _listener.Stop();
                }
                finally
                {
                    _services.Stop(1006, string.Empty);
                }
            }
            catch
            {
            }

            _state = ServerState.Stop;
        }

        private bool authenticateClient(TcpListenerWebSocketContext context)
        {
            if (_authSchemes == AuthenticationSchemes.Anonymous)
                return true;
            if (_authSchemes == AuthenticationSchemes.None)
                return false;
            return context.Authenticate(_authSchemes, _realmInUse, _userCredFinder);
        }

        private bool canSet(out string message)
        {
            message = null;
            if (_state == ServerState.Start)
            {
                message = "The server has already started.";
                return false;
            }
            if (_state == ServerState.ShuttingDown)
            {
                message = "The server is shutting down.";
                return false;
            }
            return true;
        }

        private bool checkHostNameForRequest(string name) => !_dnsStyle || Uri.CheckHostName(name) != UriHostNameType.Dns || name == _hostname;

        private static bool checkSslConfiguration(ServerSslConfiguration configuration, out string message)
        {
            message = null;
            if (configuration.ServerCertificate == null)
            {
                message = "There is no server certificate for secure connection.";
                return false;
            }
            return true;
        }

        private string getRealm()
        {
            var realm = _realm;
            return realm != null && realm.Length > 0 ? realm : _defaultRealm;
        }

        private ServerSslConfiguration getSslConfiguration()
        {
            if (_sslConfig == null)
                _sslConfig = new ServerSslConfiguration();
            return _sslConfig;
        }

        private void init(string hostname, System.Net.IPAddress address, int port, bool secure)
        {
            _hostname = hostname;
            _address = address;
            _port = port;
            _secure = secure;

            _authSchemes = AuthenticationSchemes.Anonymous;
            _dnsStyle = Uri.CheckHostName(hostname) == UriHostNameType.Dns;
            _listener = new TcpListener(address, port);
            _log = new Logger();
            _services = new WebSocketServiceManager(_log);
            _sync = new object();
        }

        private void processRequest(TcpListenerWebSocketContext context)
        {
            if (!authenticateClient(context))
            {
                context.Close(HttpStatusCode.Forbidden);
                return;
            }
            var uri = context.RequestUri;
            if (uri == null)
            {
                context.Close(HttpStatusCode.BadRequest);
                return;
            }
            if (!_allowForwardedRequest)
            {
                if (uri.Port != _port)
                {
                    context.Close(HttpStatusCode.BadRequest);
                    return;
                }
                if (!checkHostNameForRequest(uri.DnsSafeHost))
                {
                    context.Close(HttpStatusCode.NotFound);
                    return;
                }
            }
            var path = uri.AbsolutePath;
            if (path.IndexOfAny(new[] { '%', '+' }) > -1)
                path = HttpUtility.UrlDecode(path, Encoding.UTF8);
            if (!_services.InternalTryGetServiceHost(path, out WebSocketServiceHost host))
            {
                context.Close(HttpStatusCode.NotImplemented);
                return;
            }
            host.StartSession(context);
        }

        private void receiveRequest()
        {
            while (true)
            {
                TcpClient cl = null;
                try
                {
                    cl = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(state => {
                        try
                        {
                            processRequest(new TcpListenerWebSocketContext(cl, null, _secure, _sslConfigInUse, _log));
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.Message);
                            _log.Debug(ex.ToString());
                            cl.Close();
                        }
                    });
                }
                catch (SocketException ex)
                {
                    if (_state == ServerState.ShuttingDown)
                    {
                        _log.Info("The underlying listener is stopped.");
                        break;
                    }
                    _log.Fatal(ex.Message);
                    _log.Debug(ex.ToString());

                    break;
                }
                catch (Exception ex)
                {
                    _log.Fatal(ex.Message);
                    _log.Debug(ex.ToString());
                    if (cl != null)
                        cl.Close();
                    break;
                }
            }

            if (_state != ServerState.ShuttingDown)
                abort();
        }

        private void start(ServerSslConfiguration sslConfig)
        {
            if (_state == ServerState.Start)
            {
                _log.Info("The server has already started.");
                return;
            }

            if (_state == ServerState.ShuttingDown)
            {
                _log.Warn("The server is shutting down.");
                return;
            }

            lock (_sync)
            {
                _sslConfigInUse = sslConfig;
                _realmInUse = getRealm();

                _services.Start();
                try
                {
                    startReceiving();
                }
                catch
                {
                    _services.Stop(1011, string.Empty);
                    throw;
                }

                _state = ServerState.Start;
            }
        }

        private void startReceiving()
        {
            if (_reuseAddress)
            {
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }

            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                var msg = "The underlying listener has failed to start.";
                throw new InvalidOperationException(msg, ex);
            }

            _receiveThread = new Thread(new ThreadStart(receiveRequest));
            _receiveThread.IsBackground = true;
            _receiveThread.Start();
        }

        private void stop(ushort code, string reason)
        {
            if (_state == ServerState.Ready)
            {
                _log.Info("The server is not started.");
                return;
            }

            if (_state == ServerState.ShuttingDown)
            {
                _log.Info("The server is shutting down.");
                return;
            }

            if (_state == ServerState.Stop)
            {
                _log.Info("The server has already stopped.");
                return;
            }

            _state = ServerState.ShuttingDown;

            try
            {
                var threw = false;
                try
                {
                    stopReceiving(5000);
                }
                catch
                {
                    threw = true;
                    throw;
                }
                finally
                {
                    try
                    {
                        _services.Stop(code, reason);
                    }
                    catch
                    {
                        if (!threw)
                            throw;
                    }
                }
            }
            finally
            {
                _state = ServerState.Stop;
            }
        }

        private void stopReceiving(int millisecondsTimeout)
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("The underlying listener has failed to stop.", ex);
            }

            _receiveThread.Join(millisecondsTimeout);
        }

        private static bool tryCreateUri(string uriString, out Uri result, out string message)
        {
            if (!uriString.TryCreateWebSocketUri(out result, out message))
                return false;

            if (result.PathAndQuery != "/")
            {
                result = null;
                message = "It includes either or both path and query components.";

                return false;
            }

            return true;
        }

        #endregion

        #region Public Methods

        #region Description
        /// <summary>
        /// Adds a WebSocket service with the specified behavior and path.
        /// </summary>
        /// <param name="path">
        ///   <para>
        ///   A <see cref="string"/> that represents an absolute path to
        ///   the service to add.
        ///   </para>
        ///   <para>
        ///   / is trimmed from the end of the string if present.
        ///   </para>
        /// </param>
        /// <typeparam name="TBehaviorWithNew">
        ///   <para>
        ///   The type of the behavior for the service.
        ///   </para>
        ///   <para>
        ///   It must inherit the <see cref="WebSocketBehavior"/> class.
        ///   </para>
        ///   <para>
        ///   And also, it must have a public parameterless constructor.
        ///   </para>
        /// </typeparam>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="path"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is not an absolute path.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> includes either or both
        ///   query and fragment components.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is already in use.
        ///   </para>
        /// </exception>
        #endregion Description
        public WebSocketServiceHost AddWebSocketService<TBehaviorWithNew>(string path) where TBehaviorWithNew : WebSocketBehavior, new() => _services.AddService<TBehaviorWithNew>(path, null);

        #region Description
        /// <summary>
        /// Adds a WebSocket service with the specified behavior, path,
        /// and delegate.
        /// </summary>
        /// <param name="path">
        ///   <para>
        ///   A <see cref="string"/> that represents an absolute path to
        ///   the service to add.
        ///   </para>
        ///   <para>
        ///   / is trimmed from the end of the string if present.
        ///   </para>
        /// </param>
        /// <param name="initializer">
        ///   <para>
        ///   An <c>Action&lt;TBehaviorWithNew&gt;</c> delegate or
        ///   <see langword="null"/> if not needed.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when initializing
        ///   a new session instance for the service.
        ///   </para>
        /// </param>
        /// <typeparam name="TBehaviorWithNew">
        ///   <para>
        ///   The type of the behavior for the service.
        ///   </para>
        ///   <para>
        ///   It must inherit the <see cref="WebSocketBehavior"/> class.
        ///   </para>
        ///   <para>
        ///   And also, it must have a public parameterless constructor.
        ///   </para>
        /// </typeparam>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="path"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is not an absolute path.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> includes either or both
        ///   query and fragment components.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is already in use.
        ///   </para>
        /// </exception>
        #endregion Description
        public void AddWebSocketService<TBehaviorWithNew>(string path, Action<TBehaviorWithNew> initializer) where TBehaviorWithNew : WebSocketBehavior, new() => _services.AddService<TBehaviorWithNew>(path, initializer);

        #region Description
        /// <summary>
        /// Removes a WebSocket service with the specified path.
        /// </summary>
        /// <remarks>
        /// The service is stopped with close status 1001 (going away)
        /// if it has already started.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the service is successfully found and removed;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <param name="path">
        ///   <para>
        ///   A <see cref="string"/> that represents an absolute path to
        ///   the service to remove.
        ///   </para>
        ///   <para>
        ///   / is trimmed from the end of the string if present.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="path"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is not an absolute path.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> includes either or both
        ///   query and fragment components.
        ///   </para>
        /// </exception>
        #endregion Description
        public bool RemoveWebSocketService(string path) => _services.RemoveService(path);

        #region Description
        /// <summary>
        /// Starts receiving incoming handshake requests.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the server has already started or
        /// it is shutting down.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   There is no server certificate for secure connection.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The underlying <see cref="TcpListener"/> has failed to start.
        ///   </para>
        /// </exception>
        #endregion Description
        public void Start()
        {
            ServerSslConfiguration sslConfig = null;
            if (_secure)
            {
                sslConfig = new ServerSslConfiguration(getSslConfiguration());
                if (!checkSslConfiguration(sslConfig, out string msg))
                    throw new InvalidOperationException(msg);
            }
            start(sslConfig);
        }

        #region Description
        /// <summary>
        /// Stops receiving incoming handshake requests.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="TcpListener"/> has failed to stop.
        /// </exception>
        #endregion Description
        public void Stop() => stop(1001, string.Empty);

        #endregion
    }
}