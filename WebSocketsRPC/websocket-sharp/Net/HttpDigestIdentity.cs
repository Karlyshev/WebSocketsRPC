#region License
/*
 * HttpDigestIdentity.cs
 *
 * The MIT License
 *
 * Copyright (c) 2014-2017 sta.blockhead
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
using System.Collections.Specialized;
using System.Security.Principal;

namespace WebSocketsRPC.Net
{
    #region Description
    /// <summary>
    /// Holds the username and other parameters from
    /// an HTTP Digest authentication attempt.
    /// </summary>
    #endregion Description
    public class HttpDigestIdentity : GenericIdentity
    {
        #region Private Fields

        private NameValueCollection _parameters;

        #endregion

        #region Internal Constructors

        internal HttpDigestIdentity(NameValueCollection parameters) : base(parameters["username"], "Digest") => _parameters = parameters;

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets the algorithm parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the algorithm parameter.
        /// </value>
        #endregion Description
        public string Algorithm => _parameters["algorithm"];

        #region Description
        /// <summary>
        /// Gets the cnonce parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the cnonce parameter.
        /// </value>
        #endregion Description
        public string Cnonce => _parameters["cnonce"];

        #region Description
        /// <summary>
        /// Gets the nc parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the nc parameter.
        /// </value>
        #endregion Description
        public string Nc => _parameters["nc"];

        #region Description
        /// <summary>
        /// Gets the nonce parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the nonce parameter.
        /// </value>
        #endregion Description
        public string Nonce => _parameters["nonce"];

        #region Description
        /// <summary>
        /// Gets the opaque parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the opaque parameter.
        /// </value>
        #endregion Description
        public string Opaque => _parameters["opaque"];

        #region Description
        /// <summary>
        /// Gets the qop parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the qop parameter.
        /// </value>
        #endregion Description
        public string Qop => _parameters["qop"];

        #region Description
        /// <summary>
        /// Gets the realm parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the realm parameter.
        /// </value>
        #endregion Description
        public string Realm => _parameters["realm"];

        #region Description
        /// <summary>
        /// Gets the response parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the response parameter.
        /// </value>
        #endregion Description
        public string Response => _parameters["response"];

        #region Description
        /// <summary>
        /// Gets the uri parameter from a digest authentication attempt.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the uri parameter.
        /// </value>
        #endregion Description
        public string Uri => _parameters["uri"];

        #endregion

        #region Internal Methods

        internal bool IsValid(string password, string realm, string method, string entity)
        {
            var copied = new NameValueCollection(_parameters);
            copied["password"] = password;
            copied["realm"] = realm;
            copied["method"] = method;
            copied["entity"] = entity;
            return _parameters["response"] == AuthenticationResponse.CreateRequestDigest(copied);
        }

        #endregion
    }
}