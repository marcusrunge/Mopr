using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents an unreal object within the MOPR system.
    /// </summary>
    public class UnrealObject : AuditableEntityBase
    {
        private Instance? _instance;
        private int _instanceId;
        private string? _name, _className, _assetPath, _metadataJson;

        /// <summary>
        /// Gets or sets the asset path of the unreal object.
        /// </summary>
        public string? AssetPath { get => _assetPath; set => SetProperty(ref _assetPath, value); }

        /// <summary>
        /// Gets or sets the class name of the unreal object.
        /// </summary>
        public string? ClassName { get => _className; set => SetProperty(ref _className, value); }

        /// <summary>
        /// Gets or sets the instance associated with the unreal object.
        /// </summary>
        public Instance? Instance { get => _instance; set => SetProperty(ref _instance, value); }

        /// <summary>
        /// Gets or sets the ID of the instance associated with the unreal object.
        /// </summary>
        public int InstanceId { get => _instanceId; set => SetProperty(ref _instanceId, value); }

        /// <summary>
        /// Gets or sets the metadata JSON of the unreal object.
        /// </summary>
        public string? MetadataJson { get => _metadataJson; set => SetProperty(ref _metadataJson, value); }

        /// <summary>
        /// Gets or sets the name of the unreal object.
        /// </summary>
        public string? Name { get => _name; set => SetProperty(ref _name, value); }
    }
}