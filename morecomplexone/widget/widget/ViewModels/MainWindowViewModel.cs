using System;
using System.Collections.Generic;
using System.Text;
using widget.ViewModels.Base;

namespace widget.ViewModels
{
    class MainWindowViewModel : ViewModel 
    {
        private string _Title = "lsdfj"; 
        public string Title 
        {
            get => _Title;
            set => Set(ref _Title, value);
        }

    }
}
