using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NfeSaas.Infrastructure.Data.Converters;

/// <summary>
/// Cifra strings em repouso usando ASP.NET Data Protection. Aplicado em colunas que armazenam
/// secrets (senha do certificado A1, token CSC da NFC-e). Decriptografia transparente para o
/// resto da aplicação — entidades continuam vendo o valor em claro.
/// </summary>
/// <remarks>
/// Os valores cifrados são prefixados com <c>"enc:v1:"</c> para distinguir de dados legados em
/// texto claro. Migração de dados pré-existentes deve ser feita uma única vez ao subir a
/// versão (ver script em <c>scripts/encrypt_legacy_secrets.sql</c> — opcional, hoje só há
/// dados em ambiente de demo).
/// </remarks>
public sealed class EncryptedStringConverter : ValueConverter<string?, string?>
{
    private const string Prefix = "enc:v1:";

    public EncryptedStringConverter(IDataProtector protector)
        : base(
            v => Encrypt(protector, v),
            v => Decrypt(protector, v))
    {
    }

    private static string? Encrypt(IDataProtector protector, string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return plain;
        return Prefix + protector.Protect(plain);
    }

    private static string? Decrypt(IDataProtector protector, string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return stored;
        // Compat: valor legado sem prefixo é retornado como está (uma rotação de chave/upload
        // do certificado força a re-cifragem).
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;
        return protector.Unprotect(stored[Prefix.Length..]);
    }
}
