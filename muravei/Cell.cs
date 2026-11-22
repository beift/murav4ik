using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace muravei
{
    public class Cell : INotifyPropertyChanged
    {
        private bool _isBlack;

        public bool IsBlack
        {
            get => _isBlack;

            set
            {
                if (_isBlack != value)
                {
                    _isBlack = value;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Brush));
                }
            }
        }

        // Цвет клетки для отображения в интерфейсе
        public Brush Brush => IsBlack ? Brushes.Black : Brushes.White;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
