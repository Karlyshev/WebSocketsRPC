#region License
/*
 * HttpVersion.cs
 *
 * This code is derived from System.Net.HttpVersion.cs of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2012-2014 sta.blockhead
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
 * - Lawrence Pit <loz@cable.a2000.nl>
 */
#endregion

using System;

namespace WebSocketsRPC.Net
{
    #region Description
    /// <summary>
    /// Provides the HTTP version numbers.
    /// </summary>
    #endregion Description
    public class HttpVersion
    {
        #region Public Fields

        #region Description
        /// <summary>
        /// Provides a <see cref="Version"/> instance for the HTTP/1.0.
        /// </summary>
        #endregion Description
        public static readonly Version Version10 = new Version(1, 0);

        #region Description
        /// <summary>
        /// Provides a <see cref="Version"/> instance for the HTTP/1.1.
        /// </summary>
        #endregion Description
        public static readonly Version Version11 = new Version(1, 1);

        #endregion

        #region Public Constructors

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpVersion"/> class.
        /// </summary>
        #endregion Description
        public HttpVersion()
        {
        }

        #endregion
    }
}