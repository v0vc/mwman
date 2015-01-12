using Mwman.Common;
using Mwman.ViewModels;
using Ninject;

namespace Mwman
{
    public class ViewModelLocator
    {
        public static MainWindowViewModel MvViewModel
        {
            get { return NinjectContainer.VmKernel.Get<MainWindowViewModel>(); }
        }
    }
}
