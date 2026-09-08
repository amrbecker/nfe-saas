using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NfeSaas.Infrastructure.Data.Converters;

/// <summary>
/// Cifra bytes em repouso usando ASP.NET Data Protection. Aplicado em colunas binárias que
/// armazenam secrets (hoje: o PFX bruto do certificado digital A1 da Empresa). Decriptografia
/// transparente para o resto da aplicação — entidades continuam vendo os bytes em claro.
/// </summary>
/// <remarks>
/// Equivalente binário do <see cref="EncryptedStringConverter"/>: os bytes cifrados são
/// prefixados com o marcador ASCII "ENCB1:" para distinguir de dados legados gravados em claro
/// (um PFX/PKCS#12 começa com a tag ASN.1 DER 0x30, nunca com esse marcador). Valor legado sem
/// o marcador é lido como está — a re-cifragem acontece na próxima vez que o certificado for
/// re-enviado (upload substitui o valor via UPDATE).
/// </remarks>
public sealed class EncryptedBytesConverter : ValueConverter<byte[]?, byte[]?>
{
    private static readonly byte[] Marker = "ENCB1:"u8.ToArray();

    public EncryptedBytesConverter(IDataProtector protector)
        : base(
            v => Encrypt(protector, v),
            v => Decrypt(protector, v))
    {
    }

    private static byte[]? Encrypt(IDataProtector protector, byte[]? plain)
    {
        if (plain == null || plain.Length == 0) return plain;
        var protegido = protector.Protect(plain);
        var resultado = new byte[Marker.Length + protegido.Length];
        Marker.CopyTo(resultado, 0);
        protegido.CopyTo(resultado, Marker.Length);
        return resultado;
    }

    private static byte[]? Decrypt(IDataProtector protector, byte[]? stored)
    {
        if (stored == null || stored.Length == 0) return stored;
        if (stored.Length < Marker.Length || !stored.AsSpan(0, Marker.Length).SequenceEqual(Marker))
            return stored; // legado: PFX em claro, sem marcador — re-cifra no próximo upload
        return protector.Unprotect(stored[Marker.Length..]);
    }
}
