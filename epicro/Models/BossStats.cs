using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace epicro.Models
{
    public class BossStats : INotifyPropertyChanged
    {
        private string name;
        private List<TimeSpan> killTimes = new List<TimeSpan>();

        public string Name
        {
            get => name;
            set => SetField(ref name, value);
        }

        public int KillCount => killTimes.Count;
        public TimeSpan TotalTime => new TimeSpan(killTimes.Sum(ts => ts.Ticks));
        public TimeSpan AverageTime => KillCount > 0 ? new TimeSpan(TotalTime.Ticks / KillCount) : TimeSpan.Zero;
        public TimeSpan MaxTime => killTimes.Count > 0 ? killTimes.Max() : TimeSpan.Zero;
        public TimeSpan MinTime => killTimes.Count > 0 ? killTimes.Min() : TimeSpan.Zero;
        public int Over2MinCount => killTimes.Count(ts => ts.TotalSeconds > 120);
        public double Over2MinRate => KillCount > 0 ? (double)Over2MinCount / KillCount * 100 : 0;

        public void AddKill(TimeSpan time)
        {
            killTimes.Add(time);
            OnPropertyChanged(nameof(KillCount));
            OnPropertyChanged(nameof(TotalTime));
            OnPropertyChanged(nameof(AverageTime));
            OnPropertyChanged(nameof(MaxTime));
            OnPropertyChanged(nameof(MinTime));
            OnPropertyChanged(nameof(Over2MinCount));
            OnPropertyChanged(nameof(Over2MinRate));
        }

        public void ResetStats()
        {
            killTimes.Clear();
            OnPropertyChanged(nameof(KillCount));
            OnPropertyChanged(nameof(TotalTime));
            OnPropertyChanged(nameof(AverageTime));
            OnPropertyChanged(nameof(MaxTime));
            OnPropertyChanged(nameof(MinTime));
            OnPropertyChanged(nameof(Over2MinCount));
            OnPropertyChanged(nameof(Over2MinRate));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

}
