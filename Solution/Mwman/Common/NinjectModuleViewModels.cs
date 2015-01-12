using Mwman.ViewModels;
using Ninject.Modules;

namespace Mwman.Common
{
    public class NinjectModuleViewModels :NinjectModule
    {
        public override void Load()
        {
            Bind<MainWindowViewModel>().ToSelf().InSingletonScope();
        }
    }
}
