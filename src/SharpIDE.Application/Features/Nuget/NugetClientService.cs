using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Nuget;

public record IdePackageResult(IPackageSearchMetadata PackageSearchMetadata, List<PackageSource> PackageSources);
public class NugetClientService
{
	private readonly bool _includePrerelease = false;
	public async Task<List<IdePackageResult>> GetTop100Results(string directoryPath, CancellationToken cancellationToken = default)
	{
		var settings = Settings.LoadDefaultSettings(root: directoryPath);
		var packageSourceProvider = new PackageSourceProvider(settings);
		var packageSources = packageSourceProvider.LoadPackageSources().Where(p => p.IsEnabled).ToList();

		var logger = NullLogger.Instance;
		var cache = new SourceCacheContext();

		var packagesResult = new List<IdePackageResult>();

		foreach (var source in packageSources)
		{
			var repository = Repository.Factory.GetCoreV3(source.Source);
			var searchResource = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken).ConfigureAwait(false);

			if (searchResource == null)
				continue;

			// Search for top packages (no search term = all)
			var results = await searchResource.SearchAsync(
				searchTerm: string.Empty,
				filters: new SearchFilter(includePrerelease: _includePrerelease),
				skip: 0,
				take: 100,
				log: logger,
				cancellationToken: cancellationToken).ConfigureAwait(false);

			packagesResult.AddRange(results.Select(s => new IdePackageResult(s, [source])));
		}

		// Combine, group, and order by download count
		var topPackages = packagesResult
			.GroupBy(p => p.PackageSearchMetadata.Identity.Id, StringComparer.OrdinalIgnoreCase)
			.Select(g => g.First())
			.OrderByDescending(p => p.PackageSearchMetadata.DownloadCount ?? 0)
			.Take(100)
			.ToList();

		// we need to find out if other package sources have the package too
		foreach (var package in topPackages)
		{
			foreach (var source in packageSources.Except(package.PackageSources).ToList())
			{
				var repository = Repository.Factory.GetCoreV3(source.Source);
				var searchResource = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken).ConfigureAwait(false);

				if (searchResource == null)
					continue;

				var packageId = package.PackageSearchMetadata.Identity.Id;
				// TODO: Might be faster to use FindPackageByIdResource
				var results = await searchResource.SearchAsync(
					searchTerm: packageId,
					filters: new SearchFilter(includePrerelease: _includePrerelease),
					skip: 0,
					take: 2,
					log: logger,
					cancellationToken: cancellationToken).ConfigureAwait(false);
				var foundPackage = results.SingleOrDefault(r => r.Identity.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
				if (foundPackage != null)
				{
					package.PackageSources.Add(source);
				}
			}
		}

		return topPackages;
	}
}
