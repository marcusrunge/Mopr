using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels
{
    /// <summary>
    /// Exposes one imported DICOM series to the study-selection dialog.
    /// </summary>
    public sealed class SeriesItemViewModel : BindableBase
    {
        public SeriesItemViewModel(SeriesCatalogItem model)
        {
            ArgumentNullException.ThrowIfNull(model);
            Model = model;
        }

        public string Description => string.IsNullOrWhiteSpace(Model.Description) ? Properties.Resources.UnknownSeriesDescription : Model.Description;
        public string DisplayName => string.Format(Properties.Resources.SeriesDisplayFormat, Description, Modality, ImageCount);
        public int ImageCount => Model.ImageCount;
        public bool IsAvailable => Model.IsAvailable;
        public string Modality => string.IsNullOrWhiteSpace(Model.Modality) ? Properties.Resources.UnknownModality : Model.Modality;
        public SeriesCatalogItem Model { get; }
        public int SeriesId => Model.SeriesId;
        public string SeriesInstanceUid => Model.SeriesInstanceUid;
        public int? SeriesNumber => Model.SeriesNumber;
        public string StatusText => IsAvailable ? Properties.Resources.SeriesAvailable : Properties.Resources.SeriesUnavailable;
    }
}