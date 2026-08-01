using System.Text.Json;

using Domain.Services;

using FirebaseAdmin;
using FirebaseAdmin.Auth;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Infrastructure.Services;

public class FirebaseAuthenticationService : IFirebaseAuthenticationService
{
    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseAuthenticationService(FirebaseApp firebaseApp)
    {
        _firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);
    }

    public async Task<FirebaseUserResponse> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw InvalidToken();
        }

        try
        {
            var token = await _firebaseAuth.VerifyIdTokenAsync(idToken, true, cancellationToken);
            var firebaseClaim = GetDictionary(token.Claims, "firebase");
            var identities = firebaseClaim == null ? null : GetDictionary(firebaseClaim, "identities");
            var googleProviderId = identities == null ? null : GetFirstString(identities, "google.com");

            var result = new FirebaseUserResponse
            {
                Uid = token.Uid ?? string.Empty,
                Email = GetString(token.Claims, "email") ?? string.Empty,
                EmailVerified = GetBoolean(token.Claims, "email_verified"),
                Name = GetString(token.Claims, "name") ?? string.Empty,
                PictureUrl = GetString(token.Claims, "picture") ?? string.Empty,
                SignInProvider = firebaseClaim == null ? string.Empty : GetString(firebaseClaim, "sign_in_provider") ?? string.Empty,
                GoogleProviderId = googleProviderId
            };

            if (string.IsNullOrWhiteSpace(result.Uid) || string.IsNullOrWhiteSpace(result.Email))
            {
                throw InvalidToken();
            }

            return result;
        }
        catch (GenericException)
        {
            throw;
        }
        catch (FirebaseAuthException)
        {
            throw InvalidToken();
        }
        catch (FirebaseException)
        {
            throw InvalidToken();
        }
        catch (ArgumentException)
        {
            throw InvalidToken();
        }
        catch (FormatException)
        {
            throw InvalidToken();
        }
        catch (JsonException)
        {
            throw InvalidToken();
        }
    }

    private static IReadOnlyDictionary<string, object>? GetDictionary(IReadOnlyDictionary<string, object> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return null;
        }

        return value as IReadOnlyDictionary<string, object>
            ?? (value as IDictionary<string, object>)?.ToDictionary(x => x.Key, x => x.Value)
            ?? (value is JsonElement element && element.ValueKind == JsonValueKind.Object
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText())
                : null);
    }

    private static string? GetString(IReadOnlyDictionary<string, object> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            string stringValue => stringValue,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => null
        };
    }

    private static string? GetFirstString(IReadOnlyDictionary<string, object> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            IEnumerable<string> list => list.FirstOrDefault(),
            IEnumerable<object> list => list.OfType<string>().FirstOrDefault(),
            JsonElement element when element.ValueKind == JsonValueKind.Array => element.EnumerateArray()
                .FirstOrDefault(x => x.ValueKind == JsonValueKind.String).GetString(),
            string stringValue => stringValue,
            _ => null
        };
    }

    private static bool GetBoolean(IReadOnlyDictionary<string, object> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return false;
        }

        return value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            JsonElement element when element.ValueKind == JsonValueKind.True => true,
            JsonElement element when element.ValueKind == JsonValueKind.False => false,
            _ => false
        };
    }

    private static GenericException InvalidToken()
    {
        return new GenericException(Shared.Enums.ErrorCode.Failure, ErrorMessage.InvalidAccessToken, System.Net.HttpStatusCode.Unauthorized);
    }
}
