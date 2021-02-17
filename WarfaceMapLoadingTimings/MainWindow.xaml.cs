using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WarfaceMapLoadingTimings.Models;

namespace WarfaceMapLoadingTimings
{
	public partial class MainWindow : Window
	{
		private ConcurrentDictionary<string, MapTimingsInfo> Cache { get; }
			= new ConcurrentDictionary<string, MapTimingsInfo>();

		public MainWindow()
		{
			this.InitializeComponent();
		}

		private void btnComputeTimings_Click(object sender, RoutedEventArgs e)
		{
			if (!Directory.Exists(txtGamePath.Text))
				return;

			lvResults.Items.Clear();
			btnComputeTimings.IsEnabled = txtGamePath.IsEnabled = false;
			pbState.Visibility = Visibility.Visible;
			pbState.Value = 0;
			Grid.SetRowSpan(lvResults, 1);

			var files = Directory.GetFiles(Path.Combine(txtGamePath.Text, "LogBackups"), "*log").ToList();

			var lastGameLogFile = Path.Combine(txtGamePath.Text, "Game.log");

			if (File.Exists(lastGameLogFile))
				files.Add(lastGameLogFile);

			pbState.Maximum = files.Count;

			_ = Task.Run(() => this.ProcessFiles(files));
		}

		string ConfigFileName => Path.Combine(Directory.GetCurrentDirectory(), "config.ini");

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			try { File.WriteAllText(this.ConfigFileName, txtGamePath.Text); }
			catch { }
		}

		void DoProgressBarStep()
			=> this.Dispatcher.Invoke(() => pbState.Value += 1);

		static readonly Regex LoadingLevelPattern = new Regex(@".*\*LOADING\: Level (?<level_name>.*) loading time: (?<load_time>.*) (?<load_time_unit>.*)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

		void ProcessFiles(IEnumerable<string> files)
		{
			this.Cache.Clear();

			foreach (var file in files)
			{
				try
				{
					foreach (var line in File.ReadLines(file))
					{
						if (LoadingLevelPattern.IsMatch(line))
						{
							var match = LoadingLevelPattern.Match(line);

							var levelName = match.Groups["level_name"].Value;
							var levelLoadTimeRaw = float.Parse(match.Groups["load_time"].Value, CultureInfo.InvariantCulture);
							var levelLoadTime = TimeSpan.FromSeconds(levelLoadTimeRaw);

							if (this.Cache.TryGetValue(levelName, out var info))
								info.AddTime(levelLoadTime);
							else
							{
								info = new MapTimingsInfo();
								info.AddTime(levelLoadTime);
								this.Cache[levelName] = info;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}

				this.DoProgressBarStep();
			}

			this.Dispatcher.Invoke(this.PopuplateResults);
		}

		void PopuplateResults()
		{
			foreach (var (id, info) in this.Cache.OrderBy(x => x.Key))
			{
				lvResults.Items.Add(new
				{
					Name = id,
					LoadTimeAvg = info.LoadTimeAvg.ToString(@"m\:ss"),
					LoadTimeMax = info.LoadTimeMax.ToString(@"m\:ss"),
					LoadTimeCount = info.Count,
				});
			}

			btnComputeTimings.IsEnabled = txtGamePath.IsEnabled = true;
			pbState.Visibility = Visibility.Collapsed;
			Grid.SetRowSpan(lvResults, 2);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (File.Exists(this.ConfigFileName))
				txtGamePath.Text = File.ReadAllText(this.ConfigFileName);
		}
	}
}
