using System;
using System.Collections.Generic;
using System.Linq;

namespace WarfaceMapLoadingTimings.Models
{
	public class MapTimingsInfo
	{
		public string Name { get; set; }
		public int Count => this.Times.Count;
		private List<TimeSpan> Times { get; }

		public MapTimingsInfo()
		{
			this.Times = new List<TimeSpan>();
		}

		public TimeSpan LoadTimeAvg
		{
			get
			{
				TimeSpan temp;

				lock (this.Times)
				{
					temp = TimeSpan.FromSeconds(this.Times.Sum(x => x.TotalSeconds) / this.Count);
				}

				return temp;
			}
		}

		public TimeSpan LoadTimeMax
		{
			get
			{
				TimeSpan temp;

				lock (this.Times)
				{
					temp = this.Times.OrderByDescending(x => x).FirstOrDefault();
				}

				return temp;
			}
		}

		public void AddTime(TimeSpan t)
		{
			lock (this.Times)
				this.Times.Add(t);
		}
	}
}
