#region License
/*
 * HttpListenerRequest.cs
 *
 * This code is derived from HttpListenerRequest.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
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

#region Authors
/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */
#endregion

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Linq;

namespace WebSocketsRPC.Net
{
    #region Description
    /// <summary>
    /// Represents an incoming HTTP request to a <see cref="HttpListener"/>
    /// instance.
    /// </summary>
    /// <remarks>
    /// This class cannot be inherited.
    /// </remarks>
    #endregion Description
    public sealed class HttpListenerRequest
    {
        #region Private Fields

        private static readonly byte[] _100continue;
        private string[] _acceptTypes;
        private bool _chunked;
        private HttpConnection _connection;
        private Encoding _contentEncoding;
        private long _contentLength;
        private HttpListenerContext _context;
        private CookieCollection _cookies;
        private WebHeaderCollection _headers;
        private string _httpMethod;
        private Stream _inputStream;
        private Version _protocolVersion;
        private NameValueCollection _queryString;
        private string _rawUrl;
        private Guid _requestTraceIdentifier;
        private Uri _url;
        private Uri _urlReferrer;
        private bool _urlSet;
        private string _userHostName;
        private string[] _userLanguages;

        #endregion

        #region Static Constructor

        static HttpListenerRequest() => _100continue = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

        #endregion

        #region Internal Constructors

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;
            _connection = context.Connection;
            _contentLength = -1;
            _headers = new WebHeaderCollection();
            _requestTraceIdentifier = Guid.NewGuid();
        }

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets the media types that are acceptable for the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An array of <see cref="string"/> that contains the names of the media
        ///   types specified in the value of the Accept header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public string[] AcceptTypes
        {
            get
            {
                var val = _headers["Accept"];
                if (val == null)
                    return null;
                if (_acceptTypes == null)
                    _acceptTypes = val.SplitHeaderValue(',').Trim().ToArray();
                return _acceptTypes;
            }
        }

        #region Description
        /// <summary>
        /// Gets an error code that identifies a problem with the certificate
        /// provided by the client.
        /// </summary>
        /// <value>
        /// An <see cref="int"/> that represents an error code.
        /// </value>
        /// <exception cref="NotSupportedException">
        /// This property is not supported.
        /// </exception>
        #endregion Description
        public int ClientCertificateError => throw new NotSupportedException();

        #region Description
        /// <summary>
        /// Gets the encoding for the entity body data included in the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Encoding"/> converted from the charset value of the
        ///   Content-Type header.
        ///   </para>
        ///   <para>
        ///   <see cref="Encoding.UTF8"/> if the charset value is not available.
        ///   </para>
        /// </value>
        #endregion Description
        public Encoding ContentEncoding
        {
            get
            {
                if (_contentEncoding == null)
                    _contentEncoding = getContentEncoding() ?? Encoding.UTF8;
                return _contentEncoding;
            }
        }

        #region Description
        /// <summary>
        /// Gets the length in bytes of the entity body data included in the
        /// request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="long"/> converted from the value of the Content-Length
        ///   header.
        ///   </para>
        ///   <para>
        ///   -1 if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public long ContentLength64 => _contentLength;

        #region Description
        /// <summary>
        /// Gets the media type of the entity body data included in the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of the Content-Type
        ///   header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public string ContentType => _headers["Content-Type"];

        #region Description
        /// <summary>
        /// Gets the cookies included in the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="CookieCollection"/> that contains the cookies.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        #endregion Description
        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = _headers.GetCookies(false);
                return _cookies;
            }
        }

        #region Description
        /// <summary>
        /// Gets a value indicating whether the request has the entity body data.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request has the entity body data; otherwise,
        /// <c>false</c>.
        /// </value>
        #endregion Description
        public bool HasEntityBody => _contentLength > 0 || _chunked;

        #region Description
        /// <summary>
        /// Gets the headers included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        #endregion Description
        public NameValueCollection Headers => _headers;

        #region Description
        /// <summary>
        /// Gets the HTTP method specified by the client.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the HTTP method specified in
        /// the request line.
        /// </value>
        #endregion Description
        public string HttpMethod => _httpMethod;

        #region Description
        /// <summary>
        /// Gets a stream that contains the entity body data included in
        /// the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Stream"/> that contains the entity body data.
        ///   </para>
        ///   <para>
        ///   <see cref="Stream.Null"/> if the entity body data is not available.
        ///   </para>
        /// </value>
        #endregion Description
        public Stream InputStream
        {
            get
            {
                if (_inputStream == null)
                    _inputStream = getInputStream() ?? Stream.Null;
                return _inputStream;
            }
        }

        #region Description
        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public bool IsAuthenticated => _context.User != null;

        #region Description
        /// <summary>
        /// Gets a value indicating whether the request is sent from the local
        /// computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is sent from the same computer as the server;
        /// otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public bool IsLocal => _connection.IsLocal;

        #region Description
        /// <summary>
        /// Gets a value indicating whether a secure connection is used to send
        /// the request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public bool IsSecureConnection => _connection.IsSecure;

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
        public bool IsWebSocketRequest => _httpMethod == "GET" && _protocolVersion > HttpVersion.Version10 && _headers.Upgrades("websocket");

        #region Description
        /// <summary>
        /// Gets a value indicating whether a persistent connection is requested.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request specifies that the connection is kept open;
        /// otherwise, <c>false</c>.
        /// </value>
        #endregion Description
        public bool KeepAlive => _headers.KeepsAlive(_protocolVersion);

        #region Description
        /// <summary>
        /// Gets the endpoint to which the request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server IP
        /// address and port number.
        /// </value>
        #endregion Description
        public System.Net.IPEndPoint LocalEndPoint => _connection.LocalEndPoint;

        #region Description
        /// <summary>
        /// Gets the HTTP version specified by the client.
        /// </summary>
        /// <value>
        /// A <see cref="Version"/> that represents the HTTP version specified in
        /// the request line.
        /// </value>
        #endregion Description
        public Version ProtocolVersion => _protocolVersion;

        #region Description
        /// <summary>
        /// Gets the query string included in the request.
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
        public NameValueCollection QueryString
        {
            get
            {
                if (_queryString == null)
                {
                    var url = Url;
                    _queryString = QueryStringCollection.Parse(url != null ? url.Query : null, Encoding.UTF8);
                }
                return _queryString;
            }
        }

        #region Description
        /// <summary>
        /// Gets the raw URL specified by the client.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the request target specified in
        /// the request line.
        /// </value>
        #endregion Description
        public string RawUrl => _rawUrl;

        #region Description
        /// <summary>
        /// Gets the endpoint from which the request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client IP
        /// address and port number.
        /// </value>
        #endregion Description
        public System.Net.IPEndPoint RemoteEndPoint => _connection.RemoteEndPoint;

        #region Description
        /// <summary>
        /// Gets the trace identifier of the request.
        /// </summary>
        /// <value>
        /// A <see cref="Guid"/> that represents the trace identifier.
        /// </value>
        #endregion Description
        public Guid RequestTraceIdentifier => _requestTraceIdentifier;

        #region Description
        /// <summary>
        /// Gets the URL requested by the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Uri"/> that represents the URL parsed from the request.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the URL cannot be parsed.
        ///   </para>
        /// </value>
        #endregion Description
        public Uri Url
        {
            get
            {
                if (!_urlSet)
                {
                    _url = HttpUtility.CreateRequestUrl(_rawUrl, _userHostName ?? UserHostAddress, IsWebSocketRequest, IsSecureConnection);
                    _urlSet = true;
                }
                return _url;
            }
        }

        #region Description
        /// <summary>
        /// Gets the URI of the resource from which the requested URL was obtained.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Uri"/> converted from the value of the Referer header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header value is not available.
        ///   </para>
        /// </value>
        #endregion Description
        public Uri UrlReferrer
        {
            get
            {
                var val = _headers["Referer"];
                if (val == null)
                    return null;

                if (_urlReferrer == null)
                    _urlReferrer = val.ToUri();

                return _urlReferrer;
            }
        }

        #region Description
        /// <summary>
        /// Gets the user agent from which the request is originated.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of the User-Agent
        ///   header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public string UserAgent => _headers["User-Agent"];

        #region Description
        /// <summary>
        /// Gets the IP address and port number to which the request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the server IP address and port
        /// number.
        /// </value>
        #endregion Description
        public string UserHostAddress => _connection.LocalEndPoint.ToString();

        #region Description
        /// <summary>
        /// Gets the server host name requested by the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of the Host header.
        ///   </para>
        ///   <para>
        ///   It includes the port number if provided.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public string UserHostName => _userHostName;

        #region Description
        /// <summary>
        /// Gets the natural languages that are acceptable for the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An array of <see cref="string"/> that contains the names of the
        ///   natural languages specified in the value of the Accept-Language
        ///   header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the header is not present.
        ///   </para>
        /// </value>
        #endregion Description
        public string[] UserLanguages
        {
            get
            {
                var val = _headers["Accept-Language"];
                if (val == null)
                    return null;
                if (_userLanguages == null)
                    _userLanguages = val.Split(',').Trim().ToArray();
                return _userLanguages;
            }
        }

        #endregion

        #region Private Methods

        private void finishInitialization10()
        {
            var transferEnc = _headers["Transfer-Encoding"];
            if (transferEnc != null)
            {
                _context.ErrorMessage = "Invalid Transfer-Encoding header";
                return;
            }

            if (_httpMethod == "POST")
            {
                if (_contentLength == -1)
                {
                    _context.ErrorMessage = "Content-Length header required";
                    return;
                }

                if (_contentLength == 0)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                    return;
                }
            }
        }

        private Encoding getContentEncoding()
        {
            var val = _headers["Content-Type"];
            if (val == null)
                return null;
            HttpUtility.TryGetEncoding(val, out Encoding ret);
            return ret;
        }

        private RequestStream getInputStream() => _contentLength > 0 || _chunked ? _connection.GetRequestStream(_contentLength, _chunked) : null;

        #endregion

        #region Internal Methods

        internal void AddHeader(string headerField)
        {
            var start = headerField[0];
            if (start == ' ' || start == '\t')
            {
                _context.ErrorMessage = "Invalid header field";
                return;
            }

            var colon = headerField.IndexOf(':');
            if (colon < 1)
            {
                _context.ErrorMessage = "Invalid header field";
                return;
            }

            var name = headerField.Substring(0, colon).Trim();
            if (name.Length == 0 || !name.IsToken())
            {
                _context.ErrorMessage = "Invalid header name";
                return;
            }

            var val = colon < headerField.Length - 1 ? headerField.Substring(colon + 1).Trim() : string.Empty;

            _headers.InternalSet(name, val, false);

            var lower = name.ToLower(CultureInfo.InvariantCulture);
            if (lower == "host")
            {
                if (_userHostName != null)
                {
                    _context.ErrorMessage = "Invalid Host header";
                    return;
                }

                if (val.Length == 0)
                {
                    _context.ErrorMessage = "Invalid Host header";
                    return;
                }

                _userHostName = val;
                return;
            }

            if (lower == "content-length")
            {
                if (_contentLength > -1)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                    return;
                }

                if (!long.TryParse(val, out long len))
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                    return;
                }

                if (len < 0)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                    return;
                }

                _contentLength = len;
                return;
            }
        }

        internal void FinishInitialization()
        {
            if (_protocolVersion == HttpVersion.Version10)
            {
                finishInitialization10();
                return;
            }

            if (_userHostName == null)
            {
                _context.ErrorMessage = "Host header required";
                return;
            }

            var transferEnc = _headers["Transfer-Encoding"];
            if (transferEnc != null)
            {
                var comparison = StringComparison.OrdinalIgnoreCase;
                if (!transferEnc.Equals("chunked", comparison))
                {
                    _context.ErrorMessage = string.Empty;
                    _context.ErrorStatus = 501;

                    return;
                }

                _chunked = true;
            }

            if (_httpMethod == "POST" || _httpMethod == "PUT")
            {
                if (_contentLength <= 0 && !_chunked)
                {
                    _context.ErrorMessage = string.Empty;
                    _context.ErrorStatus = 411;
                    return;
                }
            }

            var expect = _headers["Expect"];
            if (expect != null)
            {
                var comparison = StringComparison.OrdinalIgnoreCase;
                if (!expect.Equals("100-continue", comparison))
                {
                    _context.ErrorMessage = "Invalid Expect header";
                    return;
                }
                _connection.GetResponseStream().InternalWrite(_100continue, 0, _100continue.Length);
            }
        }

        internal bool FlushInput()
        {
            var input = InputStream;
            if (input == Stream.Null)
                return true;

            var len = 2048;
            if (_contentLength > 0 && _contentLength < len)
                len = (int)_contentLength;

            var buff = new byte[len];

            while (true)
            {
                try
                {
                    var ares = input.BeginRead(buff, 0, len, null, null);
                    if (!ares.IsCompleted)
                    {
                        var timeout = 100;
                        if (!ares.AsyncWaitHandle.WaitOne(timeout))
                            return false;
                    }

                    if (input.EndRead(ares) <= 0)
                        return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        internal bool IsUpgradeRequest(string protocol) => _headers.Upgrades(protocol);

        internal void SetRequestLine(string requestLine)
        {
            var parts = requestLine.Split(new[] { ' ' }, 3);
            if (parts.Length < 3)
            {
                _context.ErrorMessage = "Invalid request line (parts)";
                return;
            }

            var method = parts[0];
            if (method.Length == 0)
            {
                _context.ErrorMessage = "Invalid request line (method)";
                return;
            }

            var target = parts[1];
            if (target.Length == 0)
            {
                _context.ErrorMessage = "Invalid request line (target)";
                return;
            }

            var rawVer = parts[2];
            if (rawVer.Length != 8)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }

            if (rawVer.IndexOf("HTTP/") != 0)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }

            Version ver;
            if (!rawVer.Substring(5).TryCreateVersion(out ver))
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }

            if (ver.Major < 1)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }

            if (!method.IsHttpMethod(ver))
            {
                _context.ErrorMessage = "Invalid request line (method)";
                return;
            }

            _httpMethod = method;
            _rawUrl = target;
            _protocolVersion = ver;
        }

        #endregion

        #region Public Methods

        #region Description
        /// <summary>
        /// Begins getting the certificate provided by the client asynchronously.
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncResult"/> instance that indicates the status of the
        /// operation.
        /// </returns>
        /// <param name="requestCallback">
        /// An <see cref="AsyncCallback"/> delegate that invokes the method called
        /// when the operation is complete.
        /// </param>
        /// <param name="state">
        /// An <see cref="object"/> that represents a user defined object to pass to
        /// the callback delegate.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// This method is not supported.
        /// </exception>
        #endregion Description
        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state) => throw new NotSupportedException();

        #region Description
        /// <summary>
        /// Ends an asynchronous operation to get the certificate provided by the
        /// client.
        /// </summary>
        /// <returns>
        /// A <see cref="X509Certificate2"/> that represents an X.509 certificate
        /// provided by the client.
        /// </returns>
        /// <param name="asyncResult">
        /// An <see cref="IAsyncResult"/> instance returned when the operation
        /// started.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// This method is not supported.
        /// </exception>
        #endregion Description
        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult) => throw new NotSupportedException();

        #region Description
        /// <summary>
        /// Gets the certificate provided by the client.
        /// </summary>
        /// <returns>
        /// A <see cref="X509Certificate2"/> that represents an X.509 certificate
        /// provided by the client.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// This method is not supported.
        /// </exception>
        #endregion Description
        public X509Certificate2 GetClientCertificate() => throw new NotSupportedException();

        #region Description
        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that contains the request line and headers
        /// included in the request.
        /// </returns>
        #endregion Description
        public override string ToString()
        {
            var buff = new StringBuilder(64);
            buff.Append($"{_httpMethod} {_rawUrl} HTTP/{_protocolVersion}\r\n").Append(_headers.ToString());
            return buff.ToString();
        }

        #endregion
    }
}