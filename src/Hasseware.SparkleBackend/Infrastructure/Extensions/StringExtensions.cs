using System.Text;

namespace System
{
    internal static class StringExtensions
    {
        public static string ToSlug(this string @this)
        {
            var sb = new StringBuilder(@this.Length);
            bool prevdash = false;

            foreach (char c in @this)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                    prevdash = false;
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    sb.Append(Char.ToLower(c));
                    prevdash = false;
                }
                else if ((int)c >= 128)
                {
                    string remapping;
                    if (TryRemapInternationalCharToAscii(c, out remapping))
                    {
                        sb.Append(remapping);
                        prevdash = false;
                    }
                }
                else if (c == ' ' || c == ',' || c == '.' || c == '/' ||
                    c == '\\' || c == '-' || c == '_' || c == '=')
                {
                    if (!prevdash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevdash = true;
                    }
                }
            }
            if (prevdash) sb.Length--;
            return sb.ToString();
        }
		
        private static bool TryRemapInternationalCharToAscii(char c, out string result)
        {
			switch (Char.ToLower(c))
			{
				case 'à':
				case 'å':
				case 'á':
				case 'â':
				case 'ä':
				case 'ã':
				case 'ą':
					result = "a";
					return true;
				case 'è':
				case 'é':
				case 'ê':
				case 'ë':
				case 'ę':
					result = "e";
					return true;
				case 'ì':
				case 'í':
				case 'î':
				case 'ï':
				case 'ı':
					result = "i";
					return true;
				case 'ò':
				case 'ó':
				case 'ô':
				case 'õ':
				case 'ö':
				case 'ø':
				case 'ő':
				case 'ð':
					result = "o";
					return true;
				case 'ù':
				case 'ú':
				case 'û':
				case 'ü':
				case 'ŭ':
				case 'ů':
					result = "u";
					return true;
				case 'ç':
				case 'ć':
				case 'č':
				case 'ĉ':
					result = "c";
					return true;
				case 'ż':
				case 'ź':
				case 'ž':
					result = "z";
					return true;
				case 'ś':
				case 'ş':
				case 'š':
				case 'ŝ':
					result = "s";
					return true;
				case 'ñ':
				case 'ń':
					result = "n";
					return true;
				case 'ý':
				case 'ÿ':
					result = "y";
					return true;
				case 'ğ':
				case 'ĝ':
					result = "g";
					return true;
				case 'ř':
					result = "r";
					return true;
				case 'ł':
					result = "l";
					return true;
				case 'đ':
					result = "d";
					return true;
				case 'ß':
					result = "ss";
					return true;
				case 'Þ':
					result = "th";
					return true;
				case 'ĥ':
					result = "h";
					return true;
				case 'ĵ':
					result = "j";
					return true;
				default:
				result = String.Empty;
				return false;
			}
        }
    }
}