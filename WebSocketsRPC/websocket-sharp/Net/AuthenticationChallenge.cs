#region License
/*
 * AuthenticationChallenge.cs
 *
 * The MIT License
 *
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
using System.Text;

namespace WebSocketsRPC.Net
{
    internal class AuthenticationChallenge : AuthenticationBase
    {
        #region Private Constructors

        private AuthenticationChallenge(AuthenticationSchemes scheme, NameValueCollection parameters) : base(scheme, parameters)
        {
        }

        #endregion

        #region Internal Constructors

        internal AuthenticationChallenge(AuthenticationSchemes scheme, string realm) : base(scheme, new NameValueCollection())
        {
            Parameters["realm"] = realm;
            if (scheme == AuthenticationSchemes.Digest)
            {
                Parameters["nonce"] = CreateNonceValue();
                Parameters["algorithm"] = "MD5";
                Parameters["qop"] = "auth";
            }
        }

        #endregion

        #region Public Properties

        public string Domain => Parameters["domain"];

        public string Stale => Parameters["stale"];

        #endregion

        #region Internal Methods

        internal static AuthenticationChallenge CreateBasicChallenge(string realm) => new AuthenticationChallenge(AuthenticationSchemes.Basic, realm);

        internal static AuthenticationChallenge CreateDigestChallenge(string realm) => new AuthenticationChallenge(AuthenticationSchemes.Digest, realm);

        internal static AuthenticationChallenge Parse(string value)
        {
            var chal = value.Split(new[] { ' ' }, 2);
            if (chal.Length != 2)
                return null;
            var schm = chal[0].ToLower();
            return schm == "basic" ?  new AuthenticationChallenge(AuthenticationSchemes.Basic, ParseParameters(chal[1])) : 
                   schm == "digest" ? new AuthenticationChallenge(AuthenticationSchemes.Digest, ParseParameters(chal[1])) 
                   : null;
        }

        internal override string ToBasicString() => $"Basic realm=\"{Parameters["realm"]}\"";

        internal override string ToDigestString()
        {
            var output = new StringBuilder(128);

            var domain = Parameters["domain"];
            if (domain != null)
                output.Append($"Digest realm=\"{Parameters["realm"]}\", domain=\"{domain}\", nonce=\"{Parameters["nonce"]}\"");
            else
                output.Append($"Digest realm=\"{Parameters["realm"]}\", nonce=\"{Parameters["nonce"]}\"");

            var opaque = Parameters["opaque"];
            if (opaque != null)
                output.Append($", opaque=\"{opaque}\"");

            var stale = Parameters["stale"];
            if (stale != null)
                output.Append($", stale={stale}");

            var algo = Parameters["algorithm"];
            if (algo != null)
                output.Append($", algorithm={algo}");

            var qop = Parameters["qop"];
            if (qop != null)
                output.Append($", qop=\"{qop}\"");

            return output.ToString();
        }

        #endregion
    }
}
