﻿namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Threading;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class SpeechOptionViewModel : OptionBaseViewModel {
		private Config config;
		private MediaPlayer player;
		private DispatcherTimer timer;

		public Config.SpeakApplicationType SpeakApplication {
			get { return this.config.SpeakApplication; }
			set {
				this.config.SpeakApplication = value;
				base.OnPropertyChanged("SpeakApplication");
			}
		}

		public String SoftalkFilePath {
			get { return this.config.SofTalkPath; }
			set {
				this.config.SofTalkPath = value;
				base.OnPropertyChanged("SoftalkFilePath");
			}
		}

		public String UserSoundFilePath {
			get { return this.config.UserSoundFilePath; }
			set {
				this.config.UserSoundFilePath = value;
				if (File.Exists(value)) {
					this.player.Open(new Uri(value));
				}
				base.OnPropertyChanged("UserSoundFilePath");
			}
		}

		public Decimal UserSoundTimeout {
			get { return this.config.UserSoundTimeout; }
			set {
					this.config.UserSoundTimeout = value;
					this.timer.Interval = TimeSpan.FromSeconds(Decimal.ToDouble(value));
					base.OnPropertyChanged("UserSoundTimeout");
			}
		}

		public Double UserSoundVolume {
			get { return Math.Floor(this.config.UserSoundVolume * 100); }
			set {
				Double val = value / 100;
				this.config.UserSoundVolume = val;
				this.player.Volume = val;
				base.OnPropertyChanged("UserSoundVolume");
			}
		}

		public Boolean ReadNum {
			get { return this.config.ReadCommentNumber; }
			set {
				this.config.ReadCommentNumber = value;
				base.OnPropertyChanged("ReadNum");
			}
		}

		public Boolean ReadName {
			get { return this.config.ReadCommentName; }
			set {
				this.config.ReadCommentName = value;
				base.OnPropertyChanged("ReadName");
			}
		}

		public ICommand FindSoftalkExeCommand { get; private set; }
		public ICommand FindSoundFileCommand { get; private set; }
		public ICommand PlaySoundFileCommand { get; private set; }
		public ICommand StopSoundFileCommand { get; private set; }

		public SpeechOptionViewModel() {
			this.player = new MediaPlayer();

			this.config = Config.GetConfig();
			this.SoftalkFilePath = config.SofTalkPath;
			this.UserSoundFilePath = config.UserSoundFilePath;
			this.timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher.CurrentDispatcher) {
				Interval = TimeSpan.FromSeconds(Decimal.ToDouble(this.UserSoundTimeout)),
			};
			this.timer.Tick += (e, sender) => {
				this.player.Stop();
				this.timer.Stop();
			};
			this.player.MediaEnded += (e, sender) => {
				this.timer.Stop();
			};

			this.FindSoftalkExeCommand = new RelayCommand(p => {
				var dialog = new OpenFileDialog {
					Filter = "実行ファイル|*.exe",
					CheckFileExists = true,
					CheckPathExists = true,
					Title = "Softalkの選択",
				};
				var result = dialog.ShowDialog();
				result.GetValueOrDefault();
				if (result.GetValueOrDefault() == false) {
					return;
				}
				this.SoftalkFilePath = dialog.FileName;
			});

			this.FindSoundFileCommand = new RelayCommand(p => {
				var dialog = new OpenFileDialog {
					Filter = "サウンドファイル|*.wav;*.mp3|全てのファイル|*.*",
					CheckFileExists = true,
					CheckPathExists = true,
					Title = "サウンドファイルの選択",
				};
				var result = dialog.ShowDialog();
				result.GetValueOrDefault();
				if (result.GetValueOrDefault() == false) {
					return;
				}
				this.UserSoundFilePath = dialog.FileName;
			});

			this.PlaySoundFileCommand = new RelayCommand(p => {
				if (File.Exists(this.UserSoundFilePath) == false) {
					return;
				}
				this.player.Stop();
				this.player.Play();
				if (this.timer.Interval > TimeSpan.Zero) {
					this.timer.Start();
				}
			});

			this.StopSoundFileCommand = new RelayCommand(p => {
				if (this.player.Source == null) {
					return;
				}
				this.player.Stop();
			});
		}

		protected override void OnDispose() {
			base.OnDispose();
			this.player.Close();
		}

		internal override void Save() {
			this.config.Save();
		}
	}
}
