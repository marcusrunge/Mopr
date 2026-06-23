using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Implementations
{
    internal sealed class DicomMetadataService : CreateableBindableBase<IDicomMetadataService, DicomMetadataService, IDicomBase>, IDicomMetadataService
    {
        private IDicomBase? _base;

        public bool IsDicomFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                if (!fileInfo.Exists || fileInfo.Length < 132)
                {
                    return false;
                }

                using var stream = File.OpenRead(filePath);

                var buffer = new byte[132];
                var read = stream.Read(buffer, 0, buffer.Length);

                if (read < 132)
                {
                    return false;
                }

                return buffer[128] == (byte)'D' && buffer[129] == (byte)'I' && buffer[130] == (byte)'C' && buffer[131] == (byte)'M';
            }
            catch (Exception exception)
            {
                _base?.OnExceptionThrown(exception);
                return false;
            }
        }

        protected override void OnCreate(IDicomBase @base) => _base = @base;

        protected override Task OnCreateAsync(IDicomBase @base, CancellationToken cancellationToken)
        {
            _base = @base;

            return Task.CompletedTask;
        }
    }
}