using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Demo.ViewModels
{
    public class LauncherPageModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string _MinecraftRoot;
        public string MinecraftRoot
        {
            get { return _MinecraftRoot; }
            set { _MinecraftRoot = value; }
        }

        private string _JavaExecutableFile;
        public string JavaExecutableFile
        {
            get { return _JavaExecutableFile; }
            set { _JavaExecutableFile = value; }
        }
    }
}
