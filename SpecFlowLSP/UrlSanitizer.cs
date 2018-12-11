using System;
using System.Web;

namespace SpecFlowLSP
{
    public static class UrlSanitizer
    {
        public static string SanitizeUrl(in string url)
        {
            return new Uri(HttpUtility.UrlDecode(url)).LocalPath;
        }
    }
}