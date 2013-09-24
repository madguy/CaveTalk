﻿namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Linq;
	using System.Net;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTalk.Lib;

	public sealed class StartBroadcastViewModel : ViewModelBase {
		public event Action<String> OnClose;

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

		private String tags;
		public String Tags {
			get { return this.tags; }
			set {
				this.tags = value;
				base.OnPropertyChanged("Tags");
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

		private Visibility frontLayerVisibility;
		public Visibility FrontLayerVisibility {
			get { return this.frontLayerVisibility; }
			set {
				this.frontLayerVisibility = value;
				base.OnPropertyChanged("FrontLayerVisibility");
			}
		}

		private CaveTubeClientWrapper client;
		private Config config;
		private Int32 previousCount;

		public ICommand StartBroadcastCommand { get; private set; }
		public ICommand StartTestBroadcastCommand { get; private set; }
		public ICommand LoadPreviousSettingCommand { get; private set; }

		public StartBroadcastViewModel() {
			this.config = Model.Config.GetConfig();

			this.FrontLayerVisibility = Visibility.Hidden;
			this.IdVisible = BooleanType.False;
			this.AnonymousOnly = BooleanType.False;

			this.StartBroadcastCommand = new RelayCommand(p => {
				this.StartEntry(false);
			});
			this.StartTestBroadcastCommand = new RelayCommand(p => {
				var message = "テスト配信を開始します。よろしいですか？\n\nテスト配信は画質のチェックや音量の確認などを行うための機能です。\n配信通知は行われませんが、配信は10分程度で強制的に終了します。URLを教えれば他の人に見てもらうことも可能です。なお、録画は残ります。";
				var result = MessageBox.Show(message, "確認", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
				if (result != MessageBoxResult.OK) {
					return;
				}
				this.StartEntry(true);
			});
			this.LoadPreviousSettingCommand = new RelayCommand(p => {
				this.LoadPreviousSetting();
			});

			this.client = new CaveTubeClientWrapper();
			client.Connect();
		}

		private void LoadPreviousSetting() {
			var rooms = Model.Room.GetRooms(config.UserId);
			var room = rooms.ElementAtOrDefault(this.previousCount);
			if (room == null) {
				MessageBox.Show(String.Format("{0}回前の配信は存在しません。", this.previousCount + 1));
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
		}

		private void StartEntry(Boolean isTestMode) {
			try {
				Mouse.OverrideCursor = Cursors.Wait;

				var streamName = this.RequestStartBroadcast(isTestMode, this.client.SocketId);
				if (String.IsNullOrWhiteSpace(streamName)) {
					MessageBox.Show("配信の開始に失敗しました。", "注意", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
					return;
				}

				this.WaitStream(streamName);
			} finally {
				Mouse.OverrideCursor = null;
			}
		}

		private String RequestStartBroadcast(Boolean isTestMode = false, String socketId = "") {
			var apiKey = config.ApiKey;
			if (String.IsNullOrWhiteSpace(apiKey)) {
				return String.Empty;
			}

			this.Title = this.Title ?? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
			this.Description = this.Description ?? String.Empty;
			var tags = String.IsNullOrWhiteSpace(this.Tags) ? new String[0] : Regex.Split(this.Tags, "\\s+");

			var streamInfo = CaveTubeClient.CaveTubeEntry.RequestStartBroadcast(this.Title, config.ApiKey, this.Description, tags, 0, this.IdVisible == BooleanType.True, this.AnonymousOnly == BooleanType.True, false, isTestMode, socketId);
			if (String.IsNullOrEmpty(streamInfo.WarnMessage) == false) {
				MessageBox.Show(streamInfo.WarnMessage, "注意", MessageBoxButton.OK, MessageBoxImage.Warning);
			}

			return streamInfo.StreamName;
		}

		private void WaitStream(String streamName) {
			this.FrontLayerVisibility = Visibility.Visible;
			client.OnNotifyLiveStart += liveInfo => {
				if (liveInfo.RoomId != streamName) {
					return;
				}

				client.Dispose();
				if (this.OnClose != null) {
					this.OnClose(streamName);
				}
			};
		}

		public enum BooleanType {
			True, False,
		}
	}
}
