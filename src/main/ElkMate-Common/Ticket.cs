using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ElkMate.Common
{
    public class Ticket
    {
        private const string DateTimeEncodingFormat = "yyyyMMddHHmm";
        private const int DateTimeEncodedLength = 12; // should be enough to hold a datetime as number (in hex)

	    private static Func<DateTime> TimeExtractor = () => DateTime.Now;
	    public static void SetTimeExtractor(Func<DateTime> timeExtractor)
	    {
		    TimeExtractor = timeExtractor;
	    } 

        public static string GenerateUsing(DateTime expiration, Guid key)
        {
            var clearTicket = key.ToString("N") + Convert.ToInt64(expiration.ToString(DateTimeEncodingFormat)).ToString("x" + DateTimeEncodedLength);

            var encodedTicket = ByteArrayToSafeUrlString(HexStringToByteArray(clearTicket));

            return encodedTicket;
        }

        public static string Generate(TimeSpan lifeTime)
        {
			return GenerateUsing(TimeExtractor() + lifeTime, Guid.NewGuid());
        }

        public static string GenerateFromHours(int ticketLifetimeHours)
        {
            return Generate(TimeSpan.FromHours(ticketLifetimeHours));
        }

        public static bool HasExpired(string ticket, DateTime now)
        {
            var decodedBytes = UrlStringToByteArray(ticket);
            var decodedTicket = ByteArrayToHexString(decodedBytes);

            var datetimePart = Convert.ToInt64(decodedTicket.Substring(decodedTicket.Length - DateTimeEncodedLength, DateTimeEncodedLength), 16).ToString();

            try
            {
                var dateTime = DateTime.ParseExact(datetimePart, DateTimeEncodingFormat, CultureInfo.InvariantCulture);
                return now > dateTime;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static bool HasExpired(string ticket)
        {
			return HasExpired(ticket,TimeExtractor());
        }

        private static byte[] HexStringToByteArray(String hexString)
        {
            var numberChars = hexString.Length;
            var bytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            return bytes;
        }

        private static string ByteArrayToHexString(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private static string ByteArrayToSafeUrlString(byte[] bytes)
        {
            var str = Convert.ToBase64String(bytes.ToArray(), Base64FormattingOptions.None);
            return str.Replace("=", String.Empty).Replace('+', '-').Replace('/', '_');//.Replace('+', '-').Replace('/', '_').Replace('=', '.');
        }

        private static byte[] UrlStringToByteArray(string input)
        {
            var str = input.Replace('-', '+').Replace('_', '/'); //input.Replace('-', '+').Replace('_', '/').Replace('.', '=');
            str = str.PadRight(str.Length + (4 - str.Length % 4) % 4, '=');
            return Convert.FromBase64String(str);
        }
    }
}