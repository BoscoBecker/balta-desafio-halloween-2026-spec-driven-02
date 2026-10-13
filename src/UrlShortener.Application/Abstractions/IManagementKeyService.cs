namespace UrlShortener.Application.Abstractions;

public interface IManagementKeyService
{
    string Generate();
    string Hash(string managementKey);
    bool Matches(string managementKey, string hash);
}
