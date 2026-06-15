namespace Domain;

public interface IConfigurationStore<TSettings>
    where TSettings : class, new()
{
    Task<TSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(TSettings settings, CancellationToken cancellationToken = default);
}
