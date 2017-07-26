using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScGraylog
{
	public static class Extensions
	{
		private static readonly HashSet<Type> _numericTypes = new HashSet<Type>
		{
			typeof(decimal),
			typeof(double),
			typeof(float),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(short),
			typeof(ushort)
		};

		public static bool IsNumeric(this Type type)
		{
			return _numericTypes.Contains(type);
		}

		public static IDictionary ToDictionary(this object values)
		{
			var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			if (values != null)
				foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(values))
				{
					var obj = property.GetValue(values);
					dictionary.Add(property.Name, obj);
				}
			return dictionary;
		}

		public static string TruncateMessage(this string message, int length)
		{
			if (message.Length <= length)
				return message;
			return message.Substring(0, length - 1);
		}

		public static bool ValidateJSON(this string s)
		{
			try
			{
				JToken.Parse(s);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static object ToJson(this string s)
		{
			return JsonConvert.DeserializeObject(s);
		}

		public static byte[] GzipMessage(this string message, Encoding encoding)
		{
			var bytes = encoding.GetBytes(message);
			var memoryStream = new MemoryStream();
			using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
			{
				gzipStream.Write(bytes, 0, bytes.Length);
			}
			memoryStream.Position = 0L;
			var buffer = new byte[memoryStream.Length];
			memoryStream.Read(buffer, 0, buffer.Length);
			return buffer;
		}

		public static double ToUnixTimestamp(this DateTime d)
		{
			return (d.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
		}

		public static DateTime FromUnixTimestamp(this double d)
		{
			var dateTime = new DateTime(1970, 1, 1, 0, 0, 0);
			dateTime = dateTime.AddMilliseconds(d * 1000.0);
			return dateTime.ToLocalTime();
		}
	}
}