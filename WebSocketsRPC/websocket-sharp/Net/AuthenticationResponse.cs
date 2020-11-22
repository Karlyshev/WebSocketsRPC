#region License
/*
 * AuthenticationResponse.cs
 *
 * ParseBasicCredentials is derived from System.Net.HttpListenerContext.cs of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2013-2014 sta.blockhead
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
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace WebSocketsRPC.Net
{
    internal class AuthenticationResponse : AuthenticationBase
    {
        #region Private Fields

        private uint _nonceCount;

        #endregion

        #region Private Constructors

        private AuthenticationResponse(AuthenticationSchemes scheme, NameValueCollection parameters) : base(scheme, parameters)
        {
        }

        #endregion

        #region Internal Constructors

        internal AuthenticationResponse(NetworkCredential credentials) : this(AuthenticationSchemes.Basic, new NameValueCollection(), credentials, 0)
        {
        }

        internal AuthenticationResponse(AuthenticationChallenge challenge, NetworkCredential credentials, uint nonceCount) : this(challenge.Scheme, challenge.Parameters, credentials, nonceCount)
        {
        }

        internal AuthenticationResponse(AuthenticationSchemes scheme, NameValueCollection parameters, NetworkCredential credentials, uint nonceCount) : base(scheme, parameters)
        {
            Parameters["username"] = credentials.Username;
            Parameters["password"] = credentials.Password;
            Parameters["uri"] = credentials.Domain;
            _nonceCount = nonceCount;
            if (scheme == AuthenticationSchemes.Digest)
                initAsDigest();
        }

        #endregion

        #region Internal Properties

        internal uint NonceCount => _nonceCount < uint.MaxValue  ? _nonceCount : 0;

        #endregion

        #region Public Properties

        public string Cnonce => Parameters["cnonce"];

        public string Nc => Parameters["nc"];

        public string Password => Parameters["password"];

        public string Response => Parameters["response"];

        public string Uri => Parameters["uri"];

        public string UserName => Parameters["username"];

        #endregion

        #region Private Methods

        private static string createA1(string username, string password, string realm) => $"{username}:{realm}:{password}";

        private static string createA1(string username, string password, string realm, string nonce, string cnonce) => $"{hash(createA1(username, password, realm))}:{nonce}:{cnonce}";

        private static string createA2(string method, string uri) => $"{method}:{uri}";

        private static string createA2(string method, string uri, string entity) => $"{method}:{uri}:{hash(entity)}";

        private static string hash(string value)
        {
            var hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(value));
            var res = new StringBuilder(64);
            foreach (var b in hashed)
                res.Append(b.ToString("x2"));
            return res.ToString();
        }

        private void initAsDigest()
        {
            var qops = Parameters["qop"];
            if (qops != null)
            {
                if (qops.Split(',').Contains(qop => qop.Trim().ToLower() == "auth"))
                {
                    Parameters["qop"] = "auth";
                    Parameters["cnonce"] = CreateNonceValue();
                    Parameters["nc"] = $"{++_nonceCount:x8}";
                }
                else
                {
                    Parameters["qop"] = null;
                }
            }

            Parameters["method"] = "GET";
            Parameters["response"] = CreateRequestDigest(Parameters);
        }

        #endregion

        #region Internal Methods

        internal static string CreateRequestDigest(NameValueCollection parameters)
        {
            var user = parameters["username"];
            var pass = parameters["password"];
            var realm = parameters["realm"];
            var nonce = parameters["nonce"];
            var uri = parameters["uri"];
            var algo = parameters["algorithm"];
            var qop = parameters["qop"];
            var cnonce = parameters["cnonce"];
            var nc = parameters["nc"];
            var method = parameters["method"];

            var a1 = algo != null && algo.ToLower() == "md5-sess" ? createA1(user, pass, realm, nonce, cnonce) : createA1(user, pass, realm);
            var a2 = qop != null && qop.ToLower() == "auth-int" ? createA2(method, uri, parameters["entity"]) : createA2(method, uri);
            var secret = hash(a1);
            var data = qop != null ? $"{nonce}:{nc}:{cnonce}:{qop}:{hash(a2)}" : $"{nonce}:{hash(a2)}";
            return hash($"{secret}:{data}");
        }

        internal static AuthenticationResponse Parse(string value)
        {
            try
            {
                var cred = value.Split(new[] { ' ' }, 2);
                if (cred.Length != 2)
                    return null;

                var schm = cred[0].ToLower();
                return schm == "basic" ? new AuthenticationResponse(AuthenticationSchemes.Basic, ParseBasicCredentials(cred[1])) : 
                       schm == "digest" ? new AuthenticationResponse(AuthenticationSchemes.Digest, ParseParameters(cred[1]))
                       : null;
            }
            catch
            {
            }

            return null;
        }

        internal static NameValueCollection ParseBasicCredentials(string value)
        {
            // Decode the basic-credentials (a Base64 encoded string).
            var userPass = Encoding.Default.GetString(Convert.FromBase64String(value));

            // The format is [<domain>\]<username>:<password>.
            var i = userPass.IndexOf(':');
            var user = userPass.Substring(0, i);
            var pass = i < userPass.Length - 1 ? userPass.Substring(i + 1) : String.Empty;

            // Check if 'domain' exists.
            i = user.IndexOf('\\');
            if (i > -1)
                user = user.Substring(i + 1);

            var res = new NameValueCollection();
            res["username"] = user;
            res["password"] = pass;

            return res;
        }

        internal override string ToBasicString() => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Parameters["username"]}:{Parameters["password"]}"));

        internal override string ToDigestString()
        {
            var output = new StringBuilder(256);
            output.Append($"Digest username=\"{Parameters["username"]}\", realm=\"{Parameters["realm"]}\", nonce=\"{Parameters["nonce"]}\", uri=\"{Parameters["uri"]}\", response=\"{Parameters["response"]}\"");

            var opaque = Parameters["opaque"];
            if (opaque != null)
                output.Append($", opaque=\"{opaque}\"");

            var algo = Parameters["algorithm"];
            if (algo != null)
                output.Append($", algorithm={algo}");

            var qop = Parameters["qop"];
            if (qop != null)
                output.Append($", qop={qop}, cnonce=\"{Parameters["cnonce"]}\", nc={Parameters["nc"]}");

            return output.ToString();
        }

        #endregion

        #region Public Methods

        public IIdentity ToIdentity()
        {
            var schm = Scheme;
            return schm == AuthenticationSchemes.Basic ? new HttpBasicIdentity(Parameters["username"], Parameters["password"]) as IIdentity : 
                   schm == AuthenticationSchemes.Digest ? new HttpDigestIdentity(Parameters) : 
                   null;
        }

        #endregion
    }
}