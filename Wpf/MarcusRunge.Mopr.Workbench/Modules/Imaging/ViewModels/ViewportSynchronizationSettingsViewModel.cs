using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportSynchronizationSettingsViewModel : ViewModelBase
    {
        private readonly IImagingViewportSelectionService _selectionService;
        private readonly IViewportSynchronizationService _synchronizationService;
        private DelegateCommand? _addActiveViewportCommand, _createGroupCommand, _removeActiveViewportCommand, _removeGroupCommand;
        private DelegateCommand<ViewportSynchronizationGroupAssignmentRequest?>? _addViewportToGroupCommand;
        private string _newGroupName = string.Empty;
        private ViewportSynchronizationGroupItemViewModel? _selectedGroup;

        public ViewportSynchronizationSettingsViewModel(ICore core, IViewportSynchronizationService synchronizationService)
        {
            ArgumentNullException.ThrowIfNull(core);
            _selectionService = core.ImagingService?.ImagingViewportSelectionService ?? throw new InvalidOperationException("The imaging viewport selection service is not available through the core facade.");
            _synchronizationService = synchronizationService ?? throw new ArgumentNullException(nameof(synchronizationService));
            _selectionService.ActiveViewportChanged += OnActiveViewportChanged;
            RefreshGroups();
        }

        public DelegateCommand<ViewportSynchronizationGroupAssignmentRequest?> AddViewportToGroupCommand => _addViewportToGroupCommand ??= new DelegateCommand<ViewportSynchronizationGroupAssignmentRequest?>(AddViewportToGroup, CanAddViewportToGroup);
        public DelegateCommand AddActiveViewportCommand => _addActiveViewportCommand ??= new DelegateCommand(AddActiveViewport, CanAddActiveViewport).ObservesProperty(() => SelectedGroup);
        public bool CanConfigureGroup => SelectedGroup != null;
        public DelegateCommand CreateGroupCommand => _createGroupCommand ??= new DelegateCommand(CreateGroup, CanCreateGroup).ObservesProperty(() => NewGroupName);
        public ObservableCollection<ViewportSynchronizationGroupItemViewModel> Groups { get; } = [];
        public string NewGroupName { get => _newGroupName; set => SetProperty(ref _newGroupName, value); }
        public DelegateCommand RemoveActiveViewportCommand => _removeActiveViewportCommand ??= new DelegateCommand(RemoveActiveViewport, CanRemoveActiveViewport).ObservesProperty(() => SelectedGroup);
        public DelegateCommand RemoveGroupCommand => _removeGroupCommand ??= new DelegateCommand(RemoveGroup, () => SelectedGroup != null).ObservesProperty(() => SelectedGroup);

        public ViewportSynchronizationGroupItemViewModel? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (!SetProperty(ref _selectedGroup, value)) return;
                RaiseConfigurationProperties();
                RaiseCommandStates();
            }
        }

        public bool IsSliceEnabled { get => HasFlag(ViewportSynchronizationFlags.Slice); set => SetFlag(ViewportSynchronizationFlags.Slice, value); }
        public bool IsWindowLevelEnabled { get => HasFlag(ViewportSynchronizationFlags.WindowLevel); set => SetFlag(ViewportSynchronizationFlags.WindowLevel, value); }
        public bool IsZoomEnabled { get => HasFlag(ViewportSynchronizationFlags.Zoom); set => SetFlag(ViewportSynchronizationFlags.Zoom, value); }
        public bool IsPanEnabled { get => HasFlag(ViewportSynchronizationFlags.Pan); set => SetFlag(ViewportSynchronizationFlags.Pan, value); }
        public bool IsRotationEnabled { get => HasFlag(ViewportSynchronizationFlags.Rotation); set => SetFlag(ViewportSynchronizationFlags.Rotation, value); }
        public bool IsHorizontalFlipEnabled { get => HasFlag(ViewportSynchronizationFlags.HorizontalFlip); set => SetFlag(ViewportSynchronizationFlags.HorizontalFlip, value); }
        public bool IsVerticalFlipEnabled { get => HasFlag(ViewportSynchronizationFlags.VerticalFlip); set => SetFlag(ViewportSynchronizationFlags.VerticalFlip, value); }

        public override void Destroy()
        {
            _selectionService.ActiveViewportChanged -= OnActiveViewportChanged;
            base.Destroy();
        }

        private void AddViewportToGroup(ViewportSynchronizationGroupAssignmentRequest? request)
        {
            if (!CanAddViewportToGroup(request)) return;
            SelectUpdated(_synchronizationService.AddViewport(request!.GroupId, request.ViewportId));
        }

        private bool CanAddViewportToGroup(ViewportSynchronizationGroupAssignmentRequest? request) => request != null && _synchronizationService.GetGroup(request.GroupId) is { } group && !group.ViewportIds.Contains(request.ViewportId, StringComparer.Ordinal);

        private void AddActiveViewport()
        {
            if (SelectedGroup == null || string.IsNullOrWhiteSpace(_selectionService.ActiveViewportId)) return;
            SelectUpdated(_synchronizationService.AddViewport(SelectedGroup.Id, _selectionService.ActiveViewportId));
        }

        private bool CanAddActiveViewport() => SelectedGroup != null && !string.IsNullOrWhiteSpace(_selectionService.ActiveViewportId) && !SelectedGroup.Group.ViewportIds.Contains(_selectionService.ActiveViewportId, StringComparer.Ordinal);
        private bool CanCreateGroup() => !string.IsNullOrWhiteSpace(NewGroupName) && !string.IsNullOrWhiteSpace(_selectionService.ActiveViewportId);
        private bool CanRemoveActiveViewport() => SelectedGroup != null && SelectedGroup.Group.ViewportIds.Contains(_selectionService.ActiveViewportId, StringComparer.Ordinal);

        private void CreateGroup()
        {
            if (!CanCreateGroup()) return;
            var group = _synchronizationService.CreateGroup(NewGroupName.Trim(), [_selectionService.ActiveViewportId]);
            NewGroupName = string.Empty;
            RefreshGroups(group.Id);
        }

        private bool HasFlag(ViewportSynchronizationFlags flag) => SelectedGroup?.Group.Flags.HasFlag(flag) == true;
        private void OnActiveViewportChanged(object? sender, ImagingViewportSelectionChangedEventArgs e) => RaiseCommandStates();

        private void RefreshGroups(Guid? selectedGroupId = null)
        {
            selectedGroupId ??= SelectedGroup?.Id;
            Groups.Clear();
            foreach (var group in _synchronizationService.GetGroups()) Groups.Add(new ViewportSynchronizationGroupItemViewModel(group));
            SelectedGroup = selectedGroupId.HasValue ? Groups.FirstOrDefault(group => group.Id == selectedGroupId.Value) : Groups.FirstOrDefault();
        }

        private void RaiseCommandStates()
        {
            AddActiveViewportCommand.RaiseCanExecuteChanged();
            CreateGroupCommand.RaiseCanExecuteChanged();
            RemoveActiveViewportCommand.RaiseCanExecuteChanged();
            RemoveGroupCommand.RaiseCanExecuteChanged();
        }

        private void RaiseConfigurationProperties()
        {
            RaisePropertyChanged(nameof(CanConfigureGroup));
            RaisePropertyChanged(nameof(IsSliceEnabled));
            RaisePropertyChanged(nameof(IsWindowLevelEnabled));
            RaisePropertyChanged(nameof(IsZoomEnabled));
            RaisePropertyChanged(nameof(IsPanEnabled));
            RaisePropertyChanged(nameof(IsRotationEnabled));
            RaisePropertyChanged(nameof(IsHorizontalFlipEnabled));
            RaisePropertyChanged(nameof(IsVerticalFlipEnabled));
        }

        private void RemoveActiveViewport()
        {
            if (!CanRemoveActiveViewport()) return;
            SelectUpdated(_synchronizationService.RemoveViewport(SelectedGroup!.Id, _selectionService.ActiveViewportId));
        }

        private void RemoveGroup()
        {
            if (SelectedGroup == null) return;
            _synchronizationService.RemoveGroup(SelectedGroup.Id);
            RefreshGroups();
        }

        private void SelectUpdated(Models.ViewportSynchronizationGroup group) => RefreshGroups(group.Id);

        private void SetFlag(ViewportSynchronizationFlags flag, bool enabled)
        {
            if (SelectedGroup == null) return;
            var flags = enabled ? SelectedGroup.Group.Flags | flag : SelectedGroup.Group.Flags & ~flag;
            if (flags == SelectedGroup.Group.Flags) return;
            SelectUpdated(_synchronizationService.SetGroupFlags(SelectedGroup.Id, flags));
        }
    }
}
