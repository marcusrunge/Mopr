using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Properties;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging
{
    internal sealed class ImagingLayoutService : CreateableBindableBase<IImagingLayoutService, ImagingLayoutService, IImagingServiceBase>, IImagingLayoutService
    {
        private static readonly IReadOnlyList<ViewportDescriptor> AxialSagittalCoronalViewports = new[]
        {
            new ViewportDescriptor(id: "Asc.Axial", title: Resources.ImagingLayoutService_Axial, orientation: ViewportOrientation.Axial),
            new ViewportDescriptor(id: "Asc.Sagittal", title: Resources.ImagingLayoutService_Sagittal, orientation: ViewportOrientation.Sagittal),
            new ViewportDescriptor(id: "Asc.Coronal", title: Resources.ImagingLayoutService_Coronal, orientation: ViewportOrientation.Coronal)
        };

        private static readonly IReadOnlyList<ViewportDescriptor> MprViewports = new[]
        {
            new ViewportDescriptor(id: "Mpr.Axial", title: Resources.ImagingLayoutService_Axial, orientation: ViewportOrientation.Axial),
            new ViewportDescriptor(id: "Mpr.Sagittal", title: Resources.ImagingLayoutService_Sagittal, orientation: ViewportOrientation.Sagittal),
            new ViewportDescriptor(id: "Mpr.Coronal", title: Resources.ImagingLayoutService_Coronal, orientation: ViewportOrientation.Coronal),
            new ViewportDescriptor(id: "Mpr.Preview3D", title: Resources.ImagingLayoutService_3dPreview, orientation: ViewportOrientation.VolumePreview, isInteractive: false)
        };

        private static readonly IReadOnlyList<ViewportDescriptor> SingleViewports = new[]
        {
            new ViewportDescriptor(id: "Single.Main", title: Resources.ImagingLayoutService_SingleMain, orientation: ViewportOrientation.Generic)
        };

        private static readonly IReadOnlyList<ViewportDescriptor> TwoByTwoViewports = new[]
        {
            new ViewportDescriptor(id: "TwoByTwo.Viewport1", title: Resources.ImagingLayoutService_ViewPort_TwoByTwo_1, orientation: ViewportOrientation.Generic),
            new ViewportDescriptor(id: "TwoByTwo.Viewport2", title: Resources.ImagingLayoutService_ViewPort_TwoByTwo_2, orientation: ViewportOrientation.Generic),
            new ViewportDescriptor(id: "TwoByTwo.Viewport3", title: Resources.ImagingLayoutService_ViewPort_TwoByTwo_3, orientation: ViewportOrientation.Generic),
            new ViewportDescriptor(id: "TwoByTwo.Viewport4", title: Resources.ImagingLayoutService_ViewPort_TwoByTwo_4, orientation: ViewportOrientation.Generic)
        };

        private ImagingLayout _currentLayout = ImagingLayout.Single;

        public event EventHandler<ImagingLayoutChangedEventArgs>? CurrentLayoutChanged;

        public ImagingLayout CurrentLayout => _currentLayout;

        public void CycleNextLayout()
        {
            var nextLayout = _currentLayout switch
            {
                ImagingLayout.Single => ImagingLayout.TwoByTwo,
                ImagingLayout.TwoByTwo => ImagingLayout.Mpr,
                ImagingLayout.Mpr => ImagingLayout.AxialSagittalCoronal,
                ImagingLayout.AxialSagittalCoronal => ImagingLayout.Single,
                _ => ImagingLayout.Single,
            };
            SetLayout(nextLayout);
        }

        public IReadOnlyList<ViewportDescriptor> GetViewportsForLayout(ImagingLayout layout) => layout switch
        {
            ImagingLayout.Single => SingleViewports,
            ImagingLayout.TwoByTwo => TwoByTwoViewports,
            ImagingLayout.Mpr => MprViewports,
            ImagingLayout.AxialSagittalCoronal => AxialSagittalCoronalViewports,
            _ => SingleViewports,
        };

        public void ResetLayout() => SetLayout(ImagingLayout.Single);

        public void SetLayout(ImagingLayout layout)
        {
            if (_currentLayout == layout)
            {
                return;
            }

            var oldLayout = _currentLayout;
            _currentLayout = layout;

            CurrentLayoutChanged?.Invoke(this, new ImagingLayoutChangedEventArgs(oldLayout, _currentLayout));
        }

        protected override void OnCreate(IImagingServiceBase @base)
        {            
        }

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}