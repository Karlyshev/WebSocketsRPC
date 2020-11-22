#region License
/*
 * HttpListenerResponse.cs
 *
 * This code is derived from HttpListenerResponse.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2020 sta.blockhead
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

#region Contributors
/*
 * Contributors:
 * - Nicholas Devenish
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WebSocketsRPC.Net
{
    #region Description
    /// <summary>
    /// Represents an HTTP response to an HTTP request received by
    /// a <see cref="HttpListener"/> instance.
    /// </summary>
    /// <remarks>
    /// This class cannot be inherited.
    /// </remarks>
    #endregion Description
    public sealed class HttpListenerResponse : IDisposable
    {
        #region Private Fields

        private bool _closeConnection;
        private Encoding _contentEncoding;
        private long _contentLength;
        private string _contentType;
        private HttpListenerContext _context;
        private CookieCollection _cookies;
        private bool _disposed;
        private WebHeaderCollection _headers;
        private bool _headersSent;
        private bool _keepAlive;
        private ResponseStream _outputStream;
        private Uri _redirectLocation;
        private bool _sendChunked;
        private int _statusCode;
        private string _statusDescription;
        private Version _version;

        #endregion

        #region Internal Constructors

        internal HttpListenerResponse(HttpListenerContext context)
        {
            _context = context;
            _keepAlive = true;
            _statusCode = 200;
            _statusDescription = "OK";
            _version = HttpVersion.Version11;
        }

        #endregion

        #region Internal Properties

        internal bool CloseConnection { get => _closeConnection; set => _closeConnection = value; }

        internal WebHeaderCollection FullHeaders
        {
            get
            {
                var headers = new WebHeaderCollection(HttpHeaderType.Response, true);

                if (_headers != null)
                    headers.Add(_headers);

                if (_contentType != null)
                    headers.InternalSet("Content-Type", createContentTypeHeaderText(_contentType, _contentEncoding), true);

                if (headers["Server"] == null)
                    headers.InternalSet("Server", "websocket-sharp/1.0", true);

                if (headers["Date"] == null)
                    headers.InternalSet("Date", DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture), true);

                if (_sendChunked)
                    headers.InternalSet("Transfer-Encoding", "chunked", true);
                else
                    headers.InternalSet("Content-Length", _contentLength.ToString(CultureInfo.InvariantCulture), true);

                /*
                 * Apache forces closing the connection for these status codes:
                 * - 400 Bad Request
                 * - 408 Request Timeout
                 * - 411 Length Required
                 * - 413 Request Entity Too Large
                 * - 414 Request-Uri Too Long
                 * - 500 Internal Server Error
                 * - 503 Service Unavailable
                 */
                var closeConn = !_context.Request.KeepAlive
                                || !_keepAlive
                                || _statusCode == 400
                                || _statusCode == 408
                                || _statusCode == 411
                                || _statusCode == 413
                                || _statusCode == 414
                                || _statusCode == 500
                                || _statusCode == 503;

                var reuses = _context.Connection.Reuses;

                if (closeConn || reuses >= 100)
                    headers.InternalSet("Connection", "close", true);
                else
                {
                    headers.InternalSet("Keep-Alive", $"timeout=15,max={(100 - reuses)}", true);
                    if (_context.Request.ProtocolVersion < HttpVersion.Version11)
                        headers.InternalSet("Connection", "keep-alive", true);
                }

                if (_redirectLocation != null)
                    headers.InternalSet("Location", _redirectLocation.AbsoluteUri, true);

                if (_cookies != null)
                {
                    foreach (var cookie in _cookies)
                    {
                        headers.InternalSet("Set-Cookie", cookie.ToResponseString(), true);
                    }
                }

                return headers;
            }
        }

        internal bool HeadersSent { get => _headersSent; set => _headersSent = value; }

        internal string StatusLine => $"HTTP/{_version} {_statusCode} {_statusDescription}\r\n";

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets or sets the encoding for the entity body data included in
        /// the response.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Encoding"/> that represents the encoding for
        ///   the entity body data.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if no encoding is specified.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public Encoding ContentEncoding
        {
            get => _contentEncoding;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                _contentEncoding = value;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the number of bytes in the entity body data included in
        /// the response.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="long"/> that represents the number of bytes in
        ///   the entity body data.
        ///   </para>
        ///   <para>
        ///   It is used for the value of the Content-Length header.
        ///   </para>
        ///   <para>
        ///   The default value is zero.
        ///   </para>
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value specified for a set operation is less than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public long ContentLength64
        {
            get => _contentLength;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                if (value < 0)
                    throw new ArgumentOutOfRangeException("Less than zero.", "value");

                _contentLength = value;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the media type of the entity body included in
        /// the response.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the media type of
        ///   the entity body.
        ///   </para>
        ///   <para>
        ///   It is used for the value of the Content-Type header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if no media type is specified.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The value specified for a set operation is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The value specified for a set operation contains
        ///   an invalid character.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public string ContentType
        {
            get => _contentType;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                if (value == null)
                {
                    _contentType = null;
                    return;
                }

                if (value.Length == 0)
                    throw new ArgumentException("An empty string.", "value");

                if (!isValidForContentType(value))
                    throw new ArgumentException("It contains an invalid character.", "value");

                _contentType = value;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the collection of cookies sent with the response.
        /// </summary>
        /// <value>
        /// A <see cref="CookieCollection"/> that contains the cookies sent with
        /// the response.
        /// </value>
        #endregion Description
        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = new CookieCollection();
                return _cookies;
            }
            set => _cookies = value;
        }

        #region Description
        /// <summary>
        /// Gets or sets the collection of the HTTP headers sent to the client.
        /// </summary>
        /// <value>
        /// A <see cref="WebHeaderCollection"/> that contains the headers sent to
        /// the client.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The value specified for a set operation is not valid for a response.
        /// </exception>
        #endregion Description
        public WebHeaderCollection Headers
        {
            get
            {
                if (_headers == null)
                    _headers = new WebHeaderCollection(HttpHeaderType.Response, false);
                return _headers;
            }
            set
            {
                if (value == null)
                {
                    _headers = null;
                    return;
                }
                if (value.State != HttpHeaderType.Response)
                    throw new InvalidOperationException("The value is not valid for a response.");
                _headers = value;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets a value indicating whether the server requests
        /// a persistent connection.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the server requests a persistent connection;
        ///   otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>true</c>.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public bool KeepAlive
        {
            get => _keepAlive;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                _keepAlive = value;
            }
        }

        #region Description
        /// <summary>
        /// Gets a stream instance to which the entity body data can be written.
        /// </summary>
        /// <value>
        /// A <see cref="Stream"/> instance to which the entity body data can be
        /// written.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public Stream OutputStream
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());
                if (_outputStream == null)
                    _outputStream = _context.Connection.GetResponseStream();
                return _outputStream;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the HTTP version used for the response.
        /// </summary>
        /// <value>
        /// A <see cref="Version"/> that represents the HTTP version used for
        /// the response.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// The value specified for a set operation is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The value specified for a set operation does not have its Major
        ///   property set to 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The value specified for a set operation does not have its Minor
        ///   property set to either 0 or 1.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public Version ProtocolVersion
        {
            get => _version;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Major != 1)
                    throw new ArgumentException("Its Major property is not 1.", "value");

                if (value.Minor < 0 || value.Minor > 1)
                    throw new ArgumentException("Its Minor property is not 0 or 1.", "value");

                _version = value;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the URL to which the client is redirected to locate
        /// a requested resource.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the absolute URL for
        ///   the redirect location.
        ///   </para>
        ///   <para>
        ///   It is used for the value of the Location header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if no redirect location is specified.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The value specified for a set operation is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The value specified for a set operation is not an absolute URL.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public string RedirectLocation
        {
            get => _redirectLocation != null ? _redirectLocation.OriginalString : null;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                if (value == null)
                {
                    _redirectLocation = null;
                    return;
                }

                if (value.Length == 0)
                    throw new ArgumentException("An empty string.", "value");

                if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                    throw new ArgumentException("Not an absolute URL.", "value");

                _redirectLocation = uri;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets a value indicating whether the response uses the chunked
        /// transfer encoding.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the response uses the chunked transfer encoding;
        ///   otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public bool SendChunked
        {
            get => _sendChunked;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                _sendChunked = value;
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the HTTP status code returned to the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <see cref="int"/> that represents the HTTP status code for
        ///   the response to the request.
        ///   </para>
        ///   <para>
        ///   The default value is 200. It indicates that the request has
        ///   succeeded.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        /// <exception cref="System.Net.ProtocolViolationException">
        ///   <para>
        ///   The value specified for a set operation is invalid.
        ///   </para>
        ///   <para>
        ///   Valid values are between 100 and 999 inclusive.
        ///   </para>
        /// </exception>
        #endregion Description
        public int StatusCode
        {
            get => _statusCode;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                if (value < 100 || value > 999)
                    throw new System.Net.ProtocolViolationException("A value is not between 100 and 999 inclusive.");

                _statusCode = value;
                _statusDescription = value.GetStatusDescription();
            }
        }

        #region Description
        /// <summary>
        /// Gets or sets the description of the HTTP status code returned to
        /// the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the description of
        ///   the HTTP status code for the response to the request.
        ///   </para>
        ///   <para>
        ///   The default value is
        ///   the <see href="http://tools.ietf.org/html/rfc2616#section-10">
        ///   RFC 2616</see> description for the <see cref="StatusCode"/>
        ///   property value.
        ///   </para>
        ///   <para>
        ///   An empty string if an RFC 2616 description does not exist.
        ///   </para>
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// The value specified for a set operation is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The value specified for a set operation contains an invalid character.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public string StatusDescription
        {
            get => _statusDescription;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException("The response is already being sent.");

                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Length == 0)
                {
                    _statusDescription = _statusCode.GetStatusDescription();
                    return;
                }

                if (!isValidForStatusDescription(value))
                    throw new ArgumentException("It contains an invalid character.", "value");
                
                _statusDescription = value;
            }
        }

        #endregion

        #region Private Methods

        private bool canSetCookie(Cookie cookie)
        {
            var found = findCookie(cookie).ToList();
            if (found.Count == 0)
                return true;
            var ver = cookie.Version;
            foreach (var c in found)
            {
                if (c.Version == ver)
                    return true;
            }
            return false;
        }

        private void close(bool force)
        {
            _disposed = true;
            _context.Connection.Close(force);
        }

        private void close(byte[] responseEntity, int bufferLength, bool willBlock)
        {
            var stream = OutputStream;
            if (willBlock)
            {
                stream.WriteBytes(responseEntity, bufferLength);
                close(false);
                return;
            }
            stream.WriteBytesAsync(responseEntity, bufferLength, () => close(false), null);
        }

        private static string createContentTypeHeaderText(string value, Encoding encoding)
        {
            if (value.IndexOf("charset=", StringComparison.Ordinal) > -1)
                return value;

            if (encoding == null)
                return value;

            return $"{value}; charset={encoding.WebName}";
        }

        private IEnumerable<Cookie> findCookie(Cookie cookie)
        {
            if (_cookies == null || _cookies.Count == 0)
                yield break;

            foreach (var c in _cookies)
            {
                if (c.EqualsWithoutValueAndVersion(cookie))
                    yield return c;
            }
        }

        private static bool isValidForContentType(string value)
        {
            foreach (var c in value)
            {
                if (c < 0x20)
                    return false;

                if (c > 0x7e)
                    return false;

                if ("()<>@:\\[]?{}".IndexOf(c) > -1)
                    return false;
            }

            return true;
        }

        private static bool isValidForStatusDescription(string value)
        {
            foreach (var c in value)
            {
                if (c < 0x20)
                    return false;

                if (c > 0x7e)
                    return false;
            }

            return true;
        }

        #endregion

        #region Public Methods

        #region Description
        /// <summary>
        /// Closes the connection to the client without sending a response.
        /// </summary>
        #endregion Description
        public void Abort()
        {
            if (_disposed)
                return;
            close(true);
        }

        #region Description
        /// <summary>
        /// Appends the specified cookie to the cookies sent with the response.
        /// </summary>
        /// <param name="cookie">
        /// A <see cref="Cookie"/> to append.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cookie"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public void AppendCookie(Cookie cookie) => Cookies.Add(cookie);

        #region Description
        /// <summary>
        /// Appends an HTTP header with the specified name and value to
        /// the headers for the response.
        /// </summary>
        /// <param name="name">
        /// A <see cref="string"/> that represents the name of the header to
        /// append.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the header to
        /// append.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="name"/> or <paramref name="value"/> contains
        ///   an invalid character.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> is a restricted header name.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535
        /// characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The header cannot be allowed to append to the current headers.
        /// </exception>
        #endregion Description
        public void AppendHeader(string name, string value) => Headers.Add(name, value);

        #region Description
        /// <summary>
        /// Sends the response to the client and releases the resources used by
        /// this instance.
        /// </summary>
        #endregion Description
        public void Close()
        {
            if (_disposed)
                return;
            close(false);
        }

        #region Description
        /// <summary>
        /// Sends the response with the specified entity body data to the client
        /// and releases the resources used by this instance.
        /// </summary>
        /// <param name="responseEntity">
        /// An array of <see cref="byte"/> that contains the entity body data.
        /// </param>
        /// <param name="willBlock">
        /// <c>true</c> if this method blocks execution while flushing the stream
        /// to the client; otherwise, <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="responseEntity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public void Close(byte[] responseEntity, bool willBlock)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());
            if (responseEntity == null)
                throw new ArgumentNullException("responseEntity");

            var len = responseEntity.LongLength;
            if (len > int.MaxValue)
            {
                close(responseEntity, 1024, willBlock);
                return;
            }

            var stream = OutputStream;
            if (willBlock)
            {
                stream.Write(responseEntity, 0, (int)len);
                close(false);
                return;
            }
            stream.BeginWrite(responseEntity, 0, (int)len, ar => {
                stream.EndWrite(ar);
                close(false);
            },  null);
        }

        #region Description
        /// <summary>
        /// Copies some properties from the specified response instance to
        /// this instance.
        /// </summary>
        /// <param name="templateResponse">
        /// A <see cref="HttpListenerResponse"/> to copy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="templateResponse"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            if (templateResponse == null)
                throw new ArgumentNullException("templateResponse");

            var headers = templateResponse._headers;
            if (headers != null)
            {
                if (_headers != null)
                    _headers.Clear();

                Headers.Add(headers);
            }
            else
            {
                _headers = null;
            }

            _contentLength = templateResponse._contentLength;
            _statusCode = templateResponse._statusCode;
            _statusDescription = templateResponse._statusDescription;
            _keepAlive = templateResponse._keepAlive;
            _version = templateResponse._version;
        }

        #region Description
        /// <summary>
        /// Configures the response to redirect the client's request to
        /// the specified URL.
        /// </summary>
        /// <remarks>
        /// This method sets the <see cref="RedirectLocation"/> property to
        /// <paramref name="url"/>, the <see cref="StatusCode"/> property to
        /// 302, and the <see cref="StatusDescription"/> property to "Found".
        /// </remarks>
        /// <param name="url">
        /// A <see cref="string"/> that represents the absolute URL to which
        /// the client is redirected to locate a requested resource.
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
        ///   <paramref name="url"/> is not an absolute URL.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The response is already being sent.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This instance is closed.
        /// </exception>
        #endregion Description
        public void Redirect(string url)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (_headersSent)
                throw new InvalidOperationException("The response is already being sent.");

            if (url == null)
                throw new ArgumentNullException("url");

            if (url.Length == 0)
                throw new ArgumentException("An empty string.", "url");

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                throw new ArgumentException("Not an absolute URL.", "url");

            _redirectLocation = uri;
            _statusCode = 302;
            _statusDescription = "Found";
        }

        #region Description
        /// <summary>
        /// Adds or updates a cookie in the cookies sent with the response.
        /// </summary>
        /// <param name="cookie">
        /// A <see cref="Cookie"/> to set.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cookie"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="cookie"/> already exists in the cookies but
        /// it cannot be updated.
        /// </exception>
        #endregion Description
        public void SetCookie(Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException("cookie");

            if (!canSetCookie(cookie))
            {
                var msg = "It cannot be updated.";
                throw new ArgumentException(msg, "cookie");
            }
            Cookies.Add(cookie);
        }

        #region Description
        /// <summary>
        /// Adds or updates an HTTP header with the specified name and value in
        /// the headers for the response.
        /// </summary>
        /// <param name="name">
        /// A <see cref="string"/> that represents the name of the header to set.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the header to set.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="name"/> or <paramref name="value"/> contains
        ///   an invalid character.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> is a restricted header name.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535
        /// characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The header cannot be allowed to set in the current headers.
        /// </exception>
        #endregion Description
        public void SetHeader(string name, string value) => Headers.Set(name, value);

        #endregion

        #region Explicit Interface Implementations

        #region Description
        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        #endregion Description
        void IDisposable.Dispose()
        {
            if (_disposed)
                return;
            close(true);
        }

        #endregion
    }
}