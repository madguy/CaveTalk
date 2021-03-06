﻿namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Utils;

	public sealed class OptionWindowViewModel : ViewModelBase {
		public event Action OnClose;

		private OptionBaseViewModel optionWindow;
		public OptionBaseViewModel OptionWindow {
			get { return optionWindow; }
			set {
				optionWindow = value;
				base.OnPropertyChanged("OptionWindow");
			}
		}

		private OptionBaseViewModel generalOption;
		private OptionBaseViewModel commentOption;
		private OptionBaseViewModel speechOption;
		private OptionBaseViewModel streamOption;

		public ICommand GeneralOptionOpenCommand { get; private set; }
		public ICommand CommentOptionOpenCommand { get; private set; }
		public ICommand SpeechOptionOpenCommand { get; private set; }
		public ICommand StreamOptionOpenCommand { get; private set; }
		public ICommand SaveCommand { get; private set; }
		public ICommand CancelCommand { get; private set; }

		public OptionWindowViewModel() {
			this.generalOption = new GeneralOptionViewModel();
			this.commentOption = new CommentOptionViewModel();
			this.speechOption = new SpeechOptionViewModel();
			this.streamOption = new StreamOptionViewModel();

			this.OptionWindow = this.speechOption;

			this.GeneralOptionOpenCommand = new RelayCommand(p => {
				this.OptionWindow = this.generalOption;
			});
			this.CommentOptionOpenCommand = new RelayCommand(p => {
				this.OptionWindow = this.commentOption;
			});
			this.SpeechOptionOpenCommand = new RelayCommand(p => {
				this.OptionWindow = this.speechOption;
			});
			this.StreamOptionOpenCommand = new RelayCommand(p => {
				this.OptionWindow = this.streamOption;
			});
			this.SaveCommand = new RelayCommand(p => {
				this.generalOption.Save();
				this.commentOption.Save();
				this.OnClose?.Invoke();
			});
			this.CancelCommand = new RelayCommand(p => {
				this.OnClose?.Invoke();
			});
		}
	}
}
