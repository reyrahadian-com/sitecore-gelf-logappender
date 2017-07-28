using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using log4net.Layout;
using log4net.spi;
using Newtonsoft.Json;

namespace ScGraylog.Layout
{
	public class GelfLayout : LayoutSkeleton
	{
		private const string NullText = "(null)";
		private const string GELF_VERSION = "1.0";
		private const int SHORT_MESSAGE_LENGTH = 250;

		private static readonly IEnumerable<string> FullMessageKeyValues = new string[3]
		{
			"FULLMESSAGE",
			"FULL_MESSAGE",
			"MESSAGE"
		};

		private static readonly IEnumerable<string> ShortMessageKeyValues = new string[3]
		{
			"SHORTMESSAGE",
			"SHORT_MESSAGE",
			"MESSAGE"
		};

		private readonly PatternLayout _patternLayout;
		private string _additionalFields;

		public GelfLayout()
		{
			_patternLayout = new PatternLayout();
		}

		public override string ContentType
		{
			get { return "application/json"; }
		}

		public override bool IgnoresException
		{
			get { return false; }
		}

		public bool IncludeLocationInformation { get; set; }

		public string HostName { get; set; }

		public string Facility { get; set; }
		public bool LogStackTraceFromMessage { get; set; }

		public string ConversionPattern { get; set; }

		public string AdditionalFields { get; set; }

		public string KeyValueSeparator { get; set; }

		public string FieldSeparator { get; set; }


		public override void ActivateOptions()
		{
		}

		public override string Format(LoggingEvent loggingEvent)
		{
			var gelfMessage = GetGelfMessage(loggingEvent);
			AddLoggingEventToMessage(loggingEvent, gelfMessage);
			AddAdditionalFields(loggingEvent, gelfMessage);
			return JsonConvert.SerializeObject(gelfMessage, Formatting.Indented);
		}

		private void AddAdditionalFields(LoggingEvent loggingEvent, GelfMessage gelfMessage)
		{
			var dictionary = ParseField(AdditionalFields) ?? new Dictionary<string, object>();
			foreach (DictionaryEntry property in loggingEvent.Properties.ToDictionary())
			{
				var key = property.Key as string;
				if (key != null && !key.StartsWith("log4net:"))
					dictionary.Add(key, FormatAdditionalField(property.Value));
			}
			foreach (var keyValuePair in dictionary)
			{
				var index = keyValuePair.Key.StartsWith("_") ? keyValuePair.Key : "_" + keyValuePair.Key;
				var pattern = keyValuePair.Value as string;
				var obj = pattern == null || !pattern.StartsWith("%")
					? keyValuePair.Value
					: GetValueFromPattern(loggingEvent, pattern);
				gelfMessage[index] = obj;
			}
		}

		private Dictionary<string, object> ParseField(string value)
		{
			var innerAdditionalFields = new Dictionary<string, object>();

			if (value != null)
			{
				string[] fields;
				if (!string.IsNullOrEmpty(FieldSeparator))
					fields = value.Split(new[] {FieldSeparator}, StringSplitOptions.RemoveEmptyEntries);
				else
					fields = value.Split(',');

				if (!string.IsNullOrEmpty(KeyValueSeparator))
					innerAdditionalFields = fields
						.Select(it => it.Split(new[] {KeyValueSeparator}, StringSplitOptions.RemoveEmptyEntries))
						.ToDictionary(it => it[0], it => (object) it[1]);
				else
					innerAdditionalFields = fields
						.Select(it => it.Split(':'))
						.ToDictionary(it => it[0], it => (object) it[1]);
			}
			return innerAdditionalFields;
		}


		private void AddLoggingEventToMessage(LoggingEvent loggingEvent, GelfMessage gelfMessage)
		{
			if (!string.IsNullOrWhiteSpace(ConversionPattern))
			{
				var valueFromPattern = GetValueFromPattern(loggingEvent, ConversionPattern);
				gelfMessage.FullMessage = valueFromPattern;
				gelfMessage.ShortMessage = valueFromPattern.TruncateMessage(250);
			}
			else
			{
				var messageObject = loggingEvent.MessageObject;
				if (messageObject == null)
				{
					if (!string.IsNullOrEmpty(loggingEvent.RenderedMessage))
					{
						if (loggingEvent.RenderedMessage.ValidateJSON())
							AddToMessage(gelfMessage, loggingEvent.RenderedMessage.ToJson().ToDictionary());
						gelfMessage.FullMessage = !string.IsNullOrEmpty(gelfMessage.FullMessage)
							? gelfMessage.FullMessage
							: loggingEvent.RenderedMessage;
						gelfMessage.ShortMessage = !string.IsNullOrEmpty(gelfMessage.ShortMessage)
							? gelfMessage.ShortMessage
							: gelfMessage.FullMessage.TruncateMessage(250);
						return;
					}
					gelfMessage.FullMessage = NullText;
					gelfMessage.ShortMessage = NullText;
				}
				if (messageObject is string)
				{
					var message = messageObject.ToString();
					gelfMessage.FullMessage = message;
					gelfMessage.ShortMessage = message.TruncateMessage(250);
				}
				else if (messageObject is IDictionary)
				{
					AddToMessage(gelfMessage, messageObject as IDictionary);
				}
				else
				{
					AddToMessage(gelfMessage, messageObject.ToDictionary());
				}
				gelfMessage.FullMessage = !string.IsNullOrEmpty(gelfMessage.FullMessage)
					? gelfMessage.FullMessage
					: messageObject.ToString();
				gelfMessage.ShortMessage = !string.IsNullOrEmpty(gelfMessage.ShortMessage)
					? gelfMessage.ShortMessage
					: gelfMessage.FullMessage.TruncateMessage(250);
			}
			if (!LogStackTraceFromMessage)
				return;
			gelfMessage.FullMessage = string.Format("{0} - {1}.", gelfMessage.FullMessage,
				loggingEvent.GetExceptionStrRep());
		}

		private void AddToMessage(GelfMessage gelfMessage, IDictionary messageObject)
		{
			foreach (DictionaryEntry dictionaryEntry in messageObject)
			{
				var str = (dictionaryEntry.Key ?? string.Empty).ToString();
				var message = (dictionaryEntry.Value ?? string.Empty).ToString();
				if (FullMessageKeyValues.Contains(str, StringComparer.OrdinalIgnoreCase))
				{
					gelfMessage.FullMessage = message;
				}
				else if (ShortMessageKeyValues.Contains(str, StringComparer.OrdinalIgnoreCase))
				{
					gelfMessage.ShortMessage = message.TruncateMessage(250);
				}
				else
				{
					var index = str.StartsWith("_") ? str : "_" + str;
					gelfMessage[index] = FormatAdditionalField(dictionaryEntry.Value);
				}
			}
		}

		private object FormatAdditionalField(object value)
		{
			if (value != null && !value.GetType().IsNumeric())
				return value.ToString();
			return value;
		}

		private string GetValueFromPattern(LoggingEvent loggingEvent, string pattern)
		{
			_patternLayout.ConversionPattern = pattern;
			_patternLayout.ActivateOptions();

			return _patternLayout.Format(loggingEvent);
		}

		private GelfMessage GetGelfMessage(LoggingEvent loggingEvent)
		{
			var gelfMessage = new GelfMessage
			{
				Facility = Facility ?? "GELF",
				File = string.Empty,
				Host = HostName ?? Environment.MachineName,
				Level = GetSyslogSeverity(loggingEvent.Level),
				Line = string.Empty,
				TimeStamp = loggingEvent.TimeStamp,
				Version = "1.1"
			};
			gelfMessage.Add("LoggerName", loggingEvent.LoggerName);
			if (IncludeLocationInformation)
			{
				gelfMessage.File = loggingEvent.LocationInformation.FileName != "?"
					? loggingEvent.LocationInformation.FileName
					: string.Empty;
				gelfMessage.Line = loggingEvent.LocationInformation.LineNumber != "?"
					? loggingEvent.LocationInformation.LineNumber
					: string.Empty;
			}

			return gelfMessage;
		}

		private static long GetSyslogSeverity(Level level)
		{
			if (level == Level.ALERT)
				return 1;
			if (level == Level.CRITICAL || level == Level.FATAL)
				return 2;
			if (level == Level.DEBUG)
				return 7;
			if (level == Level.EMERGENCY)
				return 0;
			if (level == Level.ERROR)
				return 3;
			if (level == Level.FINE || level == Level.FINER || level == Level.FINEST || level == Level.INFO || level == Level.OFF)
				return 6;
			if (level == Level.NOTICE || level == Level.VERBOSE || level == Level.TRACE)
				return 5;
			if (level == Level.SEVERE)
				return 0;
			return level == Level.WARN ? 4L : 7L;
		}
	}
}