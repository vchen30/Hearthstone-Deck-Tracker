﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using De.TorstenMandelkow.MetroChart;
using Hearthstone_Deck_Tracker.Annotations;
using Hearthstone_Deck_Tracker.Controls.Stats.Arena.Overview;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.FlyoutControls;
using Hearthstone_Deck_Tracker.HearthStats.API;
using Hearthstone_Deck_Tracker.Stats;
using Hearthstone_Deck_Tracker.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using DataGrid = System.Windows.Controls.DataGrid;

namespace Hearthstone_Deck_Tracker.Controls.Stats
{
	/// <summary>
	/// Interaction logic for ArenaStats.xaml
	/// </summary>
	public partial class ArenaStats : INotifyPropertyChanged
	{
		private readonly bool _initialized;
		private object _chartWinsControl = new ChartWins();

		public ArenaStats()
		{
			InitializeComponent();
			_initialized = true;
		}

		public object ChartWinsControl
		{
			get { return _chartWinsControl; }
			set
			{
				_chartWinsControl = value; 
				OnPropertyChanged();
			}
		}

		public GameStats SelectedGame { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if(handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		private void ButtonEditRewards_OnClick(object sender, RoutedEventArgs e)
		{
			var run = DataGridArenaRuns.SelectedItem as ArenaRun;
			if(run == null)
				return;
			var rewardDialog = new ArenaRewardDialog(run.Deck) {WindowStartupLocation = WindowStartupLocation.CenterOwner};
			rewardDialog.ShowDialog();
			if(rewardDialog.SaveButtonWasClicked)
				CompiledStats.Instance.UpdateArenaRuns();
		}

		private async void ButtonAddGame_OnClick(object sender, RoutedEventArgs e)
		{
			var run = DataGridArenaRuns.SelectedItem as ArenaRun;
			if(run == null)
				return;
			var window = GetParentWindow();
			if(window == null)
				return;
			var addedGame = await window.ShowAddGameDialog(run.Deck);
			if(addedGame)
				CompiledStats.Instance.UpdateArenaRuns();
		}

		private async void ButtonEditGame_OnClick(object sender, RoutedEventArgs e)
		{
			if(SelectedGame == null)
				return;
			var window = GetParentWindow();
			if(window == null)
				return;
			var edited = await window.ShowEditGameDialog(SelectedGame);
			if(edited)
				CompiledStats.Instance.UpdateArenaRuns();
		}

		public MetroWindow GetParentWindow()
		{
			var parent = VisualTreeHelper.GetParent(this);
			while(parent != null && !(parent is MetroWindow))
				parent = VisualTreeHelper.GetParent(parent);
			return (MetroWindow)parent;
		}

		private async void ButtonDeleteGame_OnClick(object sender, RoutedEventArgs e)
		{
			if(SelectedGame == null)
				return;
			var run = DataGridArenaRuns.SelectedItem as ArenaRun;
			if(run == null)
				return;
			var window = GetParentWindow();
			if(await window.ShowDeleteGameStatsMessage(SelectedGame) != MessageDialogResult.Affirmative)
				return;
			if(run.Deck.DeckStats.Games.Contains(SelectedGame))
			{
				SelectedGame.DeleteGameFile();
				run.Deck.DeckStats.Games.Remove(SelectedGame);
				Logger.WriteLine("Deleted game " + SelectedGame, "ArenaStats.ButtonDeleteGame");
			}
			if(HearthStatsAPI.IsLoggedIn && SelectedGame.HasHearthStatsId && await window.ShowCheckHearthStatsMatchDeletionDialog())
				HearthStatsManager.DeleteMatchesAsync(new List<GameStats> {SelectedGame});
			DeckStatsList.Save();
			Core.MainWindow.DeckPickerList.UpdateDecks();
			CompiledStats.Instance.UpdateArenaRuns();
		}
	}
}
