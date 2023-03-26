using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient.ViewModels
{
    public abstract class ViewModelBase<T> : INotifyPropertyChanged
    {
        protected T Model { get; private set; }

        protected ViewModelBase(T model) 
        {
            Model = model;
        }

        #region Property Changed
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion Property Changed
    }


}
