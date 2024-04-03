namespace Extensions
{
    public static class StringExtensions
    {
        public static string Reverse(this string str)
        {
            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        public static string Reverse(this string str, int startIndex, int length)
        {
            char[] charArray = str.Substring(startIndex, length).ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
