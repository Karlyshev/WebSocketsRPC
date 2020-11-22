#region License
/*
 * TcpListenerWebSocketContext.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2018 sta.blockhead
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
 * - Liryna <liryna.stark@gmail.com>
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;

namespace WebSocketsRPC.Net.WebSockets
{
    #region Description
    /// <summary>
    /// Provides the access to the information in a WebSocket handshake request to
    /// a <see cref="TcpListener"/> instance.
    /// </summary>
    #endregion Description
    internal class TcpListenerWebSocketContext : WebSocketContext
    {
        #region Private Fields

        private Logger _log;
        private NameValueCollection _queryString;
        private HttpRequest _request;
        private Uri _requestUri;
        private bool _secure;
        private System.Net.EndPoint _serverEndPoint;
        private Stream _stream;
        private TcpClient _tcpClient;
        private IPrincipal _user;
        private System.Net.EndPoint _userEndPoint;
        private WebSocket _websocket;

        #endregion

        #region Internal Constructors

        internal TcpListenerWebSocketContext(TcpClient tcpClient, string protocol, bool secure, ServerSslConfiguration sslConfig, Logger log)
        {
            _tcpClient = tcpClient;
            _secure = secure;
            _log = log;

            var netStream = tcpClient.GetStream();
            if (secure)
            {
                var sslStream = new SslStream(netStream, false, sslConfig.ClientCertificateValidationCallback);
                sslStream.AuthenticateAsServer(sslConfig.ServerCertificate, sslConfig.ClientCertificateRequired, sslConfig.EnabledSslProtocols, sslConfig.CheckCertificateRevocation);
                _stream = sslStream;
            }
            else
            {
                _stream = netStream;
            }

            var sock = tcpClient.Client;
            _serverEndPoint = sock.LocalEndPoint;
            _userEndPoint = sock.RemoteEndPoint;
            _request = HttpRequest.Read(_stream, 90000);
            _websocket = new WebSocket(this, protocol);
        }

        #endregion

        #region Internal Properties

        internal Logger Log => _log;

        internal Stream Stream => _stream;

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets the HTTP cookies included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="WebSocketsRPC.Net.CookieCollection"/> that contains
        ///   the cookies.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        #endregion Description
        public override CookieCollection CookieCollection => _request.Cookies;

        #region Description
        /// <summary>
        /// Gets the HTTP headers included in the handshake request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        #endregion Description
        public override NameValueCollection Headers => _request.Headers;

        #region Description
        /// <summary>
        /// Gets the value of the Host header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the server host name requested
        ///   by the client.
        ///   </para>
        ///   <para>
        ///   It includes the port number if provided.
        ///   </para>
        /// </value>
        #endregion Description
        public override string Host => _request.Headers["Host"];

        #region Description
        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public override bool IsAuthenticated => _user != null;

        #region Description
        /// <summary>
        /// Gets a value indicating whether the handshake request is sent from
        /// the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the handshake request is sent from the same computer
        /// as the server; otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public override bool IsLocal => UserEndPoint.Address.IsLocal();

        #region Description
        /// <summary>
        /// Gets a value indicating whether a secure connection is used to send
        /// the handshake request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public override bool IsSecureConnection => _secure;

        #region Description
        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket handshake
        /// request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket handshake request; otherwise,
        /// <c>false</c>.
        /// </value>
        #endregion Description
        public override bool IsWebSocketRequest => _request.IsWebSocketRequest;

        #region Description
        /// <summary>
        /// Gets the value of the Origin header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of the Origin header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public override string Origin => _request.Headers["Origin"];

        #region Description
        /// <summary>
        /// Gets the query string included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="NameValueCollection"/> that contains the query
        ///   parameters.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        #endregion Description
        public override NameValueCollection QueryString
        {
            get
            {
                if (_queryString == null)
                {
                    var uri = RequestUri;
                    _queryString = QueryStringCollection.Parse(uri != null ? uri.Query : null,Encoding.UTF8);
                }
                return _queryString;
            }
        }

        #region Description
        /// <summary>
        /// Gets the URI requested by the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Uri"/> that represents the URI parsed from the request.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the URI cannot be parsed.
        ///   </para>
        /// </value>
        #endregion Description
        public override Uri RequestUri
        {
            get
            {
                if (_requestUri == null)
                    _requestUri = HttpUtility.CreateRequestUrl(_request.RequestUri, _request.Headers["Host"], _request.IsWebSocketRequest, _secure);
                return _requestUri;
            }
        }

        #region Description
        /// <summary>
        /// Gets the value of the Sec-WebSocket-Key header included in
        /// the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of
        ///   the Sec-WebSocket-Key header.
        ///   </para>
        ///   <para>
        ///   The value is used to prove that the server received
        ///   a valid WebSocket handshake request.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public override string SecWebSocketKey => _request.Headers["Sec-WebSocket-Key"];

        #region Description
        /// <summary>
        /// Gets the names of the subprotocols from the Sec-WebSocket-Protocol
        /// header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <see cref="T:System.Collections.Generic.IEnumerable{string}"/>
        ///   instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the names of the subprotocols.
        ///   </para>
        /// </value>
        #endregion Description
        public override IEnumerable<string> SecWebSocketProtocols
        {
            get
            {
                var val = _request.Headers["Sec-WebSocket-Protocol"];
                if (val == null || val.Length == 0)
                    yield break;

                foreach (var elm in val.Split(','))
                {
                    var protocol = elm.Trim();
                    if (protocol.Length == 0)
                        continue;

                    yield return protocol;
                }
            }
        }

        #region Description
        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in
        /// the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the WebSocket protocol
        ///   version specified by the client.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public override string SecWebSocketVersion => _request.Headers["Sec-WebSocket-Version"];

        #region Description
        /// <summary>
        /// Gets the endpoint to which the handshake request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server IP
        /// address and port number.
        /// </value>
        #endregion Description
        public override System.Net.IPEndPoint ServerEndPoint => (System.Net.IPEndPoint)_serverEndPoint;

        #region Description
        /// <summary>
        /// Gets the client information.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="IPrincipal"/> instance that represents identity,
        ///   authentication, and security roles for the client.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the client is not authenticated.
        ///   </para>
        /// </value>
        #endregion Description
        public override IPrincipal User => _user;

        #region Description
        /// <summary>
        /// Gets the endpoint from which the handshake request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client IP
        /// address and port number.
        /// </value>
        #endregion Description
        public override System.Net.IPEndPoint UserEndPoint => (System.Net.IPEndPoint)_userEndPoint;

        #region Description
        /// <summary>
        /// Gets the WebSocket instance used for two-way communication between
        /// the client and server.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketsRPC.WebSocket"/>.
        /// </value>
        #endregion Description
        public override WebSocket WebSocket => _websocket;

        #endregion

        #region Private Methods

        private HttpRequest sendAuthenticationChallenge(string challenge)
        {
            var bytes = HttpResponse.CreateUnauthorizedResponse(challenge).ToByteArray();
            _stream.Write(bytes, 0, bytes.Length);
            return HttpRequest.Read(_stream, 15000);
        }

        #endregion

        #region Internal Methods

        internal bool Authenticate(AuthenticationSchemes scheme, string realm, Func<IIdentity, NetworkCredential> credentialsFinder)
        {
            var chal = new AuthenticationChallenge(scheme, realm).ToString();
            var retry = -1;
            Func<bool> auth = null;
            auth = () => {
                retry++;
                if (retry > 99)
                    return false;

                var user = HttpUtility.CreateUser(_request.Headers["Authorization"], scheme, realm, _request.HttpMethod, credentialsFinder);
                if (user != null && user.Identity.IsAuthenticated)
                {
                    _user = user;
                    return true;
                }

                _request = sendAuthenticationChallenge(chal);
                return auth();
            };

            return auth();
        }

        internal void Close()
        {
            _stream.Close();
            _tcpClient.Close();
        }

        internal void Close(HttpStatusCode code)
        {
            var bytes = HttpResponse.CreateCloseResponse(code).ToByteArray();
            _stream.Write(bytes, 0, bytes.Length);
            _stream.Close();
            _tcpClient.Close();
        }

        #endregion

        #region Public Methods

        #region Description
        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that contains the request line and headers
        /// included in the handshake request.
        /// </returns>
        #endregion Description
        public override string ToString() => _request.ToString();

        #endregion
    }
}
