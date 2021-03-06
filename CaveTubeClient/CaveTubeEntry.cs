﻿namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml;
	using Newtonsoft.Json.Linq;

	public static class CaveTubeEntry {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "https://www.cavelis.net";
		private static String devkey = ConfigurationManager.AppSettings["dev_key"] ?? String.Empty;

		/// <summary>
		/// 配信開始リクエストを行います。
		/// </summary>
		/// <param name="title">タイトル</param>
		/// <param name="apiKey">APIキー</param>
		/// <param name="description">配信詳細</param>
		/// <param name="tags">タグ</param>
		/// <param name="streamService">ストリームサービス</param>
		/// <param name="thumbnailSlot">サムネイルスロット</param>
		/// <param name="idVisible">ID表示の有無</param>
		/// <param name="anonymousOnly">ハンドルネーム制限</param>
		/// <param name="loginOnly">書き込み制限</param>
		/// <param name="testMode">テストモード</param>
		/// <param name="socketId">SocketIOの接続ID</param>
		/// <returns></returns>
		public static async Task<StartInfo> RequestStartBroadcastAsync(String title, String apiKey, String description, IEnumerable<String> tags, StreamService streamService, Int32 thumbnailSlot, Boolean idVisible, Boolean anonymousOnly, Boolean loginOnly, Boolean testMode, String socketId) {
			try {
				using (var client = WebClientUtil.CreateInstance()) {
					var data = new NameValueCollection {
						{"devkey", devkey},
						{"apikey", apiKey},
						{"title", title},
						{"description", description},
						{"tag", String.Join(" ", tags)},
						{"stream_service", streamService.ServiceName},
						{"stream_key", streamService.StreamKey },
						{"channel", streamService.Channel },
						{"thumbnail_slot", thumbnailSlot.ToString()},
						{"id_visible", idVisible ? "true" : "false"},
						{"anonymous_only", anonymousOnly ? "true" : "false"},
						{"login_only", loginOnly ? "true" : "false"},
						{"test_mode", testMode ? "true" : "false"},
						{"socket_id", socketId},
					};

					var response = await client.UploadValuesTaskAsync($"{webUrl}/api/start", "POST", data);
					var jsonString = Encoding.UTF8.GetString(response);

					dynamic json = JObject.Parse(jsonString);
					if (json.stream_name == null) {
						return null;
					}

					return new StartInfo(json);
				}
			} catch (WebException ex) {
				if (ex.Response == null) {
					return null;
				}

				var message = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				if (String.IsNullOrWhiteSpace(message)) {
					return null;
				}

				throw new CavetubeException(message, ex);
			}
		}

		/// <summary>
		/// ユーザー登録情報を取得します。
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		/// <returns></returns>
		public static async Task<UserData> RequestUserDataAsync(String apiKey) {
			try {
				using (var client = WebClientUtil.CreateInstance()) {
					var jsonString = await client.DownloadStringTaskAsync($"{webUrl}/api/user_data?devkey={devkey}&apikey={apiKey}");
					dynamic json = JObject.Parse(jsonString);
					return new UserData(json);
				}
			} catch (WebException) {
				return new UserData();
			} catch (XmlException) {
				return new UserData();
			}
		}

		/// <summary>
		/// ジャンル一覧を取得します。
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		/// <returns></returns>
		public static async Task<IEnumerable<Genre>> RequestGenre(String apiKey) {
			try {
				using (var client = WebClientUtil.CreateInstance()) {
					var jsonString = await client.DownloadStringTaskAsync($"{webUrl}/api/genre?devkey={devkey}&apikey={apiKey}");
					dynamic json = JObject.Parse(jsonString);

					return ((JArray)json.genres).Select(genre => new Genre(genre));
				}
			} catch (WebException) {
				return Enumerable.Empty<Genre>();
			} catch (XmlException) {
				return Enumerable.Empty<Genre>();
			}
		}

		public sealed class UserData {
			public IEnumerable<Thumbnail> Thumbnails { get; private set; }

			internal UserData() {
				this.Thumbnails = Enumerable.Empty<Thumbnail>();
			}

			internal UserData(dynamic json) {
				this.Thumbnails = ((JArray)json.thumbnails).Select(t => new Thumbnail(t));
			}
		}

		public sealed class Genre {
			public String Title { get; private set; }
			public IEnumerable<String> Tags { get; private set; }

			internal Genre(dynamic json) {
				this.Title = json.title;
				this.Tags = json.tags.ToObject<IEnumerable<String>>();
			}
		}

		public sealed class Thumbnail {
			public Int32 Slot { get; private set; }
			public String Url { get; private set; }

			internal Thumbnail(dynamic json) {
				this.Slot = (Int32)json.slot;
				this.Url = json.url;
			}
		}

		public sealed class StartInfo {
			public String StreamName { get; private set; }
			public String WarnMessage { get; private set; }

			internal StartInfo(dynamic json) {
				this.StreamName = json.stream_name;
				this.WarnMessage = json.warn_message ?? String.Empty;
			}
		}

		public sealed class StreamService {
			public String ServiceName { get; set; }
			public String StreamKey { get; set; }
			public String Channel { get; set; }
		}
	}
}
