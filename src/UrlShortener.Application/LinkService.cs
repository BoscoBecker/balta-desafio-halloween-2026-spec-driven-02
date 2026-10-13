using UrlShortener.Application.Abstractions;
using UrlShortener.Domain;

namespace UrlShortener.Application;

public sealed class LinkService
{
    private const int AutoCodeAttempts = 8;

    private readonly ILinkRepository _links;
    private readonly IShortCodeGenerator _codes;
    private readonly IManagementKeyService _managementKeys;
    private readonly ILinkRedirectCache _cache;
    private readonly IClock _clock;

    public LinkService(
        ILinkRepository links,
        IShortCodeGenerator codes,
        IManagementKeyService managementKeys,
        ILinkRedirectCache cache,
        IClock clock)
    {
        _links = links;
        _codes = codes;
        _managementKeys = managementKeys;
        _cache = cache;
        _clock = clock;
    }

    public async Task<ApplicationResult<CreatedLink>> CreateAsync(
        CreateLinkCommand command,
        CancellationToken cancellationToken)
    {
        if (!TargetUrlValidator.TryValidate(command.TargetUrl, out var urlError))
        {
            return Fail<CreatedLink>(400, "URL inválida", urlError);
        }

        var expiresAtUtc = ExpirationRules.NormalizeToEndOfUtcDate(command.ExpiresAtUtc);
        if (expiresAtUtc is not null && ExpirationRules.IsInThePast(expiresAtUtc.Value, _clock.UtcNow))
        {
            return Fail<CreatedLink>(400, "Data de expiração inválida", "A data de expiração não pode estar no passado.");
        }

        if (!string.IsNullOrWhiteSpace(command.Code))
        {
            return await CreateWithCustomCodeAsync(command.TargetUrl.Trim(), command.Code, expiresAtUtc, cancellationToken);
        }

        return await CreateWithGeneratedCodeAsync(command.TargetUrl.Trim(), expiresAtUtc, cancellationToken);
    }

    public async Task<ApplicationResult<RedirectResult>> RedirectAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || !LinkCodes.IsValid(code))
        {
            return Fail<RedirectResult>(404, "Link não encontrado", "O link informado não existe.");
        }

        var now = _clock.UtcNow;

        if (_cache.TryGet(code, out var cached) && cached is not null)
        {
            if (cached.ExpiresAtUtc is not null && now > cached.ExpiresAtUtc.Value)
            {
                _cache.Remove(code);
                return Fail<RedirectResult>(410, "Link expirado", "Este link expirou e não redireciona mais.");
            }

            var incrementedFromCache = await _links.IncrementAccessCountAsync(code, now, cancellationToken);
            if (incrementedFromCache == 0)
            {
                _cache.Remove(code);
                return Fail<RedirectResult>(410, "Link expirado", "Este link expirou e não redireciona mais.");
            }

            return ApplicationResult<RedirectResult>.Success(new RedirectResult(cached.TargetUrl));
        }

        var link = await _links.GetByCodeAsync(code, cancellationToken);
        if (link is null)
        {
            return Fail<RedirectResult>(404, "Link não encontrado", "O link informado não existe.");
        }

        if (link.IsExpired(now))
        {
            return Fail<RedirectResult>(410, "Link expirado", "Este link expirou e não redireciona mais.");
        }

        await _links.IncrementAccessCountAsync(code, now, cancellationToken);
        _cache.Set(code, new CachedLink(link.TargetUrl, link.ExpiresAtUtc));
        return ApplicationResult<RedirectResult>.Success(new RedirectResult(link.TargetUrl));
    }

    public async Task<ApplicationResult<LinkStats>> GetStatsAsync(
        string code,
        string? managementKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || !LinkCodes.IsValid(code))
        {
            return Fail<LinkStats>(404, "Link não encontrado", "O link informado não existe.");
        }

        var link = await _links.GetByCodeAsync(code, cancellationToken);
        if (link is null)
        {
            return Fail<LinkStats>(404, "Link não encontrado", "O link informado não existe.");
        }

        if (string.IsNullOrWhiteSpace(managementKey) || !_managementKeys.Matches(managementKey, link.ManagementKeyHash))
        {
            return Fail<LinkStats>(401, "Não autorizado", "A chave de gerenciamento é inválida.");
        }

        return ApplicationResult<LinkStats>.Success(
            new LinkStats(link.Code, link.TargetUrl, link.AccessCount, link.CreatedAtUtc, link.ExpiresAtUtc));
    }

    private async Task<ApplicationResult<CreatedLink>> CreateWithCustomCodeAsync(
        string targetUrl,
        string code,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken)
    {
        if (!LinkCodes.IsValid(code))
        {
            return Fail<CreatedLink>(
                400,
                "Código inválido",
                "O código deve ter de 4 a 20 caracteres, usando apenas letras maiúsculas de A a Z e números de 1 a 9.");
        }

        if (await _links.ExistsAsync(code, cancellationToken))
        {
            return Fail<CreatedLink>(409, "Código em uso", "Este código já está em uso. Escolha outro.");
        }

        try
        {
            return await PersistAsync(code, targetUrl, expiresAtUtc, cancellationToken);
        }
        catch (DuplicateCodeException)
        {
            return Fail<CreatedLink>(409, "Código em uso", "Este código já está em uso. Escolha outro.");
        }
    }

    private async Task<ApplicationResult<CreatedLink>> CreateWithGeneratedCodeAsync(
        string targetUrl,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < AutoCodeAttempts; attempt++)
        {
            var code = _codes.Generate();
            if (await _links.ExistsAsync(code, cancellationToken))
            {
                continue;
            }

            try
            {
                return await PersistAsync(code, targetUrl, expiresAtUtc, cancellationToken);
            }
            catch (DuplicateCodeException)
            {
                // RN-06: conflito de código gerado deve tentar outro.
            }
        }

        return Fail<CreatedLink>(
            503,
            "Não foi possível gerar o código",
            "Não foi possível gerar um código único. Tente novamente.");
    }

    private async Task<ApplicationResult<CreatedLink>> PersistAsync(
        string code,
        string targetUrl,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var managementKey = _managementKeys.Generate();
        var link = new Link
        {
            Id = Guid.NewGuid(),
            Code = code,
            TargetUrl = targetUrl,
            CreatedAtUtc = _clock.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            AccessCount = 0,
            ManagementKeyHash = _managementKeys.Hash(managementKey)
        };

        await _links.AddAsync(link, cancellationToken);
        await _links.SaveChangesAsync(cancellationToken);
        return ApplicationResult<CreatedLink>.Success(new CreatedLink(code, managementKey));
    }

    private static ApplicationResult<T> Fail<T>(int statusCode, string title, string detail) =>
        ApplicationResult<T>.Failure(new ApplicationError(statusCode, title, detail));
}
