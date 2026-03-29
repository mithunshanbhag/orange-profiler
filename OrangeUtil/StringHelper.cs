using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrangeUtil
{
    //////////////////////////////////
    //----< class StringHelper >----//
    //////////////////////////////////

    /// <summary>
    /// Contains some useful extension methods for System.String
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Strips the opening and closing quotes from a given string (if they exist).
        /// </summary>
        /// <param name="str">The string to be operated upon.</param>
        /// <returns>The modified string.</returns>            
        public static string StripLeadingLaggingQuotes(this string str)
        {
            // we'll strip the begining and ending quotes if any...
            if (str.StartsWith("\"") && str.EndsWith("\"") && str.Length > 2)
                return str.Substring(1, str.Length - 2);
            return str;
        }


        /// <summary>
        /// Appends given string at the end of the original string if not already appended.
        /// </summary>
        /// <param name="str">The original string to which we may append something.</param>
        /// <param name="append">The string to be appended.</param>
        /// <returns>The modified string.</returns>            
        public static string AppendIfNeeded(this string str, string append)
        {
            if (string.IsNullOrEmpty(str))
                throw new ArgumentException("Cannot pass in null or zero-length string as argument", "str");
            if (string.IsNullOrEmpty(append))
                throw new ArgumentException("Cannot pass in null or zero-length string as argument", "append");

            if (str.EndsWith("\""))
            {
                if (!str.EndsWith(append + "\""))
                    str.Insert(str.Length - 1, append + "\"");
            }
            else
            {
                if (!str.EndsWith(append))
                    str += append;
            }
            return str;
        }
    }

    ///////////////////////////////////////////////////////////////////////////
}
