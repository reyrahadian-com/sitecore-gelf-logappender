using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScGraylog
{
	public class GelfMessage : Dictionary<string, object>
	{
		private const string FacilityKey = "facility";
		private const string FileKey = "file";
		private const string FullMessageKey = "full_message";
		private const string HostKey = "host";
		private const string LevelKey = "level";
		private const string LineKey = "line";
		private const string ShortMessageKey = "short_message";
		private const string VersionKey = "version";
		private const string TimeStampKey = "timestamp";

		public string Facility
		{
			get { return PullStringValue("facility"); }
			set { StoreValue("facility", value); }
		}

		public string File
		{
			get { return PullStringValue("file"); }
			set { StoreValue("file", value); }
		}

		public string FullMessage
		{
			get { return PullStringValue("full_message"); }
			set { StoreValue("full_message", value); }
		}

		public string Host
		{
			get { return PullStringValue("host"); }
			set { StoreValue("host", value); }
		}

		public long Level
		{
			get
			{
				if (!ContainsKey("level"))
					return int.MinValue;
				return (long) this["level"];
			}
			set { StoreValue("level", value); }
		}

		public string Line
		{
			get { return PullStringValue("line"); }
			set { StoreValue("line", value); }
		}

		public string ShortMessage
		{
			get { return PullStringValue("short_message"); }
			set { StoreValue("short_message", value); }
		}

		public DateTime TimeStamp
		{
			get
			{
				double result;
				if (!ContainsKey("timestamp") ||
				    !double.TryParse(this["timestamp"] as string, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
					return DateTime.MinValue;
				return result.FromUnixTimestamp();
			}
			set { StoreValue("timestamp", value.ToUnixTimestamp().ToString(CultureInfo.InvariantCulture)); }
		}

		public string Version
		{
			get { return PullStringValue("version"); }
			set { StoreValue("version", value); }
		}

		private string PullStringValue(string key)
		{
			if (!ContainsKey(key))
				return string.Empty;
			return this[key].ToString();
		}

		private void StoreValue(string key, object value)
		{
			if (!ContainsKey(key))
				Add(key, value);
			else
				this[key] = value;
		}
	}
}