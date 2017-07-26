using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using log4net.Appender;
using log4net.spi;

namespace ScGraylog.Appender
{
	public class GelfHttpAppender : AppenderSkeleton
	{
		private readonly HttpClient _httpClient;
		private Uri _baseUrl;

		public string Url { get; set; }
		public string User { get; set; }
		public string Password { get; set; }

		public GelfHttpAppender()
		{
			//var httpClientHandler = new HttpClientHandler
			//{
			//	Proxy = new WebProxy("http://localhost:8888", false),
			//	UseProxy = true
			//};
			//_httpClient = new HttpClient(httpClientHandler);
			_httpClient = new HttpClient();
		}

		public override void ActivateOptions()
		{
			base.ActivateOptions();
			_baseUrl = new Uri(Url);
			_httpClient.DefaultRequestHeaders.ExpectContinue = false;
			if (string.IsNullOrWhiteSpace(User) || string.IsNullOrWhiteSpace(Password))
				return;
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(User + ":" + Password)));
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			var appender = this;
			try
			{
				//var sample = "{\"short_message\":\"Hello there from Sitecore\", \"host\":\"example.org\", \"facility\":\"test\", \"_foo\":\"bar\"}";
				//var stringContent = new StringContent(sample, Encoding.UTF8, "application/json");
				var stringContent = new StringContent(appender.RenderLoggingEvent(loggingEvent), Encoding.UTF8, "application/json");
				var responseMessage = appender._httpClient.PostAsync(appender._baseUrl, stringContent).Result;
			}
			catch (Exception ex)
			{
				appender.ErrorHandler.Error("Unable to send logging event to remote host: " + appender.Url, ex);
			}
		}
	}
}