using Prism.Mvvm;
using Prism.Navigation;

namespace Mopr.Workbench.Core.Mvvm
{
    public abstract class ViewModelBase : BindableBase, IDestructible
    {
        protected ViewModelBase()
        {
        }

        public virtual void Destroy()
        {
        }
    }
}