﻿namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Lib;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTubeClient;
	using static CaveTube.CaveTubeClient.CaveTubeEntry;

	public sealed class StartBroadcastViewModel : ViewModelBase {
		public event Action<String> OnStreamStart;

		private String title;
		public String Title {
			get { return this.title; }
			set {
				this.title = value;
				base.OnPropertyChanged("Title");
			}
		}

		private String description;
		public String Description {
			get { return this.description; }
			set {
				this.description = value;
				base.OnPropertyChanged("Description");
			}
		}

		private Genre genre;
		public Genre Genre {
			get { return this.genre; }
			set {
				this.genre = value;
				base.OnPropertyChanged("Genre");
			}
		}

		private String tags;
		public String Tags {
			get { return this.tags; }
			set {
				this.tags = value;
				base.OnPropertyChanged("Tags");
			}
		}

		private AStreamService streamService;
		public AStreamService StreamService {
			get { return this.streamService; }
			set {
				this.streamService = value;
				base.OnPropertyChanged("StreamService");

				if (this.config == null || value == null) {
					return;
				}

				this.config.StreamService = AStreamService.Serialize(value);
			}
		}

		private BooleanType idVisible;
		public BooleanType IdVisible {
			get { return this.idVisible; }
			set {
				this.idVisible = value;
				base.OnPropertyChanged("IdVisible");
			}
		}

		private BooleanType anonymousOnly;
		public BooleanType AnonymousOnly {
			get { return this.anonymousOnly; }
			set {
				this.anonymousOnly = value;
				base.OnPropertyChanged("AnonymousOnly");
			}
		}

		private BooleanType loginOnly;
		public BooleanType LoginOnly {
			get { return this.loginOnly; }
			set {
				this.loginOnly = value;
				base.OnPropertyChanged("LoginOnly");
			}
		}

		private Thumbnail thumbnail;
		public Thumbnail Thumbnail {
			get { return this.thumbnail; }
			set {
				this.thumbnail = value;
				base.OnPropertyChanged("Thumbnail");
			}
		}

		private Visibility frontLayerVisibility;
		public Visibility FrontLayerVisibility {
			get { return this.frontLayerVisibility; }
			set {
				this.frontLayerVisibility = value;
				base.OnPropertyChanged("FrontLayerVisibility");
			}
		}

		private IEnumerable<Genre> genres;
		public IEnumerable<Genre> Genres {
			get {
				return this.genres;
			}
		}

		private IEnumerable<AStreamService> streamServices;
		public IEnumerable<AStreamService> StreamServices {
			get {
				return this.streamServices;
			}
		}

		private IEnumerable<Thumbnail> thumbnails;
		public IEnumerable<Thumbnail> Thumbnails {
			get {
				return this.thumbnails;
			}
		}

		private CaveTubeClientWrapper client;
		private Config config;
		private Int32 previousCount;

		public ICommand StartBroadcastCommand { get; private set; }
		public ICommand StartTestBroadcastCommand { get; private set; }
		public ICommand LoadPreviousSettingCommand { get; private set; }

		public static async Task<StartBroadcastViewModel> CreateInstance() {
			var instance = new StartBroadcastViewModel();
			await instance.Init();
			return instance;
		}

		private StartBroadcastViewModel() {
		}

		private async Task Init() {
			this.config = Model.Config.GetConfig();

			this.FrontLayerVisibility = Visibility.Hidden;
			this.IdVisible = BooleanType.False;
			this.AnonymousOnly = BooleanType.False;
			this.LoginOnly = BooleanType.False;

			this.StartBroadcastCommand = new RelayCommand(p => {
				this.StartEntry(false);
			});
			this.StartTestBroadcastCommand = new RelayCommand(p => {
				var message = "テスト配信を開始します。よろしいですか？\n\nテスト配信は配信通知が行われません。\nただし、配信は10分で自動終了します。\nそれ以外は通常の配信と同等です。";
				var result = MessageBox.Show(message, "確認", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
				if (result != MessageBoxResult.OK) {
					return;
				}
				this.StartEntry(true);
			});
			this.LoadPreviousSettingCommand = new RelayCommand(p => {
				this.LoadPreviousSetting();
			});

			var accessKey = await CavetubeAuth.GetAccessKeyAsync(this.config.AccessKey);
			this.config.AccessKey = accessKey;
			this.config.Save();
			this.client = new CaveTubeClientWrapper(accessKey);
			this.client.Connect();

			this.genres = await this.RequestGenreAsync(this.config.ApiKey);
			this.Genre = this.genres.First();

			this.streamServices = AStreamService.GetStreamServices();
			this.StreamService = AStreamService.Deserialize(this.config.StreamService);

			this.thumbnails = await this.RequestThumbnailsAsync(this.config.ApiKey);
			this.Thumbnail = this.thumbnails.First();
		}

		private void LoadPreviousSetting() {
			var rooms = Model.Room.GetRooms(this.config.UserId);
			var room = rooms.ElementAtOrDefault(this.previousCount);
			if (room == null) {
				MessageBox.Show($"{this.previousCount + 1}回前の配信は存在しません。");
				this.previousCount = 0;
				return;
			}

			// 読み取りカウンタを回します。
			this.previousCount = this.previousCount < 5 ? this.previousCount + 1 : 0;

			this.Title = room.Title;
			this.Description = room.Description;
			this.Tags = room.Tags;
			this.IdVisible = room.IdVisible ? BooleanType.True : BooleanType.False;
			this.AnonymousOnly = room.AnonymousOnly ? BooleanType.True : BooleanType.False;
			this.LoginOnly = BooleanType.False;
		}

		private async void StartEntry(Boolean isTestMode) {
			try {
				Mouse.OverrideCursor = Cursors.Wait;

				var streamName = await this.RequestStartBroadcast(isTestMode, this.client.SocketId);
				if (String.IsNullOrWhiteSpace(streamName)) {
					MessageBox.Show("配信の開始に失敗しました。", "注意", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
					return;
				}

				this.WaitStream(streamName);
			} finally {
				Mouse.OverrideCursor = null;
			}
		}

		private async Task<String> RequestStartBroadcast(Boolean isTestMode = false, String socketId = "") {
			var apiKey = this.config.ApiKey;
			if (String.IsNullOrWhiteSpace(apiKey)) {
				return String.Empty;
			}

			this.Title = this.Title ?? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
			this.Description = this.Description ?? String.Empty;

			var tags = new SortedSet<String>();
			this.genre.Tags.ForEach(t => tags.Add(t));

			if (String.IsNullOrWhiteSpace(this.Tags) == false) {
				Regex.Split(this.Tags, @"\s+").ForEach(t => tags.Add(t));
			}

			var streamService = this.streamService.ToStreamService();

			try {
				var streamInfo = await CaveTubeClient.CaveTubeEntry.RequestStartBroadcastAsync(this.Title, this.config.ApiKey, this.Description, tags, streamService, this.Thumbnail.Slot, this.IdVisible == BooleanType.True, this.AnonymousOnly == BooleanType.True, this.LoginOnly == BooleanType.True, isTestMode, socketId);
				if (streamInfo == null) {
					return String.Empty;
				}

				if (String.IsNullOrEmpty(streamInfo.WarnMessage) == false) {
					MessageBox.Show(streamInfo.WarnMessage, "注意", MessageBoxButton.OK, MessageBoxImage.Warning);
				}

				return streamInfo.StreamName;
			} catch (CavetubeException ex) {
				MessageBox.Show(ex.Message, "注意", MessageBoxButton.OK, MessageBoxImage.Warning);
				return String.Empty;
			}
		}

		private void WaitStream(String streamName) {
			this.FrontLayerVisibility = Visibility.Visible;
			client.OnNotifyLiveStart += liveInfo => {
				if (liveInfo.RoomId != streamName) {
					return;
				}

				this.OnStreamStart?.Invoke(streamName);
			};
		}

		private async Task<IEnumerable<Genre>> RequestGenreAsync(String apiKey) {
			var response = new List<Genre> {
				new Genre { Title = "配信ジャンル(オプション)", Tags = Enumerable.Empty<String>() }
			};

			var genres = await CaveTubeClient.CaveTubeEntry.RequestGenre(apiKey);
			if (genres == null) {
				return response;
			}

			return Enumerable.Concat(response, genres.Select(g => new Genre { Title = g.Title, Tags = g.Tags }));
		}

		private async Task<IEnumerable<Thumbnail>> RequestThumbnailsAsync(String apiKey) {
			var result = new ObservableCollection<Thumbnail>();

			var userData = await CaveTubeClient.CaveTubeEntry.RequestUserDataAsync(apiKey);
			if (userData.Thumbnails.Any()) {
				userData.Thumbnails.ForEach((t, i) => {
					result.Add(new Thumbnail { Url = t.Url, Slot = t.Slot });
				});
			} else {
				result.Add(new Thumbnail { Slot = 0, Url = "/CaveTalk;component/Images/no_thumbnail_image.png" });
			}

			return result;
		}

		protected override void OnDispose() {
			base.OnDispose();

			this.client.Dispose();
		}

		public enum BooleanType {
			True, False,
		}
	}

	public class Genre {
		public String Title { get; set; }
		public IEnumerable<String> Tags { get; set; }
	}

	public class Thumbnail {
		public String Url { get; set; }
		public Int32 Slot { get; set; }
	}

	public abstract class AStreamService {
		protected Config config { get; private set; }
		public abstract String Name { get; }

		public abstract StreamService ToStreamService();

		public AStreamService() {
			this.config = Model.Config.GetConfig();
		}

		public override Boolean Equals(Object obj) {
			var other = obj as AStreamService;
			if (other == null) {
				return false;
			}

			return this.Name == other.Name;
		}

		public override Int32 GetHashCode() {
			return this.Name.GetHashCode();
		}

		public static AStreamService Deserialize(String name) {
			switch (name) {
				case "YouTubeLive":
					return new YouTubeLiveStreamService();
				case "Mixer":
					return new MixerStreamService();
				case "Twitch":
					return new TwitchStreamService();
				default:
					return new CaveTubeStreamService();
			}
		}

		public static String Serialize(AStreamService streamService) {
			return streamService.Name;
		}

		public static IEnumerable<AStreamService> GetStreamServices() {
			return new List<AStreamService> {
				new CaveTubeStreamService(),
				new YouTubeLiveStreamService(),
				new MixerStreamService(),
				new TwitchStreamService(),
			};
		}
	}

	public class CaveTubeStreamService : AStreamService {
		public override String Name => "CaveTube";

		public override StreamService ToStreamService() {
			return new StreamService {
				ServiceName = "cavetube",
				StreamKey = null,
				Channel = null,
			};
		}
	}

	public class YouTubeLiveStreamService : AStreamService {
		public override String Name => "YouTubeLive";

		public override StreamService ToStreamService() {
			return new StreamService {
				ServiceName = "youtubelive",
				StreamKey = this.config.YouTubeStreamKey,
				Channel = this.config.YouTubeChannelId,
			};
		}
	}

	public class MixerStreamService : AStreamService {
		public override String Name => "Mixer";

		public override StreamService ToStreamService() {
			return new StreamService {
				ServiceName = "mixer",
				StreamKey = this.config.MixerStreamKey,
				Channel = this.config.MixerUserId,
			};
		}
	}

	public class TwitchStreamService : AStreamService {
		public override String Name => "Twitch";

		public override StreamService ToStreamService() {
			return new StreamService {
				ServiceName = "twitch",
				StreamKey = this.config.TwitchStreamKey,
				Channel = this.config.TwitchUserId,
			};
		}
	}
}
