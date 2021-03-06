﻿#region

using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Youtube.Data
{
	public class YoutubeVideoSnippet
	{
		[JsonProperty(PropertyName = "title")]
		public string Title { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "tags")]
		public string[] Tags { get; set; }
	}
}
