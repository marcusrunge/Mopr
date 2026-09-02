using MarcusRunge.Mopr.Workbench.Application.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class RepositoryLocationValidationServiceTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public async Task ValidateAsync_WithEmptyPath_ReturnsInvalidResult(string directoryPath)
        {
            var result = await new RepositoryLocationValidationService().ValidateAsync(directoryPath, TestContext.Current.CancellationToken);

            Assert.False(result.Exists);
            Assert.False(result.IsReadable);
            Assert.False(result.IsWritable);
            Assert.False(result.IsValid);
            Assert.Null(result.NormalizedPath);
        }

        [Fact]
        public async Task ValidateAsync_WithMissingDirectory_ReturnsNormalizedInvalidResult()
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), "MoprRepositoryLocationTests", Guid.NewGuid().ToString("N"));
            var result = await new RepositoryLocationValidationService().ValidateAsync(directoryPath, TestContext.Current.CancellationToken);

            Assert.False(result.Exists);
            Assert.False(result.IsReadable);
            Assert.False(result.IsWritable);
            Assert.False(result.IsValid);
            Assert.Equal(Path.GetFullPath(directoryPath), result.NormalizedPath);
        }

        [Fact]
        public async Task ValidateAsync_WithAccessibleDirectory_ReturnsValidResultAndLeavesNoValidationFile()
        {
            var directoryPath = CreateTemporaryDirectory();

            try
            {
                var filesBeforeValidation = Directory.GetFiles(directoryPath);
                var result = await new RepositoryLocationValidationService().ValidateAsync(directoryPath, TestContext.Current.CancellationToken);
                var filesAfterValidation = Directory.GetFiles(directoryPath);

                Assert.True(result.Exists);
                Assert.True(result.IsReadable);
                Assert.True(result.IsWritable);
                Assert.True(result.IsValid);
                Assert.Equal(Path.GetFullPath(directoryPath), result.NormalizedPath);
                Assert.Equal(filesBeforeValidation, filesAfterValidation);
                Assert.DoesNotContain(Directory.GetFiles(directoryPath), path => Path.GetFileName(path).StartsWith(".mopr-write-test-", StringComparison.Ordinal));
            }
            finally
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }

        [Fact]
        public async Task ValidateAsync_WithCanceledToken_ThrowsOperationCanceledException()
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await new RepositoryLocationValidationService().ValidateAsync(Path.GetTempPath(), cancellation.Token));
        }

        private static string CreateTemporaryDirectory()
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), "MoprRepositoryLocationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directoryPath);

            return directoryPath;
        }
    }
}