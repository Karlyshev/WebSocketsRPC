#region License
/*
 * CookieCollection.cs
 *
 * This code is derived from CookieCollection.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2004,2009 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2019 sta.blockhead
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
 * - Gonzalo Paniagua Javier <gonzalo@ximian.com>
 * - Sebastien Pouliot <sebastien@ximian.com>
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WebSocketsRPC.Net
{
    #region Description
    /// <summary>
    /// Provides a collection of instances of the <see cref="Cookie"/> class.
    /// </summary>
    #endregion Description
    [Serializable]
    public class CookieCollection : ICollection<Cookie>
    {
        #region Private Fields

        private List<Cookie> _list;
        private bool _readOnly;
        private object _sync;

        #endregion

        #region Public Constructors

        #region Description
        /// <summary>
        /// Initializes a new instance of the <see cref="CookieCollection"/> class.
        /// </summary>
        #endregion Description
        public CookieCollection()
        {
            _list = new List<Cookie>();
            _sync = ((ICollection)_list).SyncRoot;
        }

        #endregion

        #region Internal Properties

        internal IList<Cookie> List => _list;

        internal IEnumerable<Cookie> Sorted
        {
            get
            {
                var list = new List<Cookie>(_list);
                if (list.Count > 1)
                    list.Sort(compareForSorted);
                return list;
            }
        }

        #endregion

        #region Public Properties

        #region Description
        /// <summary>
        /// Gets the number of cookies in the collection.
        /// </summary>
        /// <value>
        /// An <see cref="int"/> that represents the number of cookies in
        /// the collection.
        /// </value>
        #endregion Description
        public int Count => _list.Count;

        #region Description
        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the collection is read-only; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        #endregion Description
        public bool IsReadOnly { get => _readOnly; internal set => _readOnly = value; }

        #region Description
        /// <summary>
        /// Gets a value indicating whether the access to the collection is
        /// thread safe.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the access to the collection is thread safe;
        ///   otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        #endregion Description
        public bool IsSynchronized => false;

        #region Description
        /// <summary>
        /// Gets the cookie at the specified index from the collection.
        /// </summary>
        /// <value>
        /// A <see cref="Cookie"/> at the specified index in the collection.
        /// </value>
        /// <param name="index">
        /// An <see cref="int"/> that specifies the zero-based index of the cookie
        /// to find.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is out of allowable range for the collection.
        /// </exception>
        #endregion Description
        public Cookie this[int index]
        {
            get
            {
                if (index < 0 || index >= _list.Count)
                    throw new ArgumentOutOfRangeException("index");
                return _list[index];
            }
        }

        #region Description
        /// <summary>
        /// Gets the cookie with the specified name from the collection.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Cookie"/> with the specified name in the collection.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not found.
        ///   </para>
        /// </value>
        /// <param name="name">
        /// A <see cref="string"/> that specifies the name of the cookie to find.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public Cookie this[string name]
        {
            get
            {
                if (name == null)
                    throw new ArgumentNullException("name");
                var caseInsensitive = StringComparison.InvariantCultureIgnoreCase;
                foreach (var cookie in Sorted)
                {
                    if (cookie.Name.Equals(name, caseInsensitive))
                        return cookie;
                }

                return null;
            }
        }

        #region Description
        /// <summary>
        /// Gets an object used to synchronize access to the collection.
        /// </summary>
        /// <value>
        /// An <see cref="object"/> used to synchronize access to the collection.
        /// </value>
        #endregion Description
        public object SyncRoot => _sync;

        #endregion

        #region Private Methods

        private void add(Cookie cookie)
        {
            var idx = search(cookie);
            if (idx == -1)
            {
                _list.Add(cookie);
                return;
            }

            _list[idx] = cookie;
        }

        private static int compareForSort(Cookie x, Cookie y)
        {
            return (x.Name.Length + x.Value.Length) - (y.Name.Length + y.Value.Length);
        }

        private static int compareForSorted(Cookie x, Cookie y)
        {
            var ret = x.Version - y.Version;
            return ret != 0 ? ret : 
                   (ret = x.Name.CompareTo(y.Name)) != 0 ? ret : 
                   y.Path.Length - x.Path.Length;
        }

        private static CookieCollection parseRequest(string value)
        {
            var ret = new CookieCollection();
            Cookie cookie = null;
            var ver = 0;
            var caseInsensitive = StringComparison.InvariantCultureIgnoreCase;
            var pairs = value.SplitHeaderValue(',', ';').ToList();

            for (var i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i].Trim();
                if (pair.Length == 0)
                    continue;

                var idx = pair.IndexOf('=');
                if (idx == -1)
                {
                    if (cookie == null)
                        continue;

                    if (pair.Equals("$port", caseInsensitive))
                    {
                        cookie.Port = "\"\"";
                        continue;
                    }
                    continue;
                }

                if (idx == 0)
                {
                    if (cookie != null)
                    {
                        ret.add(cookie);
                        cookie = null;
                    }

                    continue;
                }

                var name = pair.Substring(0, idx).TrimEnd(' ');
                var val = idx < pair.Length - 1 ? pair.Substring(idx + 1).TrimStart(' ') : string.Empty;

                if (name.Equals("$version", caseInsensitive))
                {
                    if (val.Length == 0)
                        continue;

                    if (!int.TryParse(val.Unquote(), out int num))
                        continue;

                    ver = num;
                    continue;
                }

                if (name.Equals("$path", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.Path = val;
                    continue;
                }

                if (name.Equals("$domain", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.Domain = val;
                    continue;
                }

                if (name.Equals("$port", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.Port = val;
                    continue;
                }

                if (cookie != null)
                    ret.add(cookie);

                if (!Cookie.TryCreate(name, val, out cookie))
                    continue;

                if (ver != 0)
                    cookie.Version = ver;
            }

            if (cookie != null)
                ret.add(cookie);

            return ret;
        }

        private static CookieCollection parseResponse(string value)
        {
            var ret = new CookieCollection();
            Cookie cookie = null;
            var caseInsensitive = StringComparison.InvariantCultureIgnoreCase;
            var pairs = value.SplitHeaderValue(',', ';').ToList();

            for (var i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i].Trim();
                if (pair.Length == 0)
                    continue;

                var idx = pair.IndexOf('=');
                if (idx == -1)
                {
                    if (cookie == null)
                        continue;

                    if (pair.Equals("port", caseInsensitive))
                    {
                        cookie.Port = "\"\"";
                        continue;
                    }

                    if (pair.Equals("discard", caseInsensitive))
                    {
                        cookie.Discard = true;
                        continue;
                    }

                    if (pair.Equals("secure", caseInsensitive))
                    {
                        cookie.Secure = true;
                        continue;
                    }

                    if (pair.Equals("httponly", caseInsensitive))
                    {
                        cookie.HttpOnly = true;
                        continue;
                    }

                    continue;
                }

                if (idx == 0)
                {
                    if (cookie != null)
                    {
                        ret.add(cookie);
                        cookie = null;
                    }

                    continue;
                }

                var name = pair.Substring(0, idx).TrimEnd(' ');
                var val = idx < pair.Length - 1 ? pair.Substring(idx + 1).TrimStart(' ')  : string.Empty;

                if (name.Equals("version", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    int num;
                    if (!int.TryParse(val.Unquote(), out num))
                        continue;

                    cookie.Version = num;
                    continue;
                }

                if (name.Equals("expires", caseInsensitive))
                {
                    if (val.Length == 0)
                        continue;

                    if (i == pairs.Count - 1)
                        break;

                    i++;

                    if (cookie == null)
                        continue;

                    if (cookie.Expires != DateTime.MinValue)
                        continue;

                    var buff = new StringBuilder(val, 32);
                    buff.Append($", {pairs[i].Trim()}");

                    DateTime expires;
                    if (!DateTime.TryParseExact(buff.ToString(), new[] { "ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'", "r" }, CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out expires))
                        continue;

                    cookie.Expires = expires.ToLocalTime();
                    continue;
                }

                if (name.Equals("max-age", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    int num;
                    if (!int.TryParse(val.Unquote(), out num))
                        continue;

                    cookie.MaxAge = num;
                    continue;
                }

                if (name.Equals("path", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.Path = val;
                    continue;
                }

                if (name.Equals("domain", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.Domain = val;
                    continue;
                }

                if (name.Equals("port", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.Port = val;
                    continue;
                }

                if (name.Equals("comment", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.Comment = urlDecode(val, Encoding.UTF8);
                    continue;
                }

                if (name.Equals("commenturl", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.CommentUri = val.Unquote().ToUri();
                    continue;
                }

                if (name.Equals("samesite", caseInsensitive))
                {
                    if (cookie == null)
                        continue;

                    if (val.Length == 0)
                        continue;

                    cookie.SameSite = val.Unquote();
                    continue;
                }

                if (cookie != null)
                    ret.add(cookie);

                Cookie.TryCreate(name, val, out cookie);
            }

            if (cookie != null)
                ret.add(cookie);

            return ret;
        }

        private int search(Cookie cookie)
        {
            for (var i = _list.Count - 1; i >= 0; i--)
            {
                if (_list[i].EqualsWithoutValue(cookie))
                    return i;
            }

            return -1;
        }

        private static string urlDecode(string s, Encoding encoding)
        {
            if (s.IndexOfAny(new[] { '%', '+' }) == -1)
                return s;

            try
            {
                return HttpUtility.UrlDecode(s, encoding);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Internal Methods

        internal static CookieCollection Parse(string value, bool response)
        {
            try
            {
                return response ? parseResponse(value) : parseRequest(value);
            }
            catch (Exception ex)
            {
                throw new CookieException("It could not be parsed.", ex);
            }
        }

        internal void SetOrRemove(Cookie cookie)
        {
            var idx = search(cookie);
            if (idx == -1)
            {
                if (cookie.Expired)
                    return;

                _list.Add(cookie);
                return;
            }

            if (cookie.Expired)
            {
                _list.RemoveAt(idx);
                return;
            }

            _list[idx] = cookie;
        }

        internal void SetOrRemove(CookieCollection cookies)
        {
            foreach (var cookie in cookies._list)
                SetOrRemove(cookie);
        }

        internal void Sort()
        {
            if (_list.Count > 1)
                _list.Sort(compareForSort);
        }

        #endregion

        #region Public Methods

        #region Description
        /// <summary>
        /// Adds the specified cookie to the collection.
        /// </summary>
        /// <param name="cookie">
        /// A <see cref="Cookie"/> to add.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The collection is read-only.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cookie"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public void Add(Cookie cookie)
        {
            if (_readOnly)
            {
                throw new InvalidOperationException("The collection is read-only.");
            }

            if (cookie == null)
                throw new ArgumentNullException("cookie");

            add(cookie);
        }

        #region Description
        /// <summary>
        /// Adds the specified cookies to the collection.
        /// </summary>
        /// <param name="cookies">
        /// A <see cref="CookieCollection"/> that contains the cookies to add.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The collection is read-only.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cookies"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public void Add(CookieCollection cookies)
        {
            if (_readOnly)
            {
                throw new InvalidOperationException("The collection is read-only.");
            }

            if (cookies == null)
                throw new ArgumentNullException("cookies");

            foreach (var cookie in cookies._list)
                add(cookie);
        }

        #region Description
        /// <summary>
        /// Removes all cookies from the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The collection is read-only.
        /// </exception>
        #endregion Description
        public void Clear()
        {
            if (_readOnly)
            {
                throw new InvalidOperationException("The collection is read-only.");
            }

            _list.Clear();
        }

        #region Description
        /// <summary>
        /// Determines whether the collection contains the specified cookie.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the cookie is found in the collection; otherwise,
        /// <c>false</c>.
        /// </returns>
        /// <param name="cookie">
        /// A <see cref="Cookie"/> to find.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cookie"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public bool Contains(Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException("cookie");

            return search(cookie) > -1;
        }

        #region Description
        /// <summary>
        /// Copies the elements of the collection to the specified array,
        /// starting at the specified index.
        /// </summary>
        /// <param name="array">
        /// An array of <see cref="Cookie"/> that specifies the destination of
        /// the elements copied from the collection.
        /// </param>
        /// <param name="index">
        /// An <see cref="int"/> that specifies the zero-based index in
        /// the array at which copying starts.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The space from <paramref name="index"/> to the end of
        /// <paramref name="array"/> is not enough to copy to.
        /// </exception>
        #endregion Description
        public void CopyTo(Cookie[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "Less than zero.");

            if (array.Length - index < _list.Count)
            {
                throw new ArgumentException("The available space of the array is not enough to copy to.");
            }

            _list.CopyTo(array, index);
        }

        #region Description
        /// <summary>
        /// Gets the enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.IEnumerator{Cookie}"/>
        /// instance that can be used to iterate through the collection.
        /// </returns>
        #endregion Description
        public IEnumerator<Cookie> GetEnumerator() => _list.GetEnumerator();

        #region Description
        /// <summary>
        /// Removes the specified cookie from the collection.
        /// </summary>
        /// <returns>
        ///   <para>
        ///   <c>true</c> if the cookie is successfully removed; otherwise,
        ///   <c>false</c>.
        ///   </para>
        ///   <para>
        ///   <c>false</c> if the cookie is not found in the collection.
        ///   </para>
        /// </returns>
        /// <param name="cookie">
        /// A <see cref="Cookie"/> to remove.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The collection is read-only.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cookie"/> is <see langword="null"/>.
        /// </exception>
        #endregion Description
        public bool Remove(Cookie cookie)
        {
            if (_readOnly)
            {
                throw new InvalidOperationException("The collection is read-only.");
            }

            if (cookie == null)
                throw new ArgumentNullException("cookie");

            var idx = search(cookie);
            if (idx == -1)
                return false;

            _list.RemoveAt(idx);
            return true;
        }

        #endregion

        #region Explicit Interface Implementations

        #region Description
        /// <summary>
        /// Gets the enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> instance that can be used to iterate
        /// through the collection.
        /// </returns>
        #endregion Description
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        #endregion
    }
}