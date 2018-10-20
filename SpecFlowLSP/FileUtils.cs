using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpecFlowLSP
{
    public static class FileUtils
    {
        public static List<string> SplitString(in string file)
        {
            return Regex.Split(file, "\r\n|\r|\n").ToList();
        }
    }
}